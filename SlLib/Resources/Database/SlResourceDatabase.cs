using System.Diagnostics.CodeAnalysis;
using System.Runtime.Serialization;
using System.Text.Json;
using SlLib.Extensions;
using SlLib.Resources.Scene;
using SlLib.Resources.Scene.Definitions;
using SlLib.Resources.Scene.Dummies;
using SlLib.Resources.Scene.Instances;
using SlLib.Serialization;
using SlLib.Utilities;

namespace SlLib.Resources.Database;

public class SlResourceDatabase
{
    /// <summary>
    ///     Type map for all node classes.
    /// </summary>
    private static readonly Dictionary<SlResourceType, Type> TypeMap = [];
    
    /// <summary>
    ///     The platform this database is built for.
    /// </summary>
    public readonly SlPlatform Platform;

    /// <summary>
    ///     The chunks held by this database.
    /// </summary>
    private readonly List<SlResourceChunk> _chunks = [];

    /// <summary>
    ///     Cache of already loaded resources to prevent re-serialization.
    /// </summary>
    private readonly Dictionary<int, ISumoResource> _loadCache = [];

    /// <summary>
    ///     Quick lookup for nodes by their UID.
    /// </summary>
    private readonly Dictionary<int, SeNodeBase> _nodeMap = [];
    
    /// <summary>
    ///     The scene node that contains all instances in this database.
    /// </summary>
    public readonly SeInstanceSceneNode Scene = new() { UidName = "DefaultScene" };
    
    /// <summary>
    ///     Definition roots contained in this database.
    /// </summary>
    public readonly List<SeDefinitionNode> RootDefinitions = [];

    /// <summary>
    ///     Constructs an empty resource database for a specified platform.
    /// </summary>
    /// <param name="platform">Platform this database is built for</param>
    public SlResourceDatabase(SlPlatform platform)
    {
        Platform = platform;
    }

    static SlResourceDatabase()
    {
        var classes = typeof(SeNodeBase).Assembly.GetTypes().Where(t => t.IsSubclassOf(typeof(SeNodeBase)) && !t.IsAbstract);
        foreach (Type cls in classes)
        {
            SlResourceType type = SlUtil.ResourceId(cls.Name);
            TypeMap[type] = cls;
        }

        // these nodes need to be fixed up to load tsr data i guess,
        // dont really care too much if im being real tho
        // TypeMap.Remove(SlResourceType.SeDefinitionParticleStyleNode);
        // TypeMap.Remove(SlResourceType.TriggerPhantomInstanceNode);
        // TypeMap.Remove(SlResourceType.SeDefinitionParticleStyleNode);
        //
        // TypeMap.Remove(SlResourceType.SeToneMappingRefInstanceNode);
    }
    
