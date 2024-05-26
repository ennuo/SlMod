﻿using System.Diagnostics.CodeAnalysis;
using System.Runtime.Serialization;
using System.Text.Json;
using SlLib.Extensions;
using SlLib.Resources.Scene;
using SlLib.Resources.Scene.Definitions;
using SlLib.Serialization;
using SlLib.Utilities;

namespace SlLib.Resources.Database;

public class SlResourceDatabase
{
    /// <summary>
    ///     The platform this database is built for.
    /// </summary>
    public readonly SlPlatform Platform;

    /// <summary>
    ///     The chunks held by this database.
    /// </summary>
    public readonly List<SlResourceChunk> _chunks = [];

    /// <summary>
    ///     Cache of already loaded resources to prevent re-serialization.
    /// </summary>
    private readonly Dictionary<int, ISumoResource> _loadCache = [];

    /// <summary>
    ///     Cache of already loaded nodes to prevent re-serialization.
    /// </summary>
    private readonly Dictionary<int, SeNodeBase> _nodeCache = [];

    /// <summary>
    ///     Constructs an empty resource database for a specified platform.
    /// </summary>
    /// <param name="platform">Platform this database is built for</param>
    public SlResourceDatabase(SlPlatform platform)
    {
        Platform = platform;
    }

    /// <summary>
    ///     Gets a list of all scenes contained in this database.
    /// </summary>
    /// <returns></returns>
    public List<string> GetSceneList()
    {
        HashSet<string> scenes = [];
        foreach (SlResourceChunk chunk in _chunks)
        {
            if (string.IsNullOrEmpty(chunk.Scene)) continue;
            scenes.Add(chunk.Scene);
        }
        
        return [..scenes];
    }
    
    /// <summary>
    ///     Adds a resource to the database, overriding if one with the same ID already exists.
    /// </summary>
    /// <param name="resource">Resource to save to database</param>
    /// <typeparam name="T">Type of resource, must implement ISumoResource and IWritable</typeparam>
    public void AddResource<T>(T resource) where T : ISumoResource
    {
        var context = new ResourceSaveContext();
        ISaveBuffer slab = context.Allocate(resource.GetSizeForSerialization(Platform, Platform.DefaultVersion));
        context.SaveReference(slab, resource, 0);

        (byte[] cpu, byte[] gpu) = context.Flush();
        var relocations = context.Relocations;
        AddResourceInternal<T>(resource.Header.Id, cpu, gpu, relocations);
    }

    public string GetResourceNameFromHash(int hash)
    {
        SlResourceChunk? chunk = _chunks.Find(c => c.IsResource && c.Id == hash);
        if (chunk == null) return string.Empty;
        return chunk.Name;
    }

    /// <summary>
    ///     Gets raw chunk data from database for a resource by partial path.
    /// </summary>
    /// <param name="path">Partial path of resource to find</param>
    /// <param name="cpuData">Output cpu data</param>
    /// <param name="gpuData">Output gpu data</param>
    /// <typeparam name="T">Type of resource, must implement ISumoResource</typeparam>
    /// <returns>Whether or not the resource was found</returns>
    public bool GetRawResourceByPartialName<T>(string path, [NotNullWhen(true)] out byte[]? cpuData,
        [NotNullWhen(true)] out byte[]? gpuData) where T : ISumoResource
    {
        cpuData = null;
        gpuData = null;

        SlResourceType type = SlUtil.ResourceId(typeof(T).Name);
        SlResourceChunk? chunk = _chunks.Find(chunk => chunk.Type == type && chunk.Name.Contains(path));

        if (chunk == null) return false;

        cpuData = chunk.Data;
        gpuData = chunk.GpuData;
        
        return true;
    }

