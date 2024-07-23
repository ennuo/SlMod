using System.Buffers.Binary;
using System.Numerics;
using System.Runtime.Serialization;
using System.Text;
using SlLib.Extensions;
using SlLib.Resources.Database;
using SlLib.Resources.Scene;
using SlLib.Resources.Scene.Definitions;
using SlLib.Resources.Scene.Instances;
using SlLib.SumoTool.Siff;
using SlLib.Utilities;

namespace SlLib.Serialization;

public class ResourceLoadContext
{
    /// <summary>
    ///     Resource data.
    /// </summary>
    public readonly ArraySegment<byte> _data;

    /// <summary>
    ///     Optional resource database for linking other resources.
    /// </summary>
    private readonly SlResourceDatabase? _database;

    /// <summary>
    ///     Resource GPU data.
    /// </summary>
    public readonly ArraySegment<byte> _gpuData;

    /// <summary>
    ///     Cached serialized references by address.
    /// </summary>
    private readonly Dictionary<int, IResourceSerializable> _references = [];

    /// <summary>
    ///     Pointer relocations table.
    /// </summary>
    private readonly List<SlResourceRelocation> _relocations;

    /// <summary>
    ///     The current position in the stream.
    /// </summary>
    public int Position;

    public ResourceLoadContext(ArraySegment<byte> cpuData, ArraySegment<byte> gpuData = default)
    {
        // For now, just default to Win32
        Platform = SlPlatform.Win32;

        _data = cpuData;
        _gpuData = gpuData;
        _relocations = [];
    }

    /// <summary>
    ///     Constructs a resource load context for a resource database chunk.
    /// </summary>
    /// <param name="database">The database that owns this chunk</param>
    /// <param name="chunk">The chunk to load</param>
    public ResourceLoadContext(SlResourceDatabase database, SlResourceChunk chunk)
    {
        _database = database;

        Version = chunk.Version;
        Platform = database.Platform;
        _relocations = chunk.Relocations;
        _data = chunk.Data;
        _gpuData = chunk.GpuData;
    }

    /// <summary>
    ///     Offsets for all CPU pointers.
    /// </summary>
    public int Base { get; set; }

    /// <summary>
    ///     Offsets for all GPU pointers.
    /// </summary>
    public int GpuBase { get; set; }

    /// <summary>
    ///     The version of the chunk being loaded.
    /// </summary>
    public int Version;

    /// <summary>
    ///     The platform of the chunk being loaded.
    /// </summary>
    public SlPlatform Platform;

    /// <summary>
    ///     Whether this chunk is from Sonic & Sega All Stars Racing
    /// </summary>
    public bool IsSSR;
    
    /// <summary>
    ///     Creates a subcontext based on this resource load context for KSiff resources
    /// </summary>
    /// <param name="cpuBase">Address of CPU element to load</param>
    /// <param name="gpuBase">GPU data offset</param>
    /// <returns></returns>
    public ResourceLoadContext CreateSubContext(int cpuBase, int gpuBase)
    {
        return new ResourceLoadContext(_data[cpuBase..], _gpuData[gpuBase..])
        {
            Platform = Platform,
            IsSSR = IsSSR,
            Version = Version
        };
    }

    #region Reference/Object Loading

    /// <summary>
    ///     Reads an object at an address and caches the reference for future lookups.
    /// </summary>
    /// <typeparam name="T">Type of object to load, must implement ILoadable</typeparam>
    /// <returns>Reference to object</returns>
    public T LoadReference<T>() where T : IResourceSerializable, new()
    {
        if (_references.TryGetValue(Position, out IResourceSerializable? reference))
        {
            // Since we're not reading the data because an instance already exists,
            // we need to make sure to seek past the data.
            Position += reference.GetSizeForSerialization(Platform, Version);
            return (T)reference;
        }

        int start = Position;
        
        // Cache the object before loading it to fix any structures
        // that reference themselves.
        T value = new();
        _references[Position] = value;
        
        value.Load(this);
        
        // Just in case the load function doesn't read all the data, make sure we
        // always advance the correct amount.
        Position = start + value.GetSizeForSerialization(Platform, Version);
        
        return value;
    }
    
