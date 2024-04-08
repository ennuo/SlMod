using System.Buffers.Binary;
using System.Runtime.Serialization;
using SlLib.Extensions;
using SlLib.Resources.Scene;
using SlLib.Serialization;
using SlLib.Utilities;

namespace SlLib.Resources.Database;

public class SlResourceDatabase
{
    /// <summary>
    ///     The chunks held by this database.
    /// </summary>
    private readonly List<SlResourceChunk> _chunks = [];

    /// <summary>
    ///     Cache of already loaded resources to prevent re-serialization.
    /// </summary>
    private readonly Dictionary<int, ISumoResource> _loadCache = [];

    /// <summary>
    ///     Cache of already loaded nodes to prevent re-serialization.
    /// </summary>
    private readonly Dictionary<int, SeNodeBase> _nodeCache = [];

    /// <summary>
    ///     Adds a resource to the database, overriding if one with the same ID already exists.
    /// </summary>
    /// <param name="resource">Resource to save to database</param>
    /// <typeparam name="T">Type of resource, must implement ISumoResource and IWritable</typeparam>
    public void AddResource<T>(T resource) where T : ISumoResource, IWritable
    {
        var context = new ResourceSaveContext();
        ISaveBuffer slab = context.Allocate(resource.GetAllocatedSize());
        context.SaveReference(slab, resource, 0);

        (byte[] cpu, byte[] gpu) = context.Flush();
        var relocations = context.Relocations;

        SlResourceType type = SlUtil.ResourceId(typeof(T).Name);

        // Push data to chunk already in database if it exists
        SlResourceChunk? chunk = _chunks.Find(c => c.Type == type && c.Id == resource.Header.Id);
        if (chunk == null)
        {
            chunk = new SlResourceChunk(type, SlFileVersion.Windows, cpu, gpu, true);

            // Use relocations to figure out the first index this resource can be placed.
            // By default, place it before the first resource with the same type.
            int index = _chunks.FindIndex(c => c.Type == type);

            // Go through all relocations, if any of the indices are farther in the database,
            // place them there instead.
            foreach (SlResourceRelocation relocation in relocations)
            {
                if (!relocation.IsResourcePointer) continue;

                int id = cpu.ReadInt32(relocation.Offset);
                if (id == 0) continue;

                // If the resource referenced is after our current index,
                // we need to move our own index upwards.
                int referenceIndex = _chunks.FindIndex(c => c.IsResource && c.Id == id);
                if (referenceIndex > index)
                    index = referenceIndex + 1;
            }

            // For the cases where the first index is -1
            if (index < 0) index = 0;

            _chunks.Insert(index, chunk);
        }

        chunk.Data = cpu;
        chunk.GpuData = gpu;
        chunk.Relocations = relocations;

        // Update the load cache to reflect the new resource instance
        _loadCache[chunk.Id] = resource;
    }

    /// <summary>
    ///     Gets all nodes of a specified type.
    /// </summary>
    /// <typeparam name="T">Node data type, must extend SeNodeBase and implement ILoadable</typeparam>
    /// <returns>List of nodes</returns>
    public List<T> GetNodesOfType<T>() where T : SeNodeBase, ILoadable, new()
    {
        SlResourceType type = SlUtil.ResourceId(typeof(T).Name);
        var chunks = _chunks.FindAll(chunk => chunk.Type == type);
        return chunks.Select(LoadNodeInternal<T>).ToList();
    }

    /// <summary>
    ///     Gets a node that matches a partial path.
    /// </summary>
    /// <param name="path">Partial path of node to find</param>
    /// <typeparam name="T">Node data type, must extend SeNodeBase and implement ILoadable</typeparam>
    /// <returns>Node, if found</returns>
    public T? FindNodeByPartialName<T>(string path) where T : SeNodeBase, ILoadable, new()
    {
        SlResourceType type = SlUtil.ResourceId(typeof(T).Name);
        SlResourceChunk? chunk = _chunks.Find(chunk => chunk.Type == type && chunk.Name.Contains(path));
        return chunk == null ? default : LoadNodeInternal<T>(chunk);
    }

