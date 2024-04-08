using System.Numerics;
using System.Text;
using SlLib.Extensions;
using SlLib.Resources.Database;

namespace SlLib.Serialization;

public class ResourceLoadContext
{
    /// <summary>
    ///     Resource data.
    /// </summary>
    private readonly ArraySegment<byte> _data;

    /// <summary>
    ///     Optional resource database for linking other resources.
    /// </summary>
    private readonly SlResourceDatabase? _database;

    /// <summary>
    ///     Resource GPU data.
    /// </summary>
    private readonly ArraySegment<byte> _gpuData;

    /// <summary>
    ///     Cached serialized references by address.
    /// </summary>
    private readonly Dictionary<int, object> _references = [];

    /// <summary>
    ///     Pointer relocations table.
    /// </summary>
    private readonly List<SlResourceRelocation> _relocations;

    public ResourceLoadContext(ArraySegment<byte> cpuData, ArraySegment<byte> gpuData = default)
    {
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
    public int Version { get; }

    /// <summary>
    ///     Reads an object at an address.
    /// </summary>
    /// <param name="offset">Offset of object</param>
    /// <typeparam name="T">Type of object to load, must implement ILoadable</typeparam>
    /// <returns>Loaded object instance</returns>
    public T LoadObject<T>(int offset) where T : ILoadable, new()
    {
        T value = new();
        value.Load(this, offset);
        return value;
    }

    /// <summary>
    ///     Reads an object at an address and caches the reference for future lookups.
    /// </summary>
    /// <param name="offset">Offset of object</param>
    /// <typeparam name="T">Type of object to load, must implement ILoadable</typeparam>
    /// <returns>Reference to object</returns>
    public T LoadReference<T>(int offset) where T : ILoadable, new()
    {
        if (_references.TryGetValue(offset, out object? reference)) return (T)reference;
        reference = LoadObject<T>(offset);
        _references[offset] = reference;
        return (T)reference;
    }

    /// <summary>
    ///     Reads a pointer at an address and returns a reference to the object.
    /// </summary>
    /// <param name="offset">Offset of pointer to object</param>
    /// <typeparam name="T">Type of object to load, must implement ILoadable</typeparam>
    /// <returns>Reference to object</returns>
    public T? LoadPointer<T>(int offset) where T : ILoadable, new()
    {
        int address = Base + ReadInt32(offset);
        return address == 0 ? default : LoadReference<T>(address);
    }

    /// <summary>
    ///     Reads a pointer to a buffer and returns a view into it.
    /// </summary>
    /// <param name="offset">Offset of pointer to buffer</param>
    /// <param name="size">Size of buffer</param>
    /// <param name="gpu">Whether or not the buffer came from GPU data</param>
    /// <returns>Slice of buffer</returns>
    public ArraySegment<byte> LoadBufferPointer(int offset, int size, out bool gpu)
    {
        gpu = false;

        // Don't bother reading the pointer if we're not reading anything
        if (size == 0) return default;

        int address = Base + ReadInt32(offset);

        // Check if this pointer is meant to be into GPU data.
        SlResourceRelocation? relocation = _relocations.Find(relocation => relocation.Offset == offset);
        gpu = relocation?.IsGpuPointer ?? false;

        // GPU buffer pointer can be based at 0, but CPU pointers shouldn't be, treat them as null.
        if (!gpu && address == 0) return default;

        int start = address;
        int end = start + size;

        return gpu ? _gpuData[start..end] : _data[start..end];
    }

    /// <summary>
    ///     Reads a pointer to a GPU buffer and returns a view into it.
    /// </summary>
    /// <param name="offset">Offset of pointer to buffer</param>
    /// <param name="size">Size of buffer</param>
    /// <returns>Slice of buffer</returns>
    public ArraySegment<byte> LoadGpuBufferPointer(int offset, int size)
    {
        if (size == 0) return default;
        int address = GpuBase + offset;

        int start = address;
        int end = start + size;

        return _gpuData[start..end];
    }

    /// <summary>
    ///     Reads a resource pointer at an address and returns a reference to it.
    /// </summary>
    /// <param name="offset">Offset of resource pointer</param>
    /// <typeparam name="T">Type of resource to load, must implement ISumoResource</typeparam>
    /// <returns>Reference to resource</returns>
    public SlResPtr<T> LoadResourcePointer<T>(int offset) where T : ISumoResource, new()
    {
        int id = Base + ReadInt32(offset);
        return LoadResource<T>(id);
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

    public bool ReadBoolean(int offset, bool wide = false)
    {
        // Booleans are often stored as integers
        if (wide) return ReadInt32(offset) != 0;

        return _data.Array!.ReadBoolean(_data.Offset + offset);
    }

    public byte ReadInt8(int offset)
    {
        return _data[offset];
    }

    public short ReadInt16(int offset)
    {
        return _data.Array!.ReadInt16(_data.Offset + offset);
    }

    public int ReadInt32(int offset)
    {
        return _data.Array!.ReadInt32(_data.Offset + offset);
    }

    public float ReadFloat(int offset)
    {
        return _data.Array!.ReadFloat(_data.Offset + offset);
    }

    public Vector2 ReadFloat2(int offset)
    {
        return _data.Array!.ReadFloat2(_data.Offset + offset);
    }

    public Vector3 ReadFloat3(int offset)
    {
        return _data.Array!.ReadFloat3(_data.Offset + offset);
    }

    public Vector4 ReadFloat4(int offset)
    {
        return _data.Array!.ReadFloat4(_data.Offset + offset);
    }

    public Matrix4x4 ReadMatrix(int offset)
    {
        return _data.Array!.ReadMatrix(_data.Offset + offset);
    }

    public string ReadMagic(int offset)
    {
        return Encoding.ASCII.GetString(_data.Array!, _data.Offset + offset, 4);
    }

    public string ReadString(int offset)
    {
        return _data.Array!.ReadString(_data.Offset + offset);
    }

    public string ReadStringPointer(int offset)
    {
        return ReadString(ReadInt32(offset));
    }
}