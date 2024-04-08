using System.Numerics;
using SlLib.Extensions;
using SlLib.Resources.Database;
using SlLib.Utilities;

namespace SlLib.Serialization;

public class ResourceSaveContext
{
    /// <summary>
    ///     The previous slab in the list for CPU data.
    /// </summary>
    private Slab? _cpu;

    /// <summary>
    ///     The current size of the CPU buffer.
    /// </summary>
    private int _cpuSize;

    /// <summary>
    ///     The previous slab in the list for GPU data.-
    /// </summary>
    private Slab? _gpu;

    /// <summary>
    ///     The current size of the GPU buffer.
    /// </summary>
    private int _gpuSize;

    /// <summary>
    ///     Cached serialized addresses by object.
    /// </summary>
    private readonly Dictionary<IWritable, int> _references = [];

    /// <summary>
    ///     Pointer relocations table.
    /// </summary>
    public readonly List<SlResourceRelocation> Relocations = [];

    /// <summary>
    ///     Allocates and appends a slab.
    /// </summary>
    /// <param name="size">Size of slab to allocate</param>
    /// <param name="align">Address to align slab to</param>
    /// <param name="gpu">Whether or not to allocate on the GPU</param>
    /// <returns>Allocated slab</returns>
    public ISaveBuffer Allocate(int size, int align = 4, bool gpu = false)
    {
        int address;
        if (gpu)
        {
            address = SlUtil.Align(_gpuSize, align);
            _gpuSize = address + size;
            _gpu = new Slab(_gpu, address, size, true);
            return _gpu;
        }

        address = SlUtil.Align(_cpuSize, align);
        _cpuSize = address + size;
        _cpu = new Slab(_cpu, address, size, false);
        return _cpu;
    }

    /// <summary>
    ///     Saves an object at an address in a buffer.
    /// </summary>
    /// <param name="buffer">Buffer to save object to</param>
    /// <param name="writable">Object to save</param>
    /// <param name="offset">Offset into buffer to save the object</param>
    public void SaveObject(ISaveBuffer buffer, IWritable writable, int offset)
    {
        ISaveBuffer crumb = buffer.At(offset, writable.GetAllocatedSize());
        writable.Save(this, crumb);
    }

    /// <summary>
    ///     Saves an object at an address and caches the reference to avoid re-serializing subsequent pointers.
    /// </summary>
    /// <param name="buffer">Buffer to write object to</param>
    /// <param name="writable">Object to write to buffer</param>
    /// <param name="offset">Offset into buffer to write object</param>
    /// <returns>Address of object</returns>
    public int SaveReference(ISaveBuffer buffer, IWritable writable, int offset)
    {
        // Cache address before serializing for structures that reference themselves
        int address = buffer.Address + offset;
        _references[writable] = address;

        SaveObject(buffer, writable, offset);
        return address;
    }

    /// <summary>
    ///     Saves an instance of an object and writes the pointer to a given offset in an existing buffer.
    /// </summary>
    /// <param name="buffer">Buffer to write pointer to</param>
    /// <param name="writable">Object to write</param>
    /// <param name="offset">Offset in buffer to write pointer to</param>
    public void SavePointer(ISaveBuffer buffer, IWritable? writable, int offset)
    {
        if (writable == null) return;

        if (!_references.TryGetValue(writable, out int address))
        {
            ISaveBuffer allocated = Allocate(writable.GetAllocatedSize());
            address = SaveReference(allocated, writable, 0);
        }

        WriteInt32(buffer, address, offset);
        Relocations.Add(new SlResourceRelocation(buffer.Address + offset, SlRelocationType.Pointer));
    }

    /// <summary>
    ///     Allocates a buffer with a given size and writes the pointer to a given offset in an existing buffer.
    /// </summary>
    /// <param name="buffer">Buffer to write pointer to</param>
    /// <param name="offset">Offset in buffer to write pointer to</param>
    /// <param name="size">Size of buffer to allocate</param>
    /// <param name="align">Address to align buffer to</param>
    /// <returns>Allocated slab</returns>
    public ISaveBuffer SaveGenericPointer(ISaveBuffer buffer, int offset, int size, int align = 4)
    {
        ISaveBuffer allocated = Allocate(size, align);
        WriteInt32(buffer, allocated.Address, offset);
        Relocations.Add(new SlResourceRelocation(buffer.Address + offset, SlRelocationType.Pointer));
        return allocated;
    }

