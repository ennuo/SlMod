using System.Buffers.Binary;
using System.IO.Compression;
using System.Runtime.Serialization;
using SlLib.Extensions;
using SlLib.Resources.Database;
using SlLib.Serialization;
using SlLib.Utilities;

namespace SlLib.SumoTool;

public class SiffFile
{
    /// <summary>
    ///     The chunks contained in this Siff file.
    /// </summary>
    private readonly List<SiffChunk> _chunks = [];

    /// <summary>
    ///     The GPU data used by this siff file.
    /// </summary>
    private ArraySegment<byte> _gpuData;
    
    /// <summary>
    ///     Information about the platform this file comes from.
    /// </summary>
    public readonly SlPlatformContext PlatformInfo;
    
    /// <summary>
    ///     Creates an empty Siff file targeting a platform.
    /// </summary>
    /// <param name="info">Platform information for siff file</param>
    public SiffFile(SlPlatformContext info)
    {
        PlatformInfo = info;
    }
    
    /// <summary>
    ///     Checks if a resource is contained in this Siff file.
    /// </summary>
    /// <param name="type">The type of resource to search for</param>
    /// <returns>True, if the resource is contained in the Siff file</returns>
    public bool HasResource(SiffResourceType type)
    {
        return _chunks.Exists(chunk => chunk.Type == type);
    }

    /// <summary>
    ///     Loads a resource from the Siff file.
    /// </summary>
    /// <param name="type">The resource type ID to load</param>
    /// <typeparam name="T">Type of resource class to load, must implement ILoadable</typeparam>
    /// <returns>Reference to resource</returns>
    public T LoadResource<T>(SiffResourceType type) where T : IResourceSerializable, new()
    {
        SiffChunk? chunk = _chunks.Find(chunk => chunk.Type == type);

        // It should be checked if the resource exists from the HasResource method,
        // so I'm just going to throw an exception to ignore the nullability checks.
        ArgumentNullException.ThrowIfNull(chunk);

        var context = new ResourceLoadContext(chunk.Data, _gpuData)
        {
            Platform = PlatformInfo.Platform,
            Version = PlatformInfo.Version,
            IsSSR = PlatformInfo.IsSSR
        };
        
        return context.LoadObject<T>();
    }

    /// <summary>
    ///     Sets a resource in the Siff file.
    /// </summary>
    /// <param name="resource">The resource to save</param>
    /// <param name="type">The resource type ID to save</param>
    /// <param name="overrideGpuData">Whether to override the current GPU file with data generated</param>
    public void SetResource(IResourceSerializable resource, SiffResourceType type, bool overrideGpuData = false)
    {
        var context = new ResourceSaveContext
        {
            IsSSR = PlatformInfo.IsSSR,
            UseStringPool = true
        };
        
        // TODO: Allow passing in platform
        ISaveBuffer buffer = context.Allocate(resource.GetSizeForSerialization(PlatformInfo.Platform, PlatformInfo.IsSSR ? -1 : PlatformInfo.Version));
        context.SaveReference(buffer, resource, 0);
        (byte[] cpu, byte[] gpu) = context.Flush();
        var relocations = context.Relocations.Select(r => r.Offset).ToList();
        relocations.Sort();
        
        // Push data to chunk already in file if it exists
        SiffChunk? chunk = _chunks.Find(c => c.Type == type);
        if (chunk == null)
        {
            chunk = new SiffChunk { Type = type };
            _chunks.Add(chunk);
        }

        chunk.Data = cpu;
        chunk.Relocations = relocations;
        
        if (overrideGpuData) _gpuData = gpu;
    }
    
