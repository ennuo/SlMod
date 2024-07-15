using SlLib.Resources.Database;
using SlLib.Serialization;

namespace SlLib.SumoTool.Siff.Forest;

public class SuTreeGroup : IResourceSerializable
{
    public int Hash;
    public List<int> TreeHashes = [];
    
    public void Load(ResourceLoadContext context)
    {
        Hash = context.ReadInt32();
        TreeHashes = context.LoadArrayPointer(context.ReadInt32(), context.ReadInt32);
    }
    
    public void Save(ResourceSaveContext context, ISaveBuffer buffer)
    {
        context.WriteInt32(buffer, Hash, 0x0);
        context.WriteInt32(buffer, TreeHashes.Count, 0x4);
        
        // How do I still not have a helper method for this
        ISaveBuffer treeHashData = context.SaveGenericPointer(buffer, 0x8, TreeHashes.Count * 4);
        for (int i = 0; i < TreeHashes.Count; ++i)
            context.WriteInt32(treeHashData, TreeHashes[i], i * 0x4);
    }

    public int GetSizeForSerialization(SlPlatform platform, int version)
    {
        return 0xc;
    }
}