    /// <summary>
    ///     Adds a buffer to the stream and writes the pointer to a given offset in an existing buffer.
    /// </summary>
    /// <param name="buffer">Buffer to write pointer to</param>
    /// <param name="data">Data buffer to save</param>
    /// <param name="offset">Offset in buffer to write pointer to</param>
    /// <param name="align">Address to align data buffer to</param>
    /// <param name="gpu">Whether or not to allocate on the GPU</param>
    public void SaveBufferPointer(ISaveBuffer buffer, ArraySegment<byte> data, int offset, int align = 4,
        bool gpu = false)
    {
        if (data.Count == 0) return;

        ISaveBuffer allocated = Allocate(data.Count, align, gpu);
        data.CopyTo(allocated.Data);
        WriteInt32(buffer, allocated.Address, offset);

        int type = gpu ? SlRelocationType.GpuPointer : SlRelocationType.Pointer;
        Relocations.Add(new SlResourceRelocation(buffer.Address + offset, type));
    }

    /// <summary>
    ///     Saves a resource reference to a buffer.
    /// </summary>
    /// <param name="buffer">Buffer to write resource reference to</param>
    /// <param name="ptr">Resource reference to write</param>
    /// <param name="offset">Offset in buffer to write pointer</param>
    /// <typeparam name="T">Type of resource to save, must inherit ISumoResource</typeparam>
    public void SaveResource<T>(ISaveBuffer buffer, SlResPtr<T> ptr, int offset) where T : ISumoResource, new()
    {
        // No point in writing null resources.
        if (ptr.IsEmpty) return;

        int rel = (int)SlUtil.ResourceId(typeof(T).Name);
        rel &= ~0xf;
        rel |= SlRelocationType.Resource;

        WriteInt32(buffer, ptr.Id, offset);
        Relocations.Add(new SlResourceRelocation(buffer.Address + offset, rel));
    }

    /// <summary>
    ///     Saves a string to the stream and writes the pointer to a given offset in an existing buffer.
    /// </summary>
    /// <param name="buffer">Buffer to write pointer to</param>
    /// <param name="value">String to write</param>
    /// <param name="offset">Offset in buffer to write pointer</param>
    public void WriteStringPointer(ISaveBuffer buffer, string value, int offset)
    {
        ISaveBuffer allocated = SaveGenericPointer(buffer, offset, value.Length + 1, 1);
        WriteString(allocated, value, 0);
    }

    public void WriteBoolean(ISaveBuffer buffer, bool value, int offset, bool wide = false)
    {
        // Booleans are often stored as integers
        if (wide)
        {
            buffer.Data.WriteInt32(value ? 1 : 0, offset);
            return;
        }

        buffer.Data.WriteBoolean(value, offset);
    }

    public void WriteInt8(ISaveBuffer buffer, byte value, int offset)
    {
        buffer.Data.WriteInt8(value, offset);
    }

    public void WriteInt16(ISaveBuffer buffer, short value, int offset)
    {
        buffer.Data.WriteInt16(value, offset);
    }

    public void WriteInt32(ISaveBuffer buffer, int value, int offset)
    {
        buffer.Data.WriteInt32(value, offset);
    }

    public void WriteFloat(ISaveBuffer buffer, float value, int offset)
    {
        buffer.Data.WriteFloat(value, offset);
    }

    public void WriteFloat2(ISaveBuffer buffer, Vector2 value, int offset)
    {
        buffer.Data.WriteFloat2(value, offset);
    }

    public void WriteFloat3(ISaveBuffer buffer, Vector3 value, int offset)
    {
        buffer.Data.WriteFloat3(value, offset);
    }

    public void WriteFloat4(ISaveBuffer buffer, Vector4 value, int offset)
    {
        buffer.Data.WriteFloat4(value, offset);
    }

    public void WriteMatrix(ISaveBuffer buffer, Matrix4x4 value, int offset)
    {
        buffer.Data.WriteMatrix(value, offset);
    }

    public void WriteString(ISaveBuffer buffer, string value, int offset)
    {
        buffer.Data.WriteString(value, offset);
    }

    public (byte[], byte[]) Flush()
    {
        byte[] cpu = FlushLinkedListInternal(_cpu, _cpuSize);
        byte[] gpu = FlushLinkedListInternal(_gpu, _gpuSize);

        return (cpu, gpu);

        byte[] FlushLinkedListInternal(Slab? slab, int size)
        {
            if (slab == null || size == 0) return Array.Empty<byte>();
            byte[] buffer = new byte[SlUtil.Align(size, 0x10)];
            while (slab != null)
            {
                var span = new ArraySegment<byte>(buffer, slab.Address, slab.Data.Count);
                slab.Data.CopyTo(span);
                slab = slab.Previous;
            }

            return buffer;
        }
    }
}