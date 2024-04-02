using System.Collections;
using System.Runtime.Serialization;
using SlLib.Extensions;
using SlLib.Serialization;

namespace SlLib.Resources.Database;

public class SlResourceDatabase : IEnumerable<SlResourceChunk>
{
    /// <summary>
    ///     The chunks held by this database.
    /// </summary>
    private readonly List<SlResourceChunk> _chunks = [];

    private readonly Dictionary<int, ISumoResource> _loadCache = [];

    /// <summary>
    ///     Loads a chunk database by path.
    /// </summary>
    /// <param name="cpuFilePath">Path to CPU file data</param>
    /// <param name="gpuFilePath">Path to GPU file data</param>
    /// <exception cref="FileNotFoundException">Thrown if one of the files are not found</exception>
    public SlResourceDatabase(string cpuFilePath, string gpuFilePath)
    {
        if (!File.Exists(cpuFilePath))
            throw new FileNotFoundException($"CPU file at {cpuFilePath} was not found!");
        if (!File.Exists(gpuFilePath))
            throw new FileNotFoundException($"GPU file at {gpuFilePath} was not found!");

        byte[] cpuData = File.ReadAllBytes(cpuFilePath);
        byte[] gpuData = File.ReadAllBytes(gpuFilePath);

        Load(cpuData, gpuData);
    }

    /// <summary>
    ///     Loads a chunk database from buffers.
    /// </summary>
    /// <param name="cpuData">CPU data buffer</param>
    /// <param name="gpuData">GPU data buffer</param>
    public SlResourceDatabase(byte[] cpuData, byte[] gpuData)
    {
        Load(cpuData, gpuData);
    }

    public IEnumerator<SlResourceChunk> GetEnumerator()
    {
        return _chunks.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    public T? LoadResource<T>(int id) where T : ISumoResource, new()
    {
        // If this resource was already loaded, use that reference instead.
        if (_loadCache.TryGetValue(id, out ISumoResource? value)) return (T)value;

        SlResourceChunk? chunk = _chunks.Find(chunk => chunk.IsResource && chunk.Id == id);
        if (chunk == null) return default;
        var context = new ResourceLoadContext(this, chunk);

        T resource = new();
        resource.Load(context, 0);
        _loadCache[id] = resource;

        return resource;
    }

    /// <summary>
    ///     Parses chunk database.
    /// </summary>
    /// <param name="cpuData">CPU data buffer</param>
    /// <param name="gpuData">GPU data buffer</param>
    /// <exception cref="SerializationException">Thrown if an error occurs while reading chunks</exception>
    private void Load(byte[] cpuData, byte[] gpuData)
    {
        int offset = 0, gpuOffset = 0;
        while (offset < cpuData.Length)
        {
            int chunkSize = cpuData.ReadInt32(offset + 8);
            int dataSize = cpuData.ReadInt32(offset + 12);
            int gpuChunkSize = cpuData.ReadInt32(offset + 16);
            int gpuDataSize = cpuData.ReadInt32(offset + 20);

            var chunk = new SlResourceChunk
            {
                Type = (SlResourceType)cpuData.ReadInt32(offset),
                Version = cpuData.ReadInt32(offset + 4),
                Data = new ArraySegment<byte>(cpuData, offset + 0x20, dataSize),
                GpuData = new ArraySegment<byte>(gpuData, gpuOffset, gpuDataSize),
                IsResource = cpuData.ReadBoolean(offset + 28)
            };

            // Cache the name and ID of the chunk from the header
            if (chunk.IsResource)
            {
                chunk.Id = cpuData.ReadInt32(offset + 32);
                chunk.Name =
                    cpuData.ReadString(cpuData.ReadInt32(offset + (chunk.Version >= SlFileVersion.Android ? 40 : 36)));
            }
            else
            {
                chunk.Name = cpuData.ReadString(cpuData.ReadInt32(offset + 60));
            }

            // Skip to the next chunk so we can read the relocation data
            offset += chunkSize;
            gpuOffset += gpuChunkSize;

            if (cpuData.ReadInt32(offset) != 0x0eb411b1)
                throw new SerializationException("Expected relocation chunk!");
            chunkSize = cpuData.ReadInt32(offset + 8);

            // The relocation chunk is just an array of (offset, value) pairs
            int numRelocations = cpuData.ReadInt32(offset + 32);
            for (int i = 0; i < numRelocations; ++i)
            {
                int address = offset + 32 + 4 + i * 8;
                chunk.Relocations.Add(new SlResourceRelocation(cpuData.ReadInt32(address),
                    cpuData.ReadInt32(address + 4)));
            }

            offset += chunkSize;

            _chunks.Add(chunk);
        }
    }
}