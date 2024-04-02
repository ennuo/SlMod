using System.Numerics;
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

    /// <summary>
    ///     Optional resource database for linking other resources.
    /// </summary>
    private readonly SlResourceDatabase? Database;

    /// <summary>
    ///     Offsets for all pointers.
    /// </summary>
    public int Base = 0;

    /// <summary>
    ///     Constructs a resource load context for a resource database chunk.
    /// </summary>
    /// <param name="database">The database that owns this chunk</param>
    /// <param name="chunk">The chunk to load</param>
    public ResourceLoadContext(SlResourceDatabase database, SlResourceChunk chunk)
    {
        Database = database;

        Version = chunk.Version;
        _relocations = chunk.Relocations;
        _data = chunk.Data;
        _gpuData = chunk.GpuData;
    }

    /// <summary>
    ///     The version of the chunk being loaded.
    /// </summary>
    public int Version { get; }

    /// <summary>
    ///     Reads an object at an address.
    /// </summary>
    /// <param name="offset">Offset of object</param>
    /// <typeparam name="T">Type of object to load, must inherit ILoadable</typeparam>
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
    /// <typeparam name="T">Type of object to load, must inherit ILoadable</typeparam>
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
    /// <typeparam name="T">Type of object to load, must inherit ILoadable</typeparam>
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
    /// <returns>Slice of buffer</returns>
    public ArraySegment<byte> LoadBufferPointer(int offset, int size)
    {
        // Don't bother reading the pointer if we're not reading anything
        if (size == 0) return default;

        int address = Base + ReadInt32(offset);

        SlResourceRelocation? relocation = _relocations.Find(relocation => relocation.Offset == offset);
        bool isGpuPointer = relocation?.IsGpuPointer ?? false;

        // GPU buffer pointer can be based at 0, but CPU pointers shouldn't be, treat them as null.
        if (!isGpuPointer && address == 0) return default;

        int start = address;
        int end = start + size;

        return isGpuPointer ? _gpuData[start..end] : _data[start..end];
    }

    /// <summary>
    ///     Loads a resource reference from the database.
    /// </summary>
    /// <param name="id">Unique identifier of the resource to load</param>
    /// <typeparam name="T">Type of resource to load, must inherit ISumoResource</typeparam>
    /// <returns>Reference to resource</returns>
    public T? LoadResource<T>(int id) where T : ISumoResource, new()
    {
        return id == 0 || Database == null ? default : Database.LoadResource<T>(id);
    }

    public bool ReadBoolean(int offset)
    {
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

    public string ReadString(int offset)
    {
        return _data.Array!.ReadString(_data.Offset + offset);
    }

    public string ReadStringPointer(int offset)
    {
        return ReadString(ReadInt32(offset));
    }
}