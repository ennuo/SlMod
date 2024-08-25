using SlLib.Resources.Database;
using SlLib.Serialization;
using SlLib.SumoTool.Siff.Objects;
using SlLib.Utilities;

namespace SlLib.SumoTool.Siff;

public class ObjectDefLibrary : IResourceSerializable
{
    public readonly Dictionary<int, IObjectDef> Objects = [];
    
    public T? GetObjectDef<T>(string path) where T : IObjectDef
    {
        path = path.Replace("/", "\\").ToUpper();
        int hash = SlUtil.SumoHash(path);
        
        //Console.WriteLine($"{path}: {hash}");
        
        if (Objects.TryGetValue(hash, out IObjectDef? def))
            return (T)def;
        return default;
    }
    
    public T? GetObjectDef<T>(int hash) where T : IObjectDef
    {
        if (Objects.TryGetValue(hash, out IObjectDef? def))
            return (T)def;
        return default;
    }

    public GroupObject? FindGroupContaining(string path)
    {
        path = path.Replace("/", "\\").ToUpper();
        int hash = SlUtil.SumoHash(path);
        return FindGroupContaining(hash);
    }

    public GroupObject? FindGroupContaining(int hash)
    {
        foreach (IObjectDef def in Objects.Values)
        {
            if (def is not GroupObject group) continue;
            if (group.ObjectHashes.Contains(hash))
                return group;
        }

        return null;
    }
    
    /// <inheritdoc />
    public void Load(ResourceLoadContext context)
    {
        context.Position += context.Platform.GetPointerSize() * 0x2;
        int numEntries = context.ReadInt32();
        
        if (!context.IsSSR)
        {
            int hashTable = context.ReadPointer();
            int objectData = context.ReadPointer();
            context.Position = objectData;
        }

        for (int i = 0; i < numEntries; ++i)
        {
            int hash = context.ReadInt32();
            string type = context.ReadMagic();
            int address = context.ReadPointer();
            context.ReadPointer(); // SiffLoadSet
            
            // Dummy node I'm using to pad out quick lookup table until I feel like
            // figuring out how its calculated.
            if (type == "FUCK") continue;

            IObjectDef? objectDef = type switch
            {
                "GROP" => new GroupObject(),
                "TEXT" => new TextObject(),
                "TXTR" => new TextureObject(),
                "SCIS" => new ScissorObject(),
                "HELP" => new HelperObject(),
                "PNTR" => new PointerAreaObject(),
                "GORD" => new GouraudObject(),
                "GNRC" => new CustomObject(),
                _ => null
            };

            if (objectDef == null)
                throw new NotSupportedException($"{type} is an unsupported object definition type!");

            int link = context.Position;
            context.Position = address;
            objectDef.Load(context);
            context.Position = link;

            Objects[hash] = objectDef;
        }
    }

    public void Save(ResourceSaveContext context, ISaveBuffer buffer)
    {
        context.WriteInt32(buffer, Objects.Count, 0x8);
        if (context.IsSSR)
        {
            for (int i = 0; i < Objects.Count; ++i)
            {
                int offset = 0xc + (i * 0x10);
                var pair = Objects.ElementAt(i);
                context.WriteInt32(buffer, pair.Key, offset + 0x0);
                context.WriteMagic(buffer, pair.Value.ObjectType, offset + 0x4);
                context.SavePointer(buffer, pair.Value, offset + 0x8);
            }
        }
        else
        {
            ISaveBuffer hashList = context.SaveGenericPointer(buffer, 0xc, Objects.Count * 0x4);
            ISaveBuffer objectList = context.SaveGenericPointer(buffer, 0x10, Objects.Count * 0x10);
            for (int i = 0; i < Objects.Count; ++i)
            {
                int offset = i * 0x10;
                var pair = Objects.ElementAt(i);
                context.WriteInt32(objectList, pair.Key, offset + 0x0);
                context.WriteMagic(objectList, pair.Value.ObjectType, offset + 0x4);
                context.SavePointer(objectList, pair.Value, offset + 0x8);
            }
        }
    }
    
    public int GetSizeForSerialization(SlPlatform platform, int version)
    {
        if (version == -1)
            return (platform.GetPointerSize() * 0x2) + 0x4 + (0x10 * Objects.Count);
        return (platform.GetPointerSize() * 4) + 0x4;
    }
}