    /// <summary>
    ///     Gets a resource that matches a partial path.
    /// </summary>
    /// <param name="path">Partial path of resource to find</param>
    /// <typeparam name="T">Type of resource, must implement ISumoResource</typeparam>
    /// <returns>Resource, if found</returns>
    public T? FindResourceByPartialName<T>(string path) where T : ISumoResource, new()
    {
        SlResourceType type = SlUtil.ResourceId(typeof(T).Name);
        SlResourceChunk? chunk = _chunks.Find(chunk => chunk.Type == type && chunk.Name.Contains(path));
        return chunk == null ? default : LoadResourceInternal<T>(chunk);
    }

    /// <summary>
    ///     Gets a resource by name.
    /// </summary>
    /// <param name="path">Path of resource to find</param>
    /// <typeparam name="T">Type of resource, must implement ISumoResource</typeparam>
    /// <returns>Resource, if found</returns>
    public T? FindResourceByName<T>(string path) where T : ISumoResource, new()
    {
        SlResourceType type = SlUtil.ResourceId(typeof(T).Name);
        SlResourceChunk? chunk = _chunks.Find(chunk => chunk.Type == type && chunk.Name == path);
        return chunk == null ? default : LoadResourceInternal<T>(chunk);
    }

    /// <summary>
    ///     Gets a resource by its hash.
    /// </summary>
    /// <param name="id">Resource hash to find</param>
    /// <typeparam name="T">Type of resource, must implement ISumoResource</typeparam>
    /// <returns>Resource, if found</returns>
    public T? FindResourceByHash<T>(int id) where T : ISumoResource, new()
    {
        SlResourceType type = SlUtil.ResourceId(typeof(T).Name);
        SlResourceChunk? chunk = _chunks.Find(chunk => chunk.Type == type && chunk.Id == id);
        return chunk == null ? default : LoadResourceInternal<T>(chunk);
    }

    /// <summary>
    ///     Loads and caches a resource chunk.
    /// </summary>
    /// <param name="chunk">Resource chunk to load</param>
    /// <typeparam name="T">Type of resource, must implement ISumoResource</typeparam>
    /// <returns>Loaded resource</returns>
    private T LoadResourceInternal<T>(SlResourceChunk chunk) where T : ISumoResource, new()
    {
        // If this resource was already loaded, use that reference instead.
        if (_loadCache.TryGetValue(chunk.Id, out ISumoResource? value)) return (T)value;

        T resource = new();
        var context = new ResourceLoadContext(this, chunk);
        resource.Load(context, 0);

        // Cache the resource so we don't have to parse it again
        _loadCache[chunk.Id] = resource;

        return resource;
    }

    /// <summary>
    ///     Loads and caches a node definition/instance.
    /// </summary>
    /// <param name="chunk">Node chunk to load</param>
    /// <typeparam name="T">Node data type, must extend SeNodeBase and implement ILoadable</typeparam>
    /// <returns>Loaded node</returns>
    private T LoadNodeInternal<T>(SlResourceChunk chunk) where T : SeNodeBase, ILoadable, new()
    {
        // If this node was already loaded, use that reference instead.
        if (_nodeCache.TryGetValue(chunk.Id, out SeNodeBase? value)) return (T)value;

        T node = new();
        var context = new ResourceLoadContext(this, chunk);
        node.Load(context, 0);

        // Cache the node so we don't have to parse it again
        _nodeCache[chunk.Id] = node;

        return node;
    }

    /// <summary>
    ///     Loads a chunk database by path.
    /// </summary>
    /// <param name="cpuFilePath">Path to CPU file data</param>
    /// <param name="gpuFilePath">Path to GPU file data</param>
    /// <returns>Parsed resource database</returns>
    /// <exception cref="FileNotFoundException">Thrown if one of the files are not found</exception>
    public static SlResourceDatabase Load(string cpuFilePath, string gpuFilePath)
    {
        if (!File.Exists(cpuFilePath))
            throw new FileNotFoundException($"CPU file at {cpuFilePath} was not found!");
        if (!File.Exists(gpuFilePath))
            throw new FileNotFoundException($"GPU file at {gpuFilePath} was not found!");

        using FileStream cpuStream = File.OpenRead(cpuFilePath);
        using FileStream gpuStream = File.OpenRead(gpuFilePath);

        return Load(cpuStream, (int)cpuStream.Length, gpuStream);
    }