    /// <summary>
    ///     Loads a resource reference from the database.
    /// </summary>
    /// <param name="id">Unique identifier of the resource to load</param>
    /// <typeparam name="T">Type of resource to load, must implement ISumoResource</typeparam>
    /// <returns>Reference to resource</returns>
    public SlResPtr<T> LoadResource<T>(int id) where T : ISumoResource, new()
    {
        return new SlResPtr<T>(_database, id);
    }
    
    /// <summary>
    ///     Loads a scene graph node from the database.
    /// </summary>
    /// <param name="id">Unique identifier of the node to load</param>
    /// <returns>Reference to node</returns>
    public SeGraphNode? LoadNode(int id)
    {
        if (_database == null || id == 0) return null;
        
        // There's a couple special cases
        if (id == SeDefinitionFolderNode.Default.Uid)
            return SeDefinitionFolderNode.Default;
        if (id == _database.Scene.Uid) return _database.Scene;
        
        return _database?.LoadGenericNode(id);
    }
    
    public string ReadString(int offset)
    {
        return _data.Array!.ReadString(_data.Offset + offset);
    }

    /// <summary>
    ///     Aligns the current stream position to a given boundary.
    /// </summary>
    /// <param name="boundary">Alignment boundary</param>
    public void Align(int boundary)
    {
        Position = SlUtil.Align(Position, boundary);
    }

    /// <summary>
    ///     Loads an object from the stream.
    /// </summary>
    /// <typeparam name="T">Type of object to load, must implement ILoadable</typeparam>
    /// <returns>Loaded object</returns>
    public T LoadObject<T>() where T : IResourceSerializable, new()
    {
        int start = Position;

        T value = new();
        value.Load(this);

        // Just in case the load function doesn't read all the data, make sure we
        // always advance the correct amount.
        Position = start + value.GetSizeForSerialization(Platform, Version);

        return value;
    }

    /// <summary>
    ///     Reads an array of references at an offset.
    /// </summary>
    /// <param name="address">The absolute offset of the array data</param>
    /// <param name="size">Number of elements in the array</param>
    /// <typeparam name="T">Type of object to load, must implement ILoadable</typeparam>
    /// <returns>Reference array</returns>
    public List<T> LoadArray<T>(int address, int size) where T : IResourceSerializable, new()
    {
        var list = new List<T>(size);
        if (address == 0 || size == 0) return list;
        
        int link = Position;
        Position = address;
        for (int i = 0; i < size; ++i)
            list.Add(LoadReference<T>());
        Position = link;

        return list;
    }
    
    /// <summary>
    ///     Reads an array of pointers at an address.
    /// </summary>
    /// <param name="address">Address of array data</param>
    /// <param name="size">Number of elements in the array</param>
    /// <typeparam name="T">Type of object to load, must implement ILoadable</typeparam>
    /// <returns>Reference array</returns>
    public List<T> LoadPointerArray<T>(int address, int size) where T : IResourceSerializable, new()
    {
        var list = new List<T>(size);
        if (address == 0 || size == 0) return list;
        
        int link = Position;
        Position = address;
        for (int i = 0; i < size; ++i)
            list.Add(LoadPointer<T>() ?? throw new SerializationException("Pointer in array was NULL!"));
        Position = link;

        return list;
    }
    
    /// <summary>
    ///     Reads an array of pointers.
    /// </summary>
    /// <param name="size">Number of elements in the array</param>
    /// <typeparam name="T">Type of object to load, must implement ILoadable</typeparam>
    /// <returns>Reference array</returns>
    public List<T> LoadPointerArray<T>(int size) where T : IResourceSerializable, new()
    {
        int address = ReadPointer();
        var list = new List<T>(size);
        if (address == 0 || size == 0) return list;
        
        int link = Position;
        Position = address;
        for (int i = 0; i < size; ++i)
            list.Add(LoadPointer<T>() ?? throw new SerializationException("Pointer in array was NULL!"));
        Position = link;

        return list;
    }
    