    /// <summary>
    ///     Dumps all resource data in this folder to a specified folder.
    /// </summary>
    /// <param name="path">Path to dump all data to</param>
    public void DumpResourceDatabaseToFolder(string path)
    {
        var options = new JsonSerializerOptions { WriteIndented = true, IncludeFields = true };

        string nodeFolder = Path.Join(path, "nodes");
        string resFolder = Path.Join(path, "resources");
        
        foreach (SlResourceChunk chunk in _chunks)
        {
            if (!chunk.IsResource) continue;
            
            string typeFolder = Path.Join(chunk.IsResource ? resFolder : nodeFolder, chunk.Type.ToString());

            string name = SlUtil.GetShortName(chunk.Name);
            string folder = typeFolder;
            if (chunk.Name.Contains(':'))
            {
                string[] pathFragments = chunk.Name.Split(":");
                string sceneName = Path.GetFileNameWithoutExtension(pathFragments[0]);
                var folders = pathFragments[1].Split("|").ToList();
                folders.RemoveAt(folders.Count - 1);
                folder = Path.Join(path, sceneName, string.Join('/', folders));
            }

            Directory.CreateDirectory(folder);
            
            File.WriteAllBytes(Path.Join(folder, $"{name}.cpu.bin"), chunk.Data);
            if (chunk.GpuData.Length != 0)
                File.WriteAllBytes(Path.Join(folder, $"{name}.gpu.bin"), chunk.GpuData);
            
            if (chunk.Relocations.Count != 0)
            {
                chunk.Relocations.Sort((a, z) => a.Offset - z.Offset);
                string json = JsonSerializer.Serialize(chunk.Relocations, options);
                File.WriteAllText(Path.Join(folder, $"{name}.rel.json"), json);   
            }
        }
    }

    /// <summary>
    ///     Copies a resource from this database to another via name.
    /// </summary>
    /// <param name="database">Database to copy resource to</param>
    /// <param name="name">Name of resource to copy</param>
    /// <typeparam name="T">Type of resource, must implement ISumoResource</typeparam>
    public void CopyResourceByName<T>(SlResourceDatabase database, string name) where T : ISumoResource, new()
    {
        CopyResourceByHash<T>(database, SlUtil.HashString(name));
    }
    
    /// <summary>
    ///     Copies a resource from this database to another via hash.
    /// </summary>
    /// <param name="database">Database to copy resource to</param>
    /// <param name="hash">Hash of resource to copy</param>
    /// <typeparam name="T">Type of resource, must implement ISumoResource</typeparam>
    public void CopyResourceByHash<T>(SlResourceDatabase database, int hash) where T : ISumoResource, new()
    {
        SlResourceType type = SlUtil.ResourceId(typeof(T).Name);
        SlResourceChunk? chunk = _chunks.Find(resource => resource.Type == type && resource.Id == hash);
        if (chunk == null)
            throw new NullReferenceException("Chunk doesn't exist!");

        database.AddResourceInternal<T>(hash, chunk.Data, chunk.GpuData, chunk.Relocations);
    }

    /// <summary>
    ///     Copies all resources from this database to another.
    /// </summary>
    /// <param name="target">Database to copy resources to</param>
    public void CopyTo(SlResourceDatabase target)
    {
        foreach (SlResourceChunk chunk in _chunks)
            target.AddResourceInternal(chunk.Type, chunk.Id, chunk.Data, chunk.GpuData, chunk.Relocations);
    }

    /// <summary>
    ///     Checks if a resource exists by partial name.
    /// </summary>
    /// <param name="name">Partial name to search for</param>
    /// <returns>Whether or not the resource was found</returns>
    public bool ContainsResourceByPartialName(string name)
    {
        return _chunks.Exists(chunk => chunk.Name.Contains(name));
    }
    
    /// <summary>
    ///     Checks if a resource with a specified hash exists in the database.
    /// </summary>
    /// <param name="hash">Hash to find</param>
    /// <returns>Whether the resource was found</returns>
    public bool ContainsResourceWithHash(int hash)
    {
        return _chunks.Exists(chunk => chunk.Id == hash);
    }

    public SlResourceType GetNodeType(int hash)
    {
        SlResourceChunk? chunk = _chunks.Find(chunk => !chunk.IsResource && chunk.Id == hash);
        return chunk?.Type ?? SlResourceType.Invalid;
    }

    public List<T> GetChildrenOfNode<T>(int uid) where T : SeNodeBase, IResourceSerializable, new()
    {
        var nodes = GetNodesOfType<T>();
        List<T> children = [];
        foreach (T node in nodes)
        {
            if (node is not SeGraphNode graphNode) continue;
            if (graphNode.ParentUid == uid)
                children.Add(node);
        }

        return children;
    }

