using System.Numerics;
using System.Runtime.Serialization;
using System.Text;
using SlLib.Extensions;
using SlLib.Resources.Database;
using SlLib.Resources.Scene;
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
    private readonly Dictionary<IResourceSerializable, ISaveBuffer> _references = [];

    /// <summary>
    ///     Pointer relocations table.
    /// </summary>
    public readonly List<SlResourceRelocation> Relocations = [];

    public int Version;
    public readonly SlPlatform Platform = SlPlatform.Win32;
    public bool IsSSR = false;
    
    public bool UseStringPool = false;
    public bool UseDepthSortedBuffers = false;
    
    private List<StringPoolEntry> _stringCache = [];
    private List<SortedReferenceEntry> _relocationCache = [];
    private List<DeferredPointerEntry> _deferredPointers = [];

    public ResourceSaveContext()
    {
        Version = SlPlatform.Win32.DefaultVersion;
    }
    
    public ResourceSaveContext(int version, bool ssr = false)
    {
        Version = version;
        IsSSR = ssr;
    }
    
    /// <summary>
    ///     Allocates and appends a slab.
    /// </summary>
    /// <param name="size">Size of slab to allocate</param>
    /// <param name="align">Address to align slab to</param>
    /// <param name="gpu">Whether or not to allocate on the GPU</param>
    /// <param name="parentSaveBuffer">Parent of this data</param>
    /// <returns>Allocated slab</returns>
    public ISaveBuffer Allocate(int size, int align = 4, bool gpu = false, ISaveBuffer? parentSaveBuffer = null)
    {
        Slab? parent = parentSaveBuffer switch
        {
            Slab s => s,
            Crumb c => c.Slab,
            _ => null
        };
        
        int address;
        if (gpu)
        {
            address = SlUtil.Align(_gpuSize, align);
            _gpuSize = address + size;
            _gpu = new Slab(_gpu, parent, address, size, align, true);
            return _gpu;
        }
        
        address = SlUtil.Align(_cpuSize, align);
        _cpuSize = address + size;
        _cpu = new Slab(_cpu, parent, address, size, align, false);
        return _cpu;
    }
    
    /// <summary>
    ///     Saves an array of objects to a buffer and writes the pointer to a given offset in an existing buffer.
    /// </summary>
    /// <param name="buffer"></param>
    /// <param name="list"></param>
    /// <param name="offset"></param>
    public void SaveObjectArray<T>(ISaveBuffer buffer, List<T> list, int offset, int align = 4) where T : IResourceSerializable
    {
        // No point allocating an array with no data
        if (list.Count == 0)
        {
            WriteInt32(buffer, 0, offset);
            return;
        }
        
        // All elements should be the same size, but the actual size calculation
        // method is an instance method, so, just grab it from the first.
        int stride = list.First().GetSizeForSerialization(Platform, Version);
        
        ISaveBuffer bufferData = SaveGenericPointer(buffer, offset, list.Count * stride, align: align);
        for (int i = 0; i < list.Count; ++i)
            SaveObject(bufferData, list[i], i * stride);
    }
    
    /// <summary>
    ///     Saves an array of references to a buffer and writes the pointer to a given offset in an existing buffer.
    /// </summary>
    /// <param name="buffer"></param>
    /// <param name="list"></param>
    /// <param name="offset"></param>
    public void SaveReferenceArray<T>(ISaveBuffer buffer, List<T> list, int offset, int align = 4) where T : IResourceSerializable
    {
        // No point allocating an array with no data
        if (list.Count == 0)
        {
            WriteInt32(buffer, 0, offset);
            return;
        }
        
        // All elements should be the same size, but the actual size calculation
        // method is an instance method, so, just grab it from the first.
        int stride = list.First().GetSizeForSerialization(Platform, Version);
        
        ISaveBuffer bufferData = SaveGenericPointer(buffer, offset, list.Count * stride, align: align);
        for (int i = 0; i < list.Count; ++i)
        {
            if (_references.ContainsKey(list[i]))
                throw new SerializationException("Referenced element cannot already be serialized!");
            SaveReference(bufferData, list[i], i * stride);   
        }
    }

    /// <summary>
    ///     Saves an array of pointers to a buffer and writes the pointer to a given offset in an existing buffer.
    /// </summary>
    /// <param name="buffer"></param>
    /// <param name="list"></param>
    /// <param name="offset"></param>
    public void SavePointerArray<T>(ISaveBuffer buffer, List<T> list, int offset, int elementAlignment = 4, int arrayAlignment = 4, bool deferred = false) where T : IResourceSerializable
    {
        // No point allocating an array with no data
        if (list.Count == 0)
        {
            WriteInt32(buffer, 0, offset);
            return;
        }
        
        ISaveBuffer pointerData = SaveGenericPointer(buffer, offset, list.Count * 4, align: arrayAlignment);
        for (int i = 0; i < list.Count; ++i)
            SavePointer(pointerData, list[i], i * 4, elementAlignment, deferred);
    }
    
    /// <summary>
    ///     Saves an object at an address in a buffer.
    /// </summary>
    /// <param name="buffer">Buffer to save object to</param>
    /// <param name="writable">Object to save</param>
    /// <param name="offset">Offset into buffer to save the object</param>
    public void SaveObject(ISaveBuffer buffer, IResourceSerializable writable, int offset)
    {
        ISaveBuffer crumb = buffer.At(offset, writable.GetSizeForSerialization(Platform, Version));
        writable.Save(this, crumb);
    }

    /// <summary>
    ///     Saves an object at an address and caches the reference to avoid re-serializing subsequent pointers.
    /// </summary>
    /// <param name="buffer">Buffer to write object to</param>
    /// <param name="writable">Object to write to buffer</param>
    /// <param name="offset">Offset into buffer to write object</param>
    /// <returns>Address of object</returns>
    public ISaveBuffer SaveReference(ISaveBuffer buffer, IResourceSerializable writable, int offset)
    {
        ISaveBuffer crumb = buffer.At(offset, writable.GetSizeForSerialization(Platform, Version));
        _references[writable] = crumb;
        writable.Save(this, crumb);
        
        var entries = _deferredPointers.FindAll(defer => defer.Reference == writable);
        foreach (DeferredPointerEntry entry in entries)
        {
            WritePointerAtOffset(entry.Buffer, entry.Offset, crumb.Address);
            _deferredPointers.Remove(entry);
        }
        
        return crumb;
    }

    /// <summary>
    ///     Saves an instance of an object and writes the pointer to a given offset in an existing buffer.
    /// </summary>
    /// <param name="buffer">Buffer to write pointer to</param>
    /// <param name="writable">Object to write</param>
    /// <param name="offset">Offset in buffer to write pointer to</param>
    /// <param name="align">Offset to align pointer to</param>
    public void SavePointer(ISaveBuffer buffer, IResourceSerializable? writable, int offset, int align = 4, bool deferred = false)
    {
        if (writable == null) return;

        if (deferred)
        {
            if (UseDepthSortedBuffers)
                throw new SerializationException("Cannot use depth sorted buffers with deferred pointers!");
            
            _deferredPointers.Add(new DeferredPointerEntry(buffer, offset, align, writable));
            return;
        }
        
        if (!_references.TryGetValue(writable, out ISaveBuffer? reference))
        {
            ISaveBuffer allocated = Allocate(writable.GetSizeForSerialization(Platform, Version), align, false, buffer);
            reference = SaveReference(allocated, writable, 0);
        }

        if (UseDepthSortedBuffers)
        {
            _relocationCache.Add(new SortedReferenceEntry(buffer, offset, reference));
            return;
        }

        WriteInt32(buffer, reference.Address, offset);
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
        ISaveBuffer allocated = Allocate(size, align, false, buffer);

        if (UseDepthSortedBuffers)
        {
            _relocationCache.Add(new SortedReferenceEntry(buffer, offset, allocated));
            return allocated;
        }
        
        WriteInt32(buffer, allocated.Address, offset);
        Relocations.Add(new SlResourceRelocation(buffer.Address + offset, SlRelocationType.Pointer));
        return allocated;
    }

    public void WritePointerAtOffset(ISaveBuffer buffer, int offset, int pointer)
    {
        if (UseDepthSortedBuffers)
            throw new SerializationException("Cannot write manual pointers when using depth sorted buffers!");
        WriteInt32(buffer, pointer, offset);
        Relocations.Add(new SlResourceRelocation(buffer.Address + offset, SlRelocationType.Pointer));
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

        ISaveBuffer allocated = Allocate(data.Count, align, gpu, buffer);
        data.CopyTo(allocated.Data);

        if (UseDepthSortedBuffers)
        {
            _relocationCache.Add(new SortedReferenceEntry(buffer, offset, allocated));
            return;
        }
        
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

        int rel = SlUtil.HashString(typeof(T).Name);
        rel &= ~0xf;
        rel |= SlRelocationType.Resource;

        WriteInt32(buffer, ptr.Id, offset);
        Relocations.Add(new SlResourceRelocation(buffer.Address + offset, rel));
    }

    public void SaveResourcePair<T>(ISaveBuffer buffer, SlResPtr<T> ptr, int pairDataOffset, int offset)
        where T : ISumoResource, new()
    {
        // No point in writing null resources.
        if (ptr.IsEmpty) return;
        
        int rel = SlUtil.HashString(typeof(T).Name);
        rel &= ~0xf;
        rel |= SlRelocationType.ResourcePair;
        
        WriteInt32(buffer, ptr.Id, offset);
        WriteInt32(buffer, pairDataOffset, offset + 4);
        Relocations.Add(new SlResourceRelocation(buffer.Address + offset, rel));
    }

    /// <summary>
    ///     Saves a string to the stream and writes the pointer to a given offset in an existing buffer.
    /// </summary>
    /// <param name="buffer">Buffer to write pointer to</param>
    /// <param name="value">String to write</param>
    /// <param name="offset">Offset in buffer to write pointer</param>
    public void WriteStringPointer(ISaveBuffer buffer, string value, int offset, bool allowEmptyString = false)
    {
        // dumb, but make sure we're allocating a null character, I guess
        // not sure if this is actually required, but it's consistent with the original files.
        if (string.IsNullOrEmpty(value))
        {
            if (allowEmptyString) value = string.Empty;
            else
            {
                WriteInt32(buffer, 0, offset);
                return;
            }
        }

        if (UseStringPool)
        {
            StringPoolEntry? entry = _stringCache.Find(c => c.Value == value);
            if (entry != null)
                entry.References.Add((buffer, offset));
            else
                _stringCache.Add(new StringPoolEntry((buffer, offset), value));
            
            return;
        }
        
        ISaveBuffer allocated = SaveGenericPointer(buffer, offset, value.Length + 1, 1);
        WriteString(allocated, value, 0);
    }

    /// <summary>
    ///     Saves a node UID reference to the stream and writes the pointer to a given offset in an existing buffer.
    /// </summary>
    /// <param name="buffer">Buffer to write pointer to</param>
    /// <param name="value">Node UID to write</param>
    /// <param name="offset">Offset in buffer to write pointer</param>
    public void WriteNodePointer(ISaveBuffer buffer, SeNodeBase? value, int offset)
    {
        if (value == null)
        {
            WriteInt32(buffer, 0, offset);
            return;
        }
        
        ISaveBuffer allocated = SaveGenericPointer(buffer, offset, 5, 1);
        WriteInt32(allocated, value.Uid, 0);
    }

    public void WriteBuffer(ISaveBuffer buffer, ArraySegment<byte> data, int offset)
    {
        if (data.Count == 0) return;
        data.CopyTo(buffer.Data.Array!, buffer.Data.Offset + offset);
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

    public void WriteMagic(ISaveBuffer buffer, string value, int offset)
    {
        Encoding.ASCII.GetBytes(value, 0, 4, buffer.Data.Array!, buffer.Data.Offset + offset);
    }

    public void WriteString(ISaveBuffer buffer, string value, int offset)
    {
        buffer.Data.WriteString(value, offset);
    }

    public void FlushDeferredPointersOfType<T>() where T : IResourceSerializable
    {
        if (_deferredPointers.Count == 0) return;
        var elements = _deferredPointers.FindAll(defer => defer.Reference is T);
        foreach (DeferredPointerEntry element in elements)
        {
            SavePointer(element.Buffer, element.Reference, element.Offset, element.Align);
            _deferredPointers.Remove(element);
        }
    }

    public void FlushDeferredPointers()
    {
        var list = new List<DeferredPointerEntry>(_deferredPointers);
        foreach (DeferredPointerEntry element in list)
            SavePointer(element.Buffer, element.Reference, element.Offset, element.Align);
        _deferredPointers.Clear();
    }

    private void SortSlabs()
    {
        if (!UseDepthSortedBuffers) return;

        // Re-assigning all addresses based on order
        Slab? root = _cpu;
        while (root is { Parent: not null })
            root = root.Parent;

        _cpuSize = 0; 
        _gpuSize = 0;

        if (root != null)
        {
            CalculateAddress(root);
            CalculateAddresses(root);   
        }

        foreach (SortedReferenceEntry entry in _relocationCache)
        {
            Slab slab = entry.Reference switch
            {
                Slab s => s,
                Crumb c => c.Slab,
                _ => throw new SerializationException("An internal error has occurred!")
            };
            
            WriteInt32(entry.Buffer, entry.Reference.Address, entry.Offset);
            int type = slab.IsGpuData ? SlRelocationType.GpuPointer : SlRelocationType.Pointer;
            Relocations.Add(new SlResourceRelocation(entry.Buffer.Address + entry.Offset, type));
        }
        
        _relocationCache.Clear();
        UseDepthSortedBuffers = false;
        
        return;

        void CalculateAddress(Slab slab)
        {
            if (slab.IsGpuData)
            {
                slab.Address = SlUtil.Align(_gpuSize, slab.Align);

                slab.Address = _gpuSize;
                
                _gpuSize = slab.Address + slab.Data.Count;
            }
            else
            {
                slab.Address = SlUtil.Align(_cpuSize, slab.Align);

                slab.Address = _cpuSize;
                
                _cpuSize = slab.Address + slab.Data.Count;
            }
        }
        
        void CalculateAddresses(Slab? slab)
        {
            if (slab == null) return;
            
            Slab? child = slab.FirstChild;
            while (child != null)
            {
                CalculateAddress(child);
                child = child.NextSibling;
            }
            
            child = slab.FirstChild;
            while (child != null)
            {
                CalculateAddresses(child);
                child = child.NextSibling;
            }
        }
    }

    public (byte[], byte[]) Flush(int align = 16)
    {
        FlushDeferredPointers();
        SortSlabs();
        if (UseStringPool && _stringCache.Count != 0)
        {
            _stringCache.Sort((a, b) => string.Compare(a.Value, b.Value, StringComparison.Ordinal));
            int size = _stringCache.Aggregate(0, (current, entry) => SlUtil.Align(current + entry.Value.Length + 1, 4));

            ISaveBuffer buffer = Allocate(size, align: 0x4);
            int offset = 0;
            foreach (StringPoolEntry entry in _stringCache)
            {
                WriteString(buffer, entry.Value, offset);
                foreach ((ISaveBuffer b, int o) in entry.References)
                    WritePointerAtOffset(b, o, buffer.Address + offset);
                offset = SlUtil.Align(offset + entry.Value.Length + 1, 4);
            }
            
            _stringCache.Clear();
        }
        
        byte[] cpu = FlushLinkedListInternal(_cpu, _cpuSize);
        byte[] gpu = FlushLinkedListInternal(_gpu, _gpuSize);

        return (cpu, gpu);

        byte[] FlushLinkedListInternal(Slab? slab, int size)
        {
            if (slab == null || size == 0) return Array.Empty<byte>();
            byte[] buffer = new byte[SlUtil.Align(size, align)];
            while (slab != null)
            {
                var span = new ArraySegment<byte>(buffer, slab.Address, slab.Data.Count);
                slab.Data.CopyTo(span);
                slab = slab.Previous;
            }

            return buffer;
        }
    }
    
    private class StringPoolEntry((ISaveBuffer buffer, int offset) reference, string value)
    {
        public List<(ISaveBuffer buffer, int offset)> References = [reference];
        public string Value = value;
    }

    private class SortedReferenceEntry(ISaveBuffer buffer, int offset, ISaveBuffer reference)
    {
        public ISaveBuffer Buffer = buffer;
        public int Offset = offset;
        public ISaveBuffer Reference = reference;
    }

    private class DeferredPointerEntry(ISaveBuffer buffer, int offset, int align, IResourceSerializable reference)
    {
        public ISaveBuffer Buffer = buffer;
        public int Offset = offset;
        public int Align = align;
        public IResourceSerializable Reference = reference;
    }
}