    /// <summary>
    ///     Reads an array of references from the stream.
    /// </summary>
    /// <param name="size">Number of elements in the array</param>
    /// <typeparam name="T">Type of object to load, must implement ILoadable</typeparam>
    /// <returns>Reference array</returns>
    public List<T> LoadArrayPointer<T>(int size) where T : IResourceSerializable, new()
    {
        return LoadArray<T>(ReadPointer(), size);
    }
    
    /// <summary>
    ///     Reads an array of elements at an offset with a given element reader function.
    /// </summary>
    /// <param name="address">The absolute offset of the array data</param>
    /// <param name="size">Number of elements in the array</param>
    /// <param name="reader">Function to read each element</param>
    /// <typeparam name="T">Type of object to load</typeparam>
    /// <returns>Array</returns>
    public List<T> LoadArray<T>(int address, int size, Func<T> reader)
    {
        var list = new List<T>(size);
        if (address == 0 || size == 0) return list;
        
        int link = Position;
        Position = address;
        for (int i = 0; i < size; ++i)
            list.Add(reader());
        Position = link;

        return list;
    }

    /// <summary>
    ///     Reads an array of elements with a given element reader function.
    /// </summary>
    /// <param name="size">Number of elements in the array</param>
    /// <param name="reader">Function to read each element</param>
    /// <typeparam name="T">Type of object to load</typeparam>
    /// <returns>Array</returns>
    public List<T> LoadArrayPointer<T>(int size, Func<T> reader)
    {
        return LoadArray(ReadPointer(), size, reader);
    }

    /// <summary>
    ///     Reads a pointer at an address and returns a reference to the object.
    /// </summary>
    /// <typeparam name="T">Type of object to load, must implement ILoadable</typeparam>
    /// <returns>Reference to object</returns>
    public T? LoadPointer<T>() where T : IResourceSerializable, new()
    {
        int address = ReadPointer();
        if (address == 0) return default;

        // Temporarily set the position to the pointer address
        int link = Position;
        Position = address;

        var obj = LoadReference<T>();
        Position = link;
        return obj;
    }

    /// <summary>
    ///     Reads a buffer and returns a view into it.
    /// </summary>
    /// <param name="address">Address of the buffer</param>
    /// <param name="size">Size of the buffer</param>
    /// <param name="gpu">Whether or not the buffer is located in GPU data</param>
    /// <returns>Slice of buffer</returns>
    public ArraySegment<byte> LoadBuffer(int address, int size, bool gpu)
    {
        if (size == 0) return new ArraySegment<byte>([]);
        int start = address;
        int end = start + size;

        return gpu ? _gpuData[start..end] : _data[start..end];
    }
    
    /// <summary>
    ///     Reads a pointer to a buffer and returns a view into it.
    /// </summary>
    /// <param name="size">Size of buffer</param>
    /// <param name="gpu">Whether or not the buffer came from GPU data</param>
    /// <returns>Slice of buffer</returns>
    public ArraySegment<byte> LoadBufferPointer(int size, out bool gpu)
    {
        gpu = false;

        int offset = Position;
        int address = ReadPointer();

        // If the size is 0, just return the default slice.
        if (size == 0) return default;

        // Check if this pointer is meant to be into GPU data.
        SlResourceRelocation? relocation = _relocations.Find(relocation => relocation.Offset == offset);
        gpu = relocation?.IsGpuPointer ?? false;

        int start = address;
        int end = start + size;

        return gpu ? _gpuData[start..end] : _data[start..end];
    }

    /// <summary>
    ///     Reads a resource pointer at an address and returns a reference to it.
    /// </summary>
    /// <typeparam name="T">Type of resource to load, must implement ISumoResource</typeparam>
    /// <returns>Reference to resource</returns>
    public SlResPtr<T> LoadResourcePointer<T>() where T : ISumoResource, new()
    {
        // Resource IDs get re-mapped to pointers at runtime, so they'll
        // actually pointer types.
        int id;
        if (Platform.Is64Bit) id = (int)ReadInt64();
        else id = ReadInt32();

        return LoadResource<T>(id);
    }