    /// <summary>
    ///     Loads a siff resource from a set of buffers.
    /// </summary>
    /// <param name="context">Platform info</param>
    /// <param name="dat">Main data file</param>
    /// <param name="rel">Optional relocations file for non-KSiff resources</param>
    /// <param name="gpu">GPU data, if the siff resource relies on it</param>
    /// <param name="compressed">Whether the files are compressed</param>
    /// <returns>Parsed Siff file instance</returns>
    /// <exception cref="SerializationException">Thrown if any relocation chunk isn't found</exception>
    public static SiffFile Load(SlPlatformContext context, byte[] dat, byte[]? rel = null, byte[]? gpu = null, bool compressed = false)
    {
        var plat = context.Platform;
        
        if (compressed)
        {
            {
                using var stream = new MemoryStream(dat);
                using var decompressor = new ZLibStream(stream, CompressionMode.Decompress, false);

                Span<byte> scratch = stackalloc byte[4];
                decompressor.ReadExactly(scratch);
                int len = plat.ReadInt32(scratch);
                
                dat = new byte[len];
                decompressor.ReadExactly(dat);   
            }

            if (gpu != null)
            {
                using var stream = new MemoryStream(gpu);
                using var decompressor = new ZLibStream(stream, CompressionMode.Decompress, false);

                Span<byte> scratch = stackalloc byte[4];
                decompressor.ReadExactly(scratch);
                int len = plat.ReadInt32(scratch);
                
                gpu = new byte[len];
                decompressor.ReadExactly(gpu);
            }
        }
        
        SiffFile file = new(context);

        // GPU data is optional, generally only KSiff files will have them.
        if (gpu != null) file._gpuData = gpu;

        SiffResourceType type;
        ArraySegment<byte> data;

        int datOffset = 0, relOffset = 0;
        while (datOffset < dat.Length)
        {
            ReadChunk(dat, ref datOffset);
            var chunk = new SiffChunk
            {
                Type = type,
                Data = data
            };
            
            Console.WriteLine(type);
            
            // Read the relocations chunk, if we're reading a KSiff file,
            // the relocation chunk will be directly after, if it's a sumo tool
            // package, then it'll be in the separate relocation file.
            if (rel != null) ReadChunk(rel, ref relOffset);
            else ReadChunk(dat, ref datOffset);

            if (type != SiffResourceType.Relocations)
                throw new SerializationException("Expected relocation chunk!");

            var targetType = (SiffResourceType)data.ReadInt32(0);
            if (targetType != chunk.Type)
                throw new SerializationException("Relocation target chunk type doesn't match data chunk!");

            int offset = 4;
            // kind of nasty?
            while (plat.ReadInt16(data[offset..(offset += 4)]) == 1)
                chunk.Relocations.Add(plat.ReadInt32(data[offset..(offset += 4)]));
            
            file._chunks.Add(chunk);
        }

        return file;

        void ReadChunk(byte[] chunkFileData, ref int chunkPosition)
        {
            type = (SiffResourceType)chunkFileData.ReadInt32(chunkPosition); // always read as LE
            int chunkSize = plat.ReadInt32(chunkFileData, chunkPosition + 4);
            int dataSize = plat.ReadInt32(chunkFileData, chunkPosition + 8);
            data = new ArraySegment<byte>(chunkFileData, chunkPosition + 16, dataSize);
            chunkPosition += chunkSize;
        }
    }

    public void BuildKSiff(out byte[] dat, out byte[] gpu, bool compressed = false)
    {
        SlPlatform plat = PlatformInfo.Platform;
        
        int dataStreamSize = 0;
        foreach (SiffChunk chunk in _chunks)
        {
            dataStreamSize = SlUtil.Align(dataStreamSize + 0x10 + chunk.Data.Count, 0x100);
            
            int relDataSize = 0x4 + (chunk.Relocations.Count + 1) * 0x8;
            dataStreamSize = SlUtil.Align(dataStreamSize + 0x10 + relDataSize, 0x100);
        }

        dat = new byte[dataStreamSize];
        
        if (_gpuData.Count != 0)
        {
            if (compressed)
            {
                Span<byte> span = stackalloc byte[4];
                plat.WriteInt32(span, _gpuData.Count);
                
                using var ms = new MemoryStream(_gpuData.Count);
                using var compressor = new ZLibStream(ms, CompressionLevel.SmallestSize);
                compressor.Write(span);
                compressor.Write(_gpuData);
                compressor.Flush();
                
                gpu = ms.ToArray();
            }
            else
            {
                gpu = new byte[_gpuData.Count];
                _gpuData.CopyTo(gpu);
            }
        }
        else gpu = [];
        
        int datOffset = 0;
        foreach (SiffChunk chunk in _chunks)
        {
            int relDataSize = 0x4 + (chunk.Relocations.Count + 1) * 0x8;
            int datChunkSize = SlUtil.Align(0x10 + chunk.Data.Count, 0x100);
            int relChunkSize = SlUtil.Align(0x10 + relDataSize, 0x100);

            // Write data chunk to stream
            
            dat.WriteInt32((int)chunk.Type, datOffset); // always le
            plat.WriteInt32(dat, datChunkSize, datOffset + 0x4);
            plat.WriteInt32(dat, chunk.Data.Count, datOffset + 0x8);
            plat.WriteInt32(dat, 0x44332211, datOffset + 0xc); // Endian indicator
            chunk.Data.CopyTo(dat, datOffset + 0x10);

            datOffset += datChunkSize;
            
            // Write relocation chunk to stream
            dat.WriteInt32((int)SiffResourceType.Relocations, datOffset); // always le
            plat.WriteInt32(dat, relChunkSize, datOffset + 0x4);
            plat.WriteInt32(dat, relDataSize, datOffset + 0x8);
            plat.WriteInt32(dat, 0x44332211, datOffset + 0xc); // Endian indicator
            // Write relocation data
            plat.WriteInt32(dat, (int)chunk.Type, datOffset + 0x10);
            for (int i = 0; i < chunk.Relocations.Count; ++i)
            {
                int address = datOffset + 0x10 + 0x4 + i * 0x8;
                plat.WriteInt16(dat, 1, address); // Parser keeps reading relocations until this value is 0
                plat.WriteInt32(dat, chunk.Relocations[i], address + 4);
            }

            datOffset += relChunkSize;
        }

        if (compressed)
        {
            Span<byte> span = stackalloc byte[4];
            plat.WriteInt32(span, dat.Length);
                
            using var ms = new MemoryStream(dat.Length);
            using var compressor = new ZLibStream(ms, CompressionLevel.SmallestSize);
            compressor.Write(span);
            compressor.Write(dat);
            compressor.Flush();
            
            dat = ms.ToArray();
        }
    }