    public void Debug_PrintSceneRoots(string scene)
    {
        Console.WriteLine(scene + " roots:");
        foreach (var chunk in _chunks)
        {
            if (chunk.IsResource) continue;
            if (chunk.Scene != scene) continue;
            
            SeDummyNode node = new();
            var context = new ResourceLoadContext(this, chunk);
            node.Load(context);
            
            if (node.ParentUid == 0)
                Console.WriteLine("\t" + node.GetShortName());
        }
    }

    /// <summary>
    ///     Gets all nodes of a specified type.
    /// </summary>
    /// <param name="scene">Optional parameter to limit resources to a specific scene</param>
    /// <typeparam name="T">Node data type, must extend SeNodeBase and implement ILoadable</typeparam>
    /// <returns>List of nodes</returns>
    public List<T> GetNodesOfType<T>(string? scene = "") where T : SeNodeBase, IResourceSerializable, new()
    {
        SlResourceType type = SlUtil.ResourceId(typeof(T).Name);
        var chunks = _chunks.FindAll(chunk =>
        {
            if (chunk.Type != type) return false;
            if (!string.IsNullOrEmpty(scene))
                return chunk.Scene == scene;
            return true;
        });
        return chunks.Select(LoadNodeInternal<T>).ToList();
    }
    
    /// <summary>
    ///     Gets all resources of a specified type.
    /// </summary>
    /// <param name="scene">Optional parameter to limit resources to a specific scene</param>
    /// <typeparam name="T">Type of resource, must implement ISumoResource</typeparam>
    /// <returns>List of resources</returns>
    public List<T> GetResourcesOfType<T>(string? scene = "") where T : ISumoResource, new()
    {
        SlResourceType type = SlUtil.ResourceId(typeof(T).Name);
        var chunks = _chunks.FindAll(chunk =>
        {
            if (chunk.Type != type) return false;
            if (!string.IsNullOrEmpty(scene))
                return chunk.Scene == scene;
            return true;
        });
        return chunks.Select(chunk => LoadResourceInternal<T>(chunk, false)).ToList();
    }

    /// <summary>
    ///     Gets a node that matches a partial path.
    /// </summary>
    /// <param name="path">Partial path of node to find</param>
    /// <typeparam name="T">Node data type, must extend SeNodeBase and implement ILoadable</typeparam>
    /// <returns>Node, if found</returns>
    public T? FindNodeByPartialName<T>(string path) where T : SeNodeBase, IResourceSerializable, new()
    {
        SlResourceType type = SlUtil.ResourceId(typeof(T).Name);
        SlResourceChunk? chunk = _chunks.Find(chunk => chunk.Type == type && chunk.Name.Contains(path));
        return chunk == null ? default : LoadNodeInternal<T>(chunk);
    }

    /// <summary>
    ///     Gets a resource that matches a partial path.
    /// </summary>
    /// <param name="path">Partial path of resource to find</param>
    /// <param name="instance">Whether or not to load a new instance, instead of a reference</param>
    /// <typeparam name="T">Type of resource, must implement ISumoResource</typeparam>
    /// <returns>Resource, if found</returns>
    public T? FindResourceByPartialName<T>(string path, bool instance = false) where T : ISumoResource, new()
    {
        SlResourceType type = SlUtil.ResourceId(typeof(T).Name);
        SlResourceChunk? chunk = _chunks.Find(chunk => chunk.Type == type && chunk.Name.Contains(path));
        return chunk == null ? default : LoadResourceInternal<T>(chunk, instance);
    }

    /// <summary>
    ///     Gets a resource by name.
    /// </summary>
    /// <param name="path">Path of resource to find</param>
    /// <param name="instance">Whether or not to load a new instance, instead of a reference</param>
    /// <typeparam name="T">Type of resource, must implement ISumoResource</typeparam>
    /// <returns>Resource, if found</returns>
    public T? FindResourceByName<T>(string path, bool instance = false) where T : ISumoResource, new()
    {
        SlResourceType type = SlUtil.ResourceId(typeof(T).Name);
        SlResourceChunk? chunk = _chunks.Find(chunk => chunk.Type == type && chunk.Name == path);
        return chunk == null ? default : LoadResourceInternal<T>(chunk, instance);
    }