    #endregion

    #region Absolute Reading

    /// <summary>
    ///     Reads a boolean value at an offset in the stream without advancing the cursor.
    /// </summary>
    /// <param name="offset">Offset to read data from</param>
    /// <param name="wide">Whether or not to read an integer as a boolean</param>
    /// <returns>Boolean value</returns>
    public bool ReadBoolean(int offset, bool wide = false)
    {
        // Booleans are often stored as integers
        if (wide) return ReadInt32(offset) != 0;

        return _data[offset] != 0;
    }

    /// <summary>
    ///     Reads a byte value at an offset in the stream without advancing the cursor.
    /// </summary>
    /// <param name="offset">Offset to read data from</param>
    /// <returns>Byte value</returns>
    public byte ReadInt8(int offset)
    {
        return _data[offset];
    }

    /// <summary>
    ///     Reads a short value at an offset in the stream without advancing the cursor.
    /// </summary>
    /// <param name="offset">Offset to read data from</param>
    /// <returns>Short value</returns>
    public short ReadInt16(int offset)
    {
        var span = _data.AsSpan(offset, sizeof(short));
        return Platform.IsBigEndian
            ? BinaryPrimitives.ReadInt16BigEndian(span)
            : BinaryPrimitives.ReadInt16LittleEndian(span);
    }

    /// <summary>
    ///     Reads an integer value at an offset in the stream without advancing the cursor.
    /// </summary>
    /// <param name="offset">Offset to read data from</param>
    /// <returns>Integer value</returns>
    public int ReadInt32(int offset)
    {
        var span = _data.AsSpan(offset, sizeof(int));
        return Platform.IsBigEndian
            ? BinaryPrimitives.ReadInt32BigEndian(span)
            : BinaryPrimitives.ReadInt32LittleEndian(span);
    }

    /// <summary>
    ///     Reads a bitset value at an offset in the stream without advancing the cursor.
    /// </summary>
    /// <param name="offset">Offset to read data from</param>
    /// <returns>Bitset value</returns>
    public int ReadBitset32(int offset)
    {
        int value = ReadInt32(offset);
        return Platform.IsBigEndian ? SlUtil.ReverseBits(value) : value;
    }

    /// <summary>
    ///     Reads a long value at an offset in the stream without advancing the cursor.
    /// </summary>
    /// <param name="offset">Offset to read data from</param>
    /// <returns>Long value</returns>
    public long ReadInt64(int offset)
    {
        var span = _data.AsSpan(offset, sizeof(long));
        return Platform.IsBigEndian
            ? BinaryPrimitives.ReadInt64BigEndian(span)
            : BinaryPrimitives.ReadInt64LittleEndian(span);
    }

    /// <summary>
    ///     Reads a platform dependent pointer at an offset in the stream without advancing the cursor.
    /// </summary>
    /// <param name="offset">Offset to read data from</param>
    /// <param name="gpu">Whether or not the pointer is into GPU data</param>
    /// <returns>Pointer address</returns>
    public int ReadPointer(int offset, out bool gpu)
    {
        gpu = false;
        SlResourceRelocation? relocation = _relocations.Find(rel => rel.Offset == offset);
        int address;

        // Even though the pointers are 64 bit, it really should be impossible
        // for them to ever exceed 32-bit really, who's loading 4GB files???
        if (Platform.Is64Bit) address = (int)ReadInt64(offset);
        else address = ReadInt32(offset);

        // Check if the pointer is NULL or if it's just being re-mapped from the base address.
        if (address == 0 && (relocation == null || relocation.RelocationType == SlRelocationType.Null))
            return address;
        
        // Offset pointer by base
        gpu = relocation?.IsGpuPointer ?? false;
        return gpu ? GpuBase + address : Base + address;
    }
    