    /// <summary>
    ///     Loads a chunk database from buffers.
    /// </summary>
    /// <param name="cpuData">CPU data buffer</param>
    /// <param name="gpuData">GPU data buffer</param>
    /// <returns>Parsed resource database</returns>
    public static SlResourceDatabase Load(byte[] cpuData, byte[] gpuData)
    {
        using var cpuStream = new MemoryStream(cpuData);
        using var gpuStream = new MemoryStream(gpuData);
        return Load(cpuStream, cpuData.Length, gpuStream);
    }

    /// <summary>
    ///     Parses chunk data into database from CPU/GPU streams.
    /// </summary>
    /// <param name="cpuStream">CPU data stream</param>
    /// <param name="gpuStream">GPU data stream</param>
    /// <param name="cpuStreamSize">The size of the CPU data stream</param>
    /// <exception cref="SerializationException">Thrown if an error occurs while reading chunks</exception>
    public static SlResourceDatabase Load(Stream cpuStream, int cpuStreamSize, Stream gpuStream)
    {
        const int chunkHeaderSize = 0x20;
        const int relocationChunkType = 0x0eb411b1;

        Span<byte> header = stackalloc byte[chunkHeaderSize];
        var database = new SlResourceDatabase();
        using var reader = new BinaryReader(cpuStream);

        long end = cpuStream.Position + cpuStreamSize;
        while (cpuStream.Position < end)
        {
            // Cache the starting positions
            long cpuChunkStart = cpuStream.Position;
            long gpuChunkStart = gpuStream.Position;

            cpuStream.ReadExactly(header);
            var type = (SlResourceType)BinaryPrimitives.ReadInt32LittleEndian(header[..0x4]);
            int version = BinaryPrimitives.ReadInt32LittleEndian(header[0x4..0x8]);
            int chunkSize = BinaryPrimitives.ReadInt32LittleEndian(header[0x8..0xc]);
            int dataSize = BinaryPrimitives.ReadInt32LittleEndian(header[0xc..0x10]);
            int gpuChunkSize = BinaryPrimitives.ReadInt32LittleEndian(header[0x10..0x14]);
            int gpuDataSize = BinaryPrimitives.ReadInt32LittleEndian(header[0x14..0x18]);
            int chunkType = BinaryPrimitives.ReadInt32LittleEndian(header[0x1c..0x20]);

            // Calculate the offset to the next chunk
            long nextCpuChunkOffset = cpuChunkStart + chunkSize;
            long nextGpuChunkOffset = gpuChunkStart + gpuChunkSize;

            // Read the data contained in the chunk
            byte[] chunkData = reader.ReadBytes(dataSize);
            byte[] gpuChunkData = new byte[gpuDataSize];
            gpuStream.ReadExactly(gpuChunkData);

            var chunk = new SlResourceChunk(type, version, chunkData, gpuChunkData, chunkType != 0);

            cpuStream.Position = nextCpuChunkOffset;
            gpuStream.Position = nextGpuChunkOffset;

            // The next chunk will always be the relocations chunk
            // Read relocation header data, we only need some of it.
            cpuChunkStart = cpuStream.Position;
            cpuStream.ReadExactly(header);
            if (BinaryPrimitives.ReadInt32LittleEndian(header[..4]) != relocationChunkType)
                throw new SerializationException("Expected relocation chunk!");
            chunkSize = BinaryPrimitives.ReadInt32LittleEndian(header[0x8..0xc]);
            nextCpuChunkOffset = cpuChunkStart + chunkSize;

            // The relocation chunk is just an array of (offset, value) pairs
            int numRelocations = reader.ReadInt32();
            for (int i = 0; i < numRelocations; ++i)
                chunk.Relocations.Add(new SlResourceRelocation(reader.ReadInt32(), reader.ReadInt32()));

            cpuStream.Position = nextCpuChunkOffset;
            database._chunks.Add(chunk);
        }

        return database;
    }

    /// <summary>
    ///     Saves this database to CPU and GPU buffers.
    /// </summary>
    /// <returns>CPU/GPU buffer tuple</returns>
    public (byte[], byte[]) Save()
    {
        using var cpuStream = new MemoryStream();
        using var gpuStream = new MemoryStream();
        Save(cpuStream, gpuStream);
        return (cpuStream.ToArray(), gpuStream.ToArray());
    }