    /// <summary>
    ///     Flushes all nodes in the scene to the persistent database.
    /// </summary>
    public void FlushSceneGraph()
    {
        _nodeMap.Clear();
        _chunks.RemoveAll(chunk => !chunk.IsResource);
        
        foreach (SeDefinitionNode def in RootDefinitions) AddNodes(def);
        
        SeGraphNode? root = Scene.FirstChild;
        while (root != null)
        {
            AddNodes(root);
            root = root.NextSibling;
        }

        return;
        
        void AddNodes(SeGraphNode node)
        {
            // Don't add the default folder node to the database, it won't break anything, but
            // I don't want there to be any overlaps regardless.
            if (node == SeDefinitionFolderNode.Default) return;
            
            _nodeMap[node.Uid] = node;
            if (node is SeInstanceNode { Definition: not null } instance) AddNodes(instance.Definition);
            
            AddNode(node);
            
            SeGraphNode? child = node.FirstChild;
            while (child != null)
            {
                AddNodes(child);
                child = child.NextSibling;
            }
        }
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

    public void AddNode<T>(T node) where T : SeNodeBase
    {
        var context = new ResourceSaveContext();
        ISaveBuffer slab = context.Allocate(node.GetSizeForSerialization(Platform, Platform.DefaultVersion));
        context.SaveReference(slab, node, 0);

        
        (byte[] cpu, byte[] gpu) = context.Flush();
        var relocations = context.Relocations;
        AddNodeInternal(SlUtil.ResourceId(node.GetType().Name), node.Uid, cpu, gpu, relocations);
        
        _nodeMap[node.Uid] = node;
    }
    
    /// <summary>
    ///     Creates a duplicate of a node from another database and stores it in this database.
    /// </summary>
    /// <param name="node">Node to duplicate</param>
    /// <typeparam name="T">Type of node to duplicate</typeparam>
    public void PasteNode<T>(T node) where T : SeNodeBase
    {
        var context = new ResourceSaveContext();
        ISaveBuffer slab = context.Allocate(node.GetSizeForSerialization(Platform, Platform.DefaultVersion));
        context.SaveReference(slab, node, 0);
        
        (byte[] cpu, byte[] gpu) = context.Flush();
        var relocations = context.Relocations;
        AddNodeInternal(SlUtil.ResourceId(node.GetType().Name), node.Uid, cpu, gpu, relocations);
        
        // Trigger a load so the duplicated instance gets stored in the scenegraph
        LoadGenericNode(node.Uid);
    }

    public string GetResourceNameFromHash(int hash)
    {
        SlResourceChunk? chunk = _chunks.Find(c => c.IsResource && c.Id == hash);
        if (chunk == null) return string.Empty;
        return chunk.Name;
    }

    public byte[]? GetNodeResourceData(int uid)
    {
        var chunk = _chunks.Find(chunk => !chunk.IsResource && chunk.Id == uid);
        return chunk?.Data;
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
    ///     Duplicates a hierarchy of nodes into this database.
    /// </summary>
    /// <param name="nodes">Nodes to duplicate</param>
    /// <param name="parent">Node to parent duplicated nodes to</param>
    public void PasteClipboard(List<SeGraphNode> nodes, SeGraphNode? parent = null)
    {
        parent ??= Scene;
        
        SlResourceDatabase clipboard = new(Platform);
        
        // Copy all the nodes to a temporary clipboard database,
        // handles the deep cloning for us.
        foreach (SeGraphNode node in nodes)
            AddNodeRecursive(node);
        
        // Rename all the nodes so we can use them
        
        // Parent all root nodes to wherever the user selected
        SeGraphNode? child = clipboard.Scene.FirstChild;
        while (child != null)
        {
            child.Parent = parent;
            child = child.NextSibling;
        }
        
        clipboard.Save("C:/Users/Aidan/Desktop/clipboard.cpu.spc", "C:/Users/Aidan/Desktop/clipboard.gpu.spc",
            inMemory: true);
        
        // Copy the duplicated instances to our own database
        clipboard.CopyTo(this);
        
        return;
        
        void AddNodeRecursive(SeGraphNode node)
        {
            clipboard.PasteNode(node);
            SeGraphNode? child = node.FirstChild;
            while (child != null)
            {
                AddNodeRecursive(child);
                child = child.NextSibling;
            }
        }
    }
    
    /// <summary>
    ///     Removes a node from the scene graph.
    /// </summary>
    /// <param name="id">ID of the node to remove</param>
    public void RemoveNode(int id)
    {
        if (_nodeMap.TryGetValue(id, out SeNodeBase? node))
        {
            // Make sure the node is removed from the hierarchy
            if (node is SeGraphNode sgNode)
                sgNode.Parent = null;
        }
        
        _nodeMap.Remove(id);
        _chunks.RemoveAll(chunk => !chunk.IsResource && chunk.Id == id);
    }

    /// <summary>
    ///     Removes a resource from the database.
    /// </summary>
    /// <param name="id">ID of the resource to remove</param>
    public void RemoveResource(int id)
    {
        _loadCache.Remove(id);
        _chunks.RemoveAll(chunk => chunk.IsResource && chunk.Id == id);
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

        int colIndex = 0;
        foreach (SlResourceChunk chunk in _chunks)
        {
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
    ///     Dumps all nodes in the scene graph to a folder.
    /// </summary>
    /// <param name="path">Folder to dump nodes to</param>
    public void DumpNodesToFolder(string path)
    {
        foreach (SlResourceChunk chunk in _chunks)
        {
            string typeFolder = Path.Join(Path.Join(path, "/nodes/"), chunk.Type.ToString());
            
            string name = SlUtil.GetShortName(chunk.Name);
            string folder = typeFolder;
            
            Directory.CreateDirectory(folder);
            File.WriteAllBytes(Path.Join(folder, $"{name}.bin"), chunk.Data);
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
        if (hash == 0) return;
        SlResourceType type = SlUtil.ResourceId(typeof(T).Name);
        SlResourceChunk? chunk = _chunks.Find(resource => resource.Type == type && resource.Id == hash);
        if (chunk == null)
            return;

        database.AddResourceInternal<T>(hash, chunk.Data, chunk.GpuData, chunk.Relocations);
    }

    /// <summary>
    ///     Copies all resources from this database to another.
    /// </summary>
    /// <param name="target">Database to copy resources to</param>
    public void CopyTo(SlResourceDatabase target)
    {
        foreach (SlResourceChunk chunk in _chunks)
        {
            if (chunk.IsResource)
                target.AddResourceInternal(chunk.Type, chunk.Id, chunk.Data, chunk.GpuData, chunk.Relocations);
            else
            {
                target.AddNodeInternal(chunk.Type, chunk.Id, chunk.Data, chunk.GpuData, chunk.Relocations);
                target.LoadGenericNode(chunk.Id); // Trigger a load of the node, so it gets added to the scene graph
            }
        }
    }

    /// <summary>
    ///     Checks if a resource exists by partial name.
    /// </summary>
    /// <param name="name">Partial name to search for</param>
    /// <returns>Whether the resource was found</returns>
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
    
    /// <summary>
    ///     Gets all nodes of a specified type.
    /// </summary>
    /// <typeparam name="T">Node data type, must extend SeNodeBase and implement ILoadable</typeparam>
    /// <returns>List of nodes</returns>
    public List<T> FindNodesThatDeriveFrom<T>() where T : SeGraphNode, IResourceSerializable, new()
    {
        List<T> nodes = [];
        
        Gather(Scene);
        return nodes;
        
        void Gather(SeGraphNode node)
        {
            if (node is T derivation) nodes.Add(derivation);
            
            SeGraphNode? child = node.FirstChild;
            while (child != null)
            {
                Gather(child);
                child = child.NextSibling;
            }
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
    /// <param name="type">Node class type</param>
    /// <returns>Loaded node</returns>
    private SeNodeBase LoadNodeInternal(SlResourceChunk chunk, Type type)
    {
        // If this node was already loaded, use that reference instead.
        if (_nodeMap.TryGetValue(chunk.Id, out SeNodeBase? value)) return value;
        
        SeNodeBase node = (SeNodeBase?)Activator.CreateInstance(type) ??
                          throw new Exception("Unable to create node instance!");
        var context = new ResourceLoadContext(this, chunk);
        
        // Cache the node so we don't have to parse it again
        _nodeMap[chunk.Id] = node;
        
        node.Load(context);
        
        return node;
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
        if (_nodeMap.TryGetValue(chunk.Id, out SeNodeBase? value)) return (T)value;

        T node = new();
        var context = new ResourceLoadContext(this, chunk);
        
        // Cache the node so we don't have to parse it again
        _nodeMap[chunk.Id] = node;
        
        node.Load(context);
        
        return node;
    }
    
    /// <summary>
    ///     Adds a new resource to this database.
    /// </summary>
    /// <param name="hash">Node name hash</param>
    /// <param name="cpu">CPU data</param>
    /// <param name="gpu">GPU data</param>
    /// <param name="relocations">All pointer relocations for the resource</param>
    private void AddNodeInternal<T>(int hash, byte[] cpu, byte[] gpu, List<SlResourceRelocation> relocations) where T : SeNodeBase
    {
        SlResourceType type = SlUtil.ResourceId(typeof(T).Name);
        AddNodeInternal(type, hash, cpu, gpu, relocations);
    }
    
    /// <summary>
    ///     Adds a new resource to this database.
    /// </summary>
    /// <param name="type">Type of node</param>
    /// <param name="hash">Node name hash</param>
    /// <param name="cpu">CPU data</param>
    /// <param name="gpu">GPU data</param>
    /// <param name="relocations">All pointer relocations for the resource</param>
    private void AddNodeInternal(SlResourceType type, int hash, byte[] cpu, byte[] gpu, List<SlResourceRelocation> relocations)
    {
        // Push data to chunk already in database if it exists
        SlResourceChunk? chunk = _chunks.Find(c => c.Type == type && c.Id == hash);
        if (chunk == null)
        {
            chunk = new SlResourceChunk(type, Platform, Platform.DefaultVersion, cpu, gpu, false);
            
            // Just push to the end, nodes don't get resolved until after everything is loaded,
            // so the order doesn't actually matter
            _chunks.Add(chunk);
        }
        
        chunk.Data = cpu;
        chunk.GpuData = gpu;
        chunk.Relocations = relocations;
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
    
    // hidden from manager list
    // SeDefinitionCollisionNode
    // SeInstanceParticleAffectorNode
    // SeDefinitionParticleAffectorNode
    // SeDefinitionCollisionMaterialNode
    // SeDefinitionAreaNode
    // SeDefinitionEntityShadowNode
    // SeDefinitionSkyNode
    // SeDefinitionEntityNode
    // SeFile
    // SeBinaryFile
    // SeProjectEnd
    // SeProject
    // SeWorkspace
    // SeWorkspaceEnd
    // Water13DefNode
    // WaterSeaDefinitionNode
    // SeInstanceNodeDriftZone(?)
    // SeInstanceNodeSlipStream(?)
    // SeInstanceNodeVapourTrail(?)
    // SeDefinitionViewportOverlay
    // 

    private static HashSet<SlResourceType> UnsupportedTypes = [];
    
    public SeGraphNode? LoadGenericNode(int id)
    {
        if (id == 0) return null;
        if (_nodeMap.TryGetValue(id, out SeNodeBase? node))
            return (SeGraphNode)node;
        
        SlResourceChunk? chunk = _chunks.Find(chunk => !chunk.IsResource && chunk.Id == id);
        if (chunk == null) return null;

        if (TypeMap.TryGetValue(chunk.Type, out Type? cls))
            node = LoadNodeInternal(chunk, cls);
        
        if (node == null)
        {
            if (chunk.Type.ToString().Contains("Def")) node = LoadNodeInternal<SeDummyDefinitionNode>(chunk);
            else if (chunk.Type.ToString().Contains("Instance"))
                node = LoadNodeInternal<SeDummyInstanceNode>(chunk);
            else
                node = LoadNodeInternal<SeDummyGraphNode>(chunk);

            UnsupportedTypes.Add(chunk.Type);
        }
        
        // Make sure we're keeping everything in order.
        if (node is SeDefinitionNode { Parent: null } def)
            RootDefinitions.Add(def);

        // Any instances with no parents should be re-attached to the root of the scene,
        // generally won't be many cases of this aside from clipboard shenanigans
        if (node is SeInstanceNode { Parent: null } inst)
            inst.Parent = Scene;
        
        node.Debug_ResourceType = chunk.Type;
        return (SeGraphNode)node;
    }
    
    /// <summary>
    ///     Sets up the scene graph in the database on load finish.
    /// </summary>
    private void OnLoadFinished()
    {
        var projects = new Stack<SeProject>();
        var workspaces = new Stack<SeWorkspace>();
        
        foreach (SlResourceChunk chunk in _chunks)
        {
            if (chunk.IsResource) continue;
            SeGraphNode? node = LoadGenericNode(chunk.Id);
            if (node == null) continue;

            if (node is SeProject project)
            {
                projects.Push(project);
                continue;
            }

            if (node is SeWorkspace workspace)
            {
                workspaces.Push(workspace);
                continue;
            }

            if (node is SeProjectEnd)
            {
                projects.Pop();
                continue;
            }

            if (node is SeWorkspaceEnd)
            {
                workspaces.Pop();
                continue;
            }
        }
        
        Console.WriteLine(string.Join(',', UnsupportedTypes));
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
        const int oldSkeletonResourceType = 0x40FEA5F2;
        
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
            
            bool trustResourceFlag = version > 0xb;
            
            // Resource types were changed at some point to have their lower bits chopped
            // off to fit relocation types, but earlier revisions don't have this.
            if (version <= 0x1b)
            {
                // I guess the name of the SlSkeleton class got renamed at some point?
                if ((int)type == oldSkeletonResourceType)
                    type = SlResourceType.SlSkeleton;
                else 
                    type = (SlResourceType)((int)type >>> 4);
            }

            if (type == SlResourceType.BadnikDefinitionNode) type = SlResourceType.CameoObjectDefinitionNode;
            if (type == SlResourceType.BadnikInstanceNode) type = SlResourceType.CameoObjectInstanceNode;


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

            bool isResource = chunkType != 0;
            if (!trustResourceFlag)
            {
                // hack
                isResource = (type.ToString().StartsWith("Sl") && !type.ToString().Contains("Node")) || type == SlResourceType.Water13Simulation || type == SlResourceType.Water13Renderable || type == SlResourceType.WaterSeaRenderable;
            }
            
            var chunk = new SlResourceChunk(type, platform, version, chunkData, gpuChunkData, isResource);
            
            cpuStream.Position = nextCpuChunkOffset;
            gpuStream.Position = nextGpuChunkOffset;

            // The next chunk will always be the relocations chunk
            // Read relocation header data, we only need some of it.
            cpuChunkStart = cpuStream.Position;
            cpuStream.ReadExactly(header);
            
            
            int relType = platform.ReadInt32(header[..4]);
            if (version <= 0x1b) relType >>>= 4;
            
            if (relType != relocationChunkType)
                throw new SerializationException("Expected relocation chunk!");
            chunkSize = platform.ReadInt32(header[0x8..0xc]);
            nextCpuChunkOffset = cpuChunkStart + chunkSize;

            // The relocation chunk is just an array of (offset, value) pairs
            int numRelocations = reader.ReadInt32();
            for (int i = 0; i < numRelocations; ++i)
            {
                int offset = reader.ReadInt32();
                int value = reader.ReadInt32();
                
                if (version <= 0x1b)
                {
                    value = value switch
                    {
                        -1 => SlRelocationType.Pointer,
                        -2 => SlRelocationType.GpuPointer,
                        _ => SlRelocationType.Resource | value >>> 4
                    };
                }
                
                chunk.Relocations.Add(new SlResourceRelocation(offset, value));
            }

            cpuStream.Position = nextCpuChunkOffset;
            database._chunks.Add(chunk);
        }

        database.OnLoadFinished();
        
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