    /// <summary>
    ///     Reads a platform dependent pointer at an offset in the stream without advancing the cursor.
    /// </summary>
    /// <param name="offset">Offset to read data from</param>
    /// <returns>Pointer address</returns>
    public int ReadPointer(int offset)
    {
        return ReadPointer(offset, out _);
    }
    
    /// <summary>
    ///     Reads a floating point value at an offset in the stream without advancing the cursor.
    /// </summary>
    /// <param name="offset">Offset to read data from</param>
    /// <returns>Floating point value</returns>
    public float ReadFloat(int offset)
    {
        var span = _data.AsSpan(offset, sizeof(float));
        return Platform.IsBigEndian
            ? BinaryPrimitives.ReadSingleBigEndian(span)
            : BinaryPrimitives.ReadSingleLittleEndian(span);
    }

    /// <summary>
    ///     Reads a float2 at an offset in the stream without advancing the cursor.
    /// </summary>
    /// <param name="offset">Offset to read data from</param>
    /// <returns>Float2</returns>
    public Vector2 ReadFloat2(int offset)
    {
        float x = ReadFloat(offset), y = ReadFloat(offset + 4);
        return new Vector2(x, y);
    }

    /// <summary>
    ///     Reads a float3 at an offset in the stream without advancing the cursor.
    /// </summary>
    /// <param name="offset">Offset to read data from</param>
    /// <returns>Float3</returns>
    public Vector3 ReadFloat3(int offset)
    {
        float x = ReadFloat(offset), y = ReadFloat(offset + 4);
        float z = ReadFloat(offset + 8);
        return new Vector3(x, y, z);
    }

    /// <summary>
    ///     Reads a float4 at an offset in the stream without advancing the cursor.
    /// </summary>
    /// <param name="offset">Offset to read data from</param>
    /// <returns>Float4</returns>
    public Vector4 ReadFloat4(int offset)
    {
        float x = ReadFloat(offset), y = ReadFloat(offset + 4);
        float z = ReadFloat(offset + 8), w = ReadFloat(offset + 12);
        return new Vector4(x, y, z, w);
    }

    /// <summary>
    ///     Reads a 4x4 matrix at an offset in the stream without advancing the cursor.
    /// </summary>
    /// <param name="offset">Offset to read data from</param>
    /// <returns>Matrix</returns>
    public Matrix4x4 ReadMatrix(int offset)
    {
        // row, column
        var matrix = new Matrix4x4();
        for (int i = 0; i < 4; ++i)
        for (int j = 0; j < 4; ++j)
            matrix[i, j] = ReadFloat(offset + (i * 16) + (j * 4));
        return matrix;
    }

    /// <summary>
    ///     Reads a 4-byte magic string at an offset in the stream without advancing the cursor.
    /// </summary>
    /// <param name="offset">Offset to read data from</param>
    /// <returns>Magic value</returns>
    public string ReadMagic(int offset)
    {
        return Encoding.ASCII.GetString(_data.Array!, _data.Offset + offset, 4);
    }

    /// <summary>
    ///     Reads a string pointer at an offset in the stream without advancing the cursor.
    /// </summary>
    /// <param name="offset">Offset to read data from</param>
    /// <returns>String value</returns>
    public string ReadStringPointer(int offset)
    {
        offset = ReadPointer(offset);
        return offset == 0 ? string.Empty : ReadString(offset);
    }

    #endregion


    #region Stream-Based Reading

    /// <summary>
    ///     Reads a boolean value from the stream and advances the cursor.
    /// </summary>
    /// <param name="wide">Whether or not to read an integer as a boolean</param>
    /// <returns>Boolean value</returns>
    public bool ReadBoolean(bool wide = false)
    {
        // Booleans are often stored as integers
        if (wide) return ReadInt32() != 0;

        return _data[Position++] != 0;
    }

    /// <summary>
    ///     Reads a byte value from the stream and advances the cursor.
    /// </summary>
    /// <returns>Byte value</returns>
    public byte ReadInt8()
    {
        return _data[Position++];
    }