    /// <summary>
    ///     Saves this database to CPU and GPU files.
    /// </summary>
    /// <param name="cpuFilePath">Path of CPU file to write</param>
    /// <param name="gpuFilePath">Path of GPU file to write</param>
    /// <param name="inMemory">Whether or not to build the database in memory</param>
    public void Save(string cpuFilePath, string gpuFilePath, bool inMemory = false)
    {
        if (inMemory)
        {
            (byte[] cpu, byte[] gpu) = Save();
            File.WriteAllBytes(cpuFilePath, cpu);
            File.WriteAllBytes(gpuFilePath, gpu);
            return;
        }
        
        using FileStream cpuStream = File.Create(cpuFilePath);
        using FileStream gpuStream = File.Create(gpuFilePath);
        Save(cpuStream, gpuStream);
    }

    /// <summary>
    ///     Saves this database to CPU and GPU streams.
    /// </summary>
    public void Save(Stream cpuStream, Stream gpuStream)
    {
        const int chunkHeaderSize = 0x20;
        const int relocationChunkType = 0x0eb411b1;

        // Pre-calculate buffer sizes to speed up serialization
        int cpuSize = 0, gpuSize = 0;
        foreach (SlResourceChunk chunk in _chunks)
        {
            cpuSize = SlUtil.Align(cpuSize + chunkHeaderSize + chunk.Data.Length, 0x80);
            if (chunk.GpuData.Length != 0)
                gpuSize = SlUtil.Align(gpuSize + chunk.GpuData.Length, 0x100);
            // Relocation chunk allocation
            cpuSize = SlUtil.Align(cpuSize + chunkHeaderSize + 0x4 + 0x8 * chunk.Relocations.Count, 0x80);
        }

        cpuStream.SetLength(cpuSize);
        gpuStream.SetLength(gpuSize);

        Span<byte> header = stackalloc byte[chunkHeaderSize];
        using var writer = new BinaryWriter(cpuStream);
        foreach (SlResourceChunk chunk in _chunks)
        {
            WriteChunk(chunk);

            // Append relocation data after each chunk
            int dataSize = 0x4 + 0x8 * chunk.Relocations.Count;
            int chunkSize = SlUtil.Align(chunkHeaderSize + dataSize, 0x80);
            long nextChunkPosition = cpuStream.Position + chunkSize;

            // Make sure header is zero-initialized
            header.Clear();
            BinaryPrimitives.WriteInt32LittleEndian(header[..0x4], relocationChunkType);
            BinaryPrimitives.WriteInt32LittleEndian(header[0x8..0xc], chunkSize);
            BinaryPrimitives.WriteInt32LittleEndian(header[0xc..0x10], dataSize);
            cpuStream.Write(header);

            writer.Write(chunk.Relocations.Count);
            foreach (SlResourceRelocation relocation in chunk.Relocations)
            {
                writer.Write(relocation.Offset);
                writer.Write(relocation.Value);
            }

            cpuStream.Position = nextChunkPosition;
        }

        return;

        void WriteChunk(SlResourceChunk chunk)
        {
            int chunkSize = SlUtil.Align(chunkHeaderSize + chunk.Data.Length, 0x80);
            int gpuChunkSize = SlUtil.Align(chunk.GpuData.Length, 0x100);

            long nextChunkPosition = cpuStream.Position + chunkSize;
            long nextGpuChunkPosition = gpuStream.Position + gpuChunkSize;

            Span<byte> header = stackalloc byte[chunkHeaderSize];
            BinaryPrimitives.WriteInt32LittleEndian(header[..0x4], (int)chunk.Type);
            BinaryPrimitives.WriteInt32LittleEndian(header[0x4..0x8], chunk.Version);
            BinaryPrimitives.WriteInt32LittleEndian(header[0x8..0xc], chunkSize);
            BinaryPrimitives.WriteInt32LittleEndian(header[0xc..0x10], chunk.Data.Length);
            BinaryPrimitives.WriteInt32LittleEndian(header[0x10..0x14], gpuChunkSize);
            BinaryPrimitives.WriteInt32LittleEndian(header[0x14..0x18], chunk.GpuData.Length);
            BinaryPrimitives.WriteInt32LittleEndian(header[0x18..0x1c], 0);
            BinaryPrimitives.WriteInt32LittleEndian(header[0x1c..0x20], chunk.IsResource ? 1 : 0);

            cpuStream.Write(header);
            cpuStream.Write(chunk.Data);
            gpuStream.Write(chunk.GpuData);

            cpuStream.Position = nextChunkPosition;
            gpuStream.Position = nextGpuChunkPosition;
        }
    }
}