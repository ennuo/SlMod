using System.Runtime.Serialization;
using SlLib.Extensions;
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
    public T LoadResource<T>(SiffResourceType type) where T : ILoadable, new()
    {
        SiffChunk? chunk = _chunks.Find(chunk => chunk.Type == type);

        // It should be checked if the resource exists from the HasResource method,
        // so I'm just going to throw an exception to ignore the nullability checks.
        ArgumentNullException.ThrowIfNull(chunk);

        var context = new ResourceLoadContext(chunk.Data, _gpuData);
        return context.LoadObject<T>(0);
    }

    /// <summary>
    ///     Sets a resource in the Siff file.
    /// </summary>
    /// <param name="resource">The resource to save</param>
    /// <param name="type">The resource type ID to save</param>
    public void SetResource(IWritable resource, SiffResourceType type)
    {
        var context = new ResourceSaveContext();
        ISaveBuffer buffer = context.Allocate(resource.GetAllocatedSize());
        context.SaveReference(buffer, resource, 0);
        (byte[] cpu, byte[] _) = context.Flush();
        var relocations = context.Relocations.Select(r => r.Offset).ToList();

        // Push data to chunk already in file if it exists
        SiffChunk? chunk = _chunks.Find(c => c.Type == type);
        if (chunk == null)
        {
            chunk = new SiffChunk { Type = type };
            _chunks.Add(chunk);
        }

        chunk.Data = cpu;
        chunk.Relocations = relocations;
    }

    /// <summary>
    ///     Loads a siff resource from a set of buffers.
    /// </summary>
    /// <param name="dat">Main data file</param>
    /// <param name="rel">Optional relocations file for non-KSiff resources</param>
    /// <param name="gpu">GPU data, if the siff resource relies on it</param>
    /// <returns>Parsed Siff file instance</returns>
    /// <exception cref="SerializationException">Thrown if any relocation chunk isn't found</exception>
    public static SiffFile Load(byte[] dat, byte[]? rel = null, byte[]? gpu = null)
    {
        SiffFile file = new();

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
            while (data.ReadInt16((offset += 4) - 4) == 1)
                chunk.Relocations.Add(data.ReadInt32((offset += 4) - 4));

            file._chunks.Add(chunk);
        }

        return file;

        void ReadChunk(byte[] chunkFileData, ref int chunkPosition)
        {
            type = (SiffResourceType)chunkFileData.ReadInt32(chunkPosition);
            int chunkSize = chunkFileData.ReadInt32(chunkPosition + 4);
            int dataSize = chunkFileData.ReadInt32(chunkPosition + 8);
            data = new ArraySegment<byte>(chunkFileData, chunkPosition + 16, dataSize);
            chunkPosition += chunkSize;
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
            dat.WriteInt32((int)chunk.Type, datOffset);
            dat.WriteInt32(datChunkSize, datOffset + 0x4);
            dat.WriteInt32(chunk.Data.Count, datOffset + 0x8);
            dat.WriteInt32(0x44332211, datOffset + 0xc); // Endian indicator
            chunk.Data.CopyTo(dat, datOffset + 0x10);

            // Write relocation chunk to stream
            rel.WriteInt32((int)SiffResourceType.Relocations, relOffset);
            rel.WriteInt32(relChunkSize, relOffset + 0x4);
            rel.WriteInt32(relDataSize, relOffset + 0x8);
            rel.WriteInt32(0x44332211, relOffset + 0xc); // Endian indicator
            // Write relocation data
            rel.WriteInt32((int)chunk.Type, relOffset + 0x10);
            for (int i = 0; i < chunk.Relocations.Count; ++i)
            {
                int address = relOffset + 0x10 + 0x4 + i * 0x8;
                rel.WriteInt16(1, address); // Parser keeps reading relocations until this value is 0
                rel.WriteInt32(chunk.Relocations[i], address + 4);
            }

            datOffset += datChunkSize;
            relOffset += relChunkSize;
        }
    }
}