    /// <summary>
    ///     Reads a short value from the stream and advances the cursor.
    /// </summary>
    /// <returns>Short value</returns>
    public short ReadInt16()
    {
        short value = ReadInt16(Position);
        Position += sizeof(short);
        return value;
    }

    /// <summary>
    ///     Reads an integer value from the stream and advances the cursor.
    /// </summary>
    /// <returns>Integer value</returns>
    public int ReadInt32()
    {
        int value = ReadInt32(Position);
        Position += sizeof(int);
        return value;
    }

    /// <summary>
    ///     Reads a long value from the stream and advances the cursor.
    /// </summary>
    /// <returns>Long value</returns>
    public long ReadInt64()
    {
        long value = ReadInt64(Position);
        Position += sizeof(long);
        return value;
    }

    /// <summary>
    ///     Reads a platform dependent pointer from the stream and advances the cursor.
    /// </summary>
    /// <returns>Pointer address</returns>
    public int ReadPointer()
    {
        int pointer = ReadPointer(Position);
        Position += Platform.GetPointerSize();
        return pointer;
    }

    /// <summary>
    ///     Reads a platform dependent pointer from the stream and advances the cursor.
    /// </summary>
    /// <param name="gpu">Whether or not the pointer is into GPU data</param>
    /// <returns>Pointer address</returns>
    public int ReadPointer(out bool gpu)
    {
        int pointer = ReadPointer(Position, out gpu);
        Position += Platform.GetPointerSize();
        return pointer;
    }
    
    /// <summary>
    ///     Reads a floating point value from the stream and advances the cursor.
    /// </summary>
    /// <returns>Floating point value</returns>
    public float ReadFloat()
    {
        var span = _data.AsSpan(Position, sizeof(float));
        Position += sizeof(float);
        return Platform.IsBigEndian
            ? BinaryPrimitives.ReadSingleBigEndian(span)
            : BinaryPrimitives.ReadSingleLittleEndian(span);
    }

    /// <summary>
    ///     Reads a float2 from the stream and advances the cursor.
    /// </summary>
    /// <returns>Float2</returns>
    public Vector2 ReadFloat2()
    {
        float x = ReadFloat(), y = ReadFloat();
        return new Vector2(x, y);
    }

    /// <summary>
    ///     Reads a float3 from the stream and advances the cursor.
    /// </summary>
    /// <returns>Float3</returns>
    public Vector3 ReadFloat3()
    {
        float x = ReadFloat(), y = ReadFloat();
        float z = ReadFloat();
        return new Vector3(x, y, z);
    }

    /// <summary>
    ///     Reads a float4 from the stream and advances the cursor.
    /// </summary>
    /// <returns>Float4</returns>
    public Vector4 ReadFloat4()
    {
        float x = ReadFloat(), y = ReadFloat();
        float z = ReadFloat(), w = ReadFloat();
        return new Vector4(x, y, z, w);
    }

    /// <summary>
    ///     Reads a 16-byte float3 from the stream and advances the cursor.
    /// </summary>
    /// <returns>Float3</returns>
    public Vector3 ReadAlignedFloat3()
    {
        float x = ReadFloat(), y = ReadFloat(), z = ReadFloat();
        Position += 4;
        return new Vector3(x, y, z);
    }

    /// <summary>
    ///     Reads a matrix from the stream and advances the cursor.
    /// </summary>
    /// <returns>Matrix</returns>
    public Matrix4x4 ReadMatrix()
    {
        Matrix4x4 matrix = ReadMatrix(Position);
        Position += 64;
        return matrix;
    }

    public string ReadMagic()
    {
        string magic = Encoding.ASCII.GetString(_data.Array!, _data.Offset + Position, 4);
        Position += 4;
        return magic;
    }

    public string ReadStringPointer()
    {
        int offset = ReadPointer();
        return offset == 0 ? string.Empty : ReadString(offset);
    }
    
    public string ReadFixedString(int size)
    {
        string value = ReadString(Position);
        Position += size;
        return value;
    }
    
    #endregion
}