    /// <summary>
    ///     Gets a resource by its hash.
    /// </summary>
    /// <param name="id">Resource hash to find</param>
    /// <param name="instance">Whether or not to load a new instance, instead of a reference</param>
    /// <typeparam name="T">Type of resource, must implement ISumoResource</typeparam>
    /// <returns>Resource, if found</returns>
    public T? FindResourceByHash<T>(int id, bool instance = false) where T : ISumoResource, new()
    {
        SlResourceType type = SlUtil.ResourceId(typeof(T).Name);
        SlResourceChunk? chunk = _chunks.Find(chunk => chunk.Type == type && chunk.Id == id);
        return chunk == null ? default : LoadResourceInternal<T>(chunk, instance);
    }
    
    /// <summary>
    ///     Adds a new resource to this database.
    /// </summary>
    /// <param name="hash">Resource name hash</param>
    /// <param name="cpu">CPU data</param>
    /// <param name="gpu">GPU data</param>
    /// <param name="relocations">All pointer relocations for the resource</param>
    /// <typeparam name="T">Type of resource, must implement ISumoResource</typeparam>
    private void AddResourceInternal<T>(int hash, byte[] cpu, byte[] gpu, List<SlResourceRelocation> relocations)
    {
        SlResourceType type = SlUtil.ResourceId(typeof(T).Name);
        AddResourceInternal(type, hash, cpu, gpu, relocations);
    }
    