    /// <summary>
    ///     Builds this Siff file for use in sumo tool packages.
    /// </summary>
    /// <param name="dat">Output dat chunk file</param>
    /// <param name="rel">Output rel chunk file</param>
    /// <param name="gpu">Output gpu chunk file</param>
    public void BuildForSumoTool(out byte[] dat, out byte[] rel, out byte[] gpu)
    {
        SlPlatform plat = PlatformInfo.Platform;
        
        int relStreamSize = 0;
        int dataStreamSize = 0;
        foreach (SiffChunk chunk in _chunks)
        {
            dataStreamSize = SlUtil.Align(dataStreamSize + 0x10 + chunk.Data.Count, 0x40);

            int relDataSize = 0x4 + (chunk.Relocations.Count + 1) * 0x8;
            relStreamSize = SlUtil.Align(relStreamSize + 0x10 + relDataSize, 0x40);
        }

        dat = new byte[dataStreamSize];
        rel = new byte[relStreamSize];

        gpu = new byte[_gpuData.Count];
        if (_gpuData.Count != 0) _gpuData.CopyTo(gpu);

        int datOffset = 0, relOffset = 0;
        foreach (SiffChunk chunk in _chunks)
        {
            int relDataSize = 0x4 + (chunk.Relocations.Count + 1) * 0x8;
            int datChunkSize = SlUtil.Align(0x10 + chunk.Data.Count, 0x40);
            int relChunkSize = SlUtil.Align(0x10 + relDataSize, 0x40);

            // Write data chunk to stream
            dat.WriteInt32((int)chunk.Type, datOffset); // always le
            plat.WriteInt32(dat, datChunkSize, datOffset + 0x4);
            plat.WriteInt32(dat, chunk.Data.Count, datOffset + 0x8);
            plat.WriteInt32(dat, 0x44332211, datOffset + 0xc); // Endian indicator
            chunk.Data.CopyTo(dat, datOffset + 0x10);

            // Write relocation chunk to stream
            rel.WriteInt32((int)SiffResourceType.Relocations, relOffset); // always le
            plat.WriteInt32(rel, relChunkSize, relOffset + 0x4);
            plat.WriteInt32(rel, relDataSize, relOffset + 0x8);
            plat.WriteInt32(rel, 0x44332211, relOffset + 0xc); // Endian indicator
            // Write relocation data
            plat.WriteInt32(rel, (int)chunk.Type, relOffset + 0x10);
            for (int i = 0; i < chunk.Relocations.Count; ++i)
            {
                int address = relOffset + 0x10 + 0x4 + i * 0x8;
                plat.WriteInt16(rel, 1, address); // Parser keeps reading relocations until this value is 0
                plat.WriteInt32(rel, chunk.Relocations[i], address + 4);
            }

            datOffset += datChunkSize;
            relOffset += relChunkSize;
        }
    }
}