    /// <summary>
    ///     Adds a new resource to this database.
    /// </summary>
    /// <param name="type">Type of resource</param>
    /// <param name="hash">Resource name hash</param>
    /// <param name="cpu">CPU data</param>
    /// <param name="gpu">GPU data</param>
    /// <param name="relocations">All pointer relocations for the resource</param>
    private void AddResourceInternal(SlResourceType type, int hash, byte[] cpu, byte[] gpu, List<SlResourceRelocation> relocations)
    {
        // Push data to chunk already in database if it exists
        SlResourceChunk? chunk = _chunks.Find(c => c.Type == type && c.Id == hash);
        if (chunk == null)
        {
            chunk = new SlResourceChunk(type, Platform, Platform.DefaultVersion, cpu, gpu, true);

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
    }

    /// <summary>
    ///     Loads and caches a resource chunk.
    /// </summary>
    /// <param name="chunk">Resource chunk to load</param>
    /// <param name="instance">Whether or not to load a new instance, instead of a reference</param>
    /// <typeparam name="T">Type of resource, must implement ISumoResource</typeparam>
    /// <returns>Loaded resource</returns>
    private T LoadResourceInternal<T>(SlResourceChunk chunk, bool instance) where T : ISumoResource, new()
    {
        // If this resource was already loaded, use that reference instead.
        if (!instance && _loadCache.TryGetValue(chunk.Id, out ISumoResource? value)) return (T)value;

        T resource = new();
        var context = new ResourceLoadContext(this, chunk);
        resource.Load(context);
        
        // Cache the resource so we don't have to parse it again
        if (!instance) _loadCache[chunk.Id] = resource;

        return resource;
    }

    /// <summary>
    ///     Loads and caches a node definition/instance.
    /// </summary>
    /// <param name="chunk">Node chunk to load</param>
    /// <typeparam name="T">Node data type, must extend SeNodeBase and implement ILoadable</typeparam>
    /// <returns>Loaded node</returns>
    private T LoadNodeInternal<T>(SlResourceChunk chunk) where T : SeNodeBase, IResourceSerializable, new()
    {
        // If this node was already loaded, use that reference instead.
        if (_nodeCache.TryGetValue(chunk.Id, out SeNodeBase? value)) return (T)value;

        T node = new();
        var context = new ResourceLoadContext(this, chunk);
        node.Load(context);

        // Cache the node so we don't have to parse it again
        _nodeCache[chunk.Id] = node;

        return node;
    }

    /// <summary>
    ///     Attempts to remove most un-used resources from the database.
    /// </summary>
    public void RemoveUnusedResources()
    {
        HashSet<SlResourceChunk> saveList = [];
        HashSet<SlResourceChunk> rootList = [];
     
        // The scene inherently relies on the node setup for what's actually used
        // So first do a pass of all resources used based on the nodes
        foreach (SlResourceChunk chunk in _chunks)
        {
            if (chunk.IsResource) continue;
            saveList.Add(chunk);
            
            // Some nodes are specifically designed so that their names
            // are paths to resources, so check for that
            SlResourceChunk? nameChunk = _chunks.Find(c => c.IsResource && c.Id == chunk.Id);
            if (nameChunk != null) rootList.Add(nameChunk);
            foreach (SlResourceRelocation relocation in chunk.Relocations)
            {
                if (!relocation.IsResourcePointer) continue;
                int id = chunk.Data.ReadInt32(relocation.Offset);
                if (id == 0) continue;
                SlResourceChunk? referenceChunk = _chunks.Find(c => c.IsResource && c.Id == id);
                if (referenceChunk != null)
                    rootList.Add(referenceChunk);
            }
        }
        
        // Special case, make sure the brake light materials doesn't get deleted
        var brakeLightMaterial =
            _chunks.Find(c => c.Name.Contains("brakelight") && c.Type == SlResourceType.SlMaterial2);
        if (brakeLightMaterial != null) rootList.Add(brakeLightMaterial);
        
        foreach (SlResourceChunk chunk in rootList)
            RecurseResourceDependencies(chunk);
        
        var pruneList = _chunks.Where(chunk => !saveList.Contains(chunk)).ToList();
        foreach (SlResourceChunk chunk in pruneList)
            _chunks.Remove(chunk);
        
        return;

        void RecurseResourceDependencies(SlResourceChunk root)
        {
            saveList.Add(root);
            foreach (SlResourceRelocation relocation in root.Relocations)
            {
                if (!relocation.IsResourcePointer) continue;
                int id = root.Data.ReadInt32(relocation.Offset);
                if (id == 0) continue;
                SlResourceChunk? chunk = _chunks.Find(c => c.IsResource && c.Id == id);
                if (chunk != null)
                    RecurseResourceDependencies(chunk);
            }
        }



    }

    /// <summary>
    ///     Loads a chunk database by path.
    /// </summary>
    /// <param name="cpuFilePath">Path to CPU file data</param>
    /// <param name="gpuFilePath">Path to GPU file data</param>
    /// <param name="inMemory">Whether to cache all data in memory before loading</param>
    /// <returns>Parsed resource database</returns>
    /// <exception cref="FileNotFoundException">Thrown if one of the files are not found</exception>
    public static SlResourceDatabase Load(string cpuFilePath, string gpuFilePath, bool inMemory = false)
    {
        if (!File.Exists(cpuFilePath))
            throw new FileNotFoundException($"CPU file at {cpuFilePath} was not found!");
        if (!File.Exists(gpuFilePath))
            throw new FileNotFoundException($"GPU file at {gpuFilePath} was not found!");

        SlPlatform platform = SlPlatform.GuessPlatformFromExtension(cpuFilePath);

        if (inMemory)
        {
            byte[] cpuData = File.ReadAllBytes(cpuFilePath);
            byte[] gpuData = File.ReadAllBytes(gpuFilePath);
            return Load(cpuData, gpuData, platform);
        }
        
        using FileStream cpuStream = File.OpenRead(cpuFilePath);
        using FileStream gpuStream = File.OpenRead(gpuFilePath);

        return Load(cpuStream, (int)cpuStream.Length, gpuStream, platform);
    }

    /// <summary>
    ///     Loads a chunk database from buffers.
    /// </summary>
    /// <param name="cpuData">CPU data buffer</param>
    /// <param name="gpuData">GPU data buffer</param>
    /// <param name="platform">The platform this database was built for</param>
    /// <returns>Parsed resource database</returns>
    public static SlResourceDatabase Load(byte[] cpuData, byte[] gpuData, SlPlatform platform)
    {
        using var cpuStream = new MemoryStream(cpuData);
        using var gpuStream = new MemoryStream(gpuData);
        return Load(cpuStream, cpuData.Length, gpuStream, platform);
    }

    /// <summary>
    ///     Parses chunk data into database from CPU/GPU streams.
    /// </summary>
    /// <param name="cpuStream">CPU data stream</param>
    /// <param name="gpuStream">GPU data stream</param>
    /// <param name="cpuStreamSize">The size of the CPU data stream</param>
    /// <param name="platform">The platform this database was built for</param>
    /// <exception cref="SerializationException">Thrown if an error occurs while reading chunks</exception>
    public static SlResourceDatabase Load(Stream cpuStream, int cpuStreamSize, Stream gpuStream, SlPlatform platform)
    {
        const int chunkHeaderSize = 0x20;
        const int relocationChunkType = 0x0eb411b1;

        Span<byte> header = stackalloc byte[chunkHeaderSize];
        var database = new SlResourceDatabase(platform);
        using BinaryReader reader = platform.GetReader(cpuStream);

        long end = cpuStream.Position + cpuStreamSize;
        while (cpuStream.Position < end)
        {
            // Cache the starting positions
            long cpuChunkStart = cpuStream.Position;
            long gpuChunkStart = gpuStream.Position;

            cpuStream.ReadExactly(header);
            var type = (SlResourceType)platform.ReadInt32(header[..0x4]);
            int version = platform.ReadInt32(header[0x4..0x8]);
            int chunkSize = platform.ReadInt32(header[0x8..0xc]);
            int dataSize = platform.ReadInt32(header[0xc..0x10]);
            int gpuChunkSize = platform.ReadInt32(header[0x10..0x14]);
            int gpuDataSize = platform.ReadInt32(header[0x14..0x18]);
            int chunkType = platform.ReadInt32(header[0x1c..0x20]);

            // Calculate the offset to the next chunk
            long nextCpuChunkOffset = cpuChunkStart + chunkSize;
            long nextGpuChunkOffset = gpuChunkStart + gpuChunkSize;

            // Read the data contained in the chunk
            byte[] chunkData = reader.ReadBytes(dataSize);
            byte[] gpuChunkData = new byte[gpuDataSize];
            gpuStream.ReadExactly(gpuChunkData);

            var chunk = new SlResourceChunk(type, platform, version, chunkData, gpuChunkData, chunkType != 0);

            cpuStream.Position = nextCpuChunkOffset;
            gpuStream.Position = nextGpuChunkOffset;

            // The next chunk will always be the relocations chunk
            // Read relocation header data, we only need some of it.
            cpuChunkStart = cpuStream.Position;
            cpuStream.ReadExactly(header);
            if (platform.ReadInt32(header[..4]) != relocationChunkType)
                throw new SerializationException("Expected relocation chunk!");
            chunkSize = platform.ReadInt32(header[0x8..0xc]);
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
        using BinaryWriter writer = Platform.GetWriter(cpuStream);
        foreach (SlResourceChunk chunk in _chunks)
        {
            WriteChunk(chunk);

            // Append relocation data after each chunk
            int dataSize = 0x4 + 0x8 * chunk.Relocations.Count;
            int chunkSize = SlUtil.Align(chunkHeaderSize + dataSize, 0x80);
            long nextChunkPosition = cpuStream.Position + chunkSize;

            // Make sure header is zero-initialized
            header.Clear();

            Platform.WriteInt32(header[..0x4], relocationChunkType);
            Platform.WriteInt32(header[0x8..0xc], chunkSize);
            Platform.WriteInt32(header[0xc..0x10], dataSize);
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
            Platform.WriteInt32(header[..0x4], (int)chunk.Type);
            Platform.WriteInt32(header[0x4..0x8], chunk.Version);
            Platform.WriteInt32(header[0x8..0xc], chunkSize);
            Platform.WriteInt32(header[0xc..0x10], chunk.Data.Length);
            Platform.WriteInt32(header[0x10..0x14], gpuChunkSize);
            Platform.WriteInt32(header[0x14..0x18], chunk.GpuData.Length);
            Platform.WriteInt32(header[0x18..0x1c], 0);
            Platform.WriteInt32(header[0x1c..0x20], chunk.IsResource ? 1 : 0);

            cpuStream.Write(header);
            cpuStream.Write(chunk.Data);
            gpuStream.Write(chunk.GpuData);

            cpuStream.Position = nextChunkPosition;
            gpuStream.Position = nextGpuChunkPosition;
        }
    }
}