using SlLib.Resources.Database;
using SlLib.Serialization;

namespace SlLib.SumoTool.Siff.Forest.Blind;

public class SuNameUint32Pairs : IResourceSerializable
{
    public readonly List<(int NameHash, int Value)> Pairs = [];
    
    public void Load(ResourceLoadContext context)
    {
        int numPairs = context.ReadInt32();
        for (int i = 0; i < numPairs; ++i)
            Pairs.Add((context.ReadInt32(), context.ReadInt32()));
    }

    public void Save(ResourceSaveContext context, ISaveBuffer buffer)
    {
        context.WriteInt32(buffer, Pairs.Count, 0x0);
        for (int i = 0; i < Pairs.Count; ++i)
        {
            int offset = 0x4 + (i * 0x8);
            
            context.WriteInt32(buffer, Pairs[i].NameHash, offset);
            context.WriteInt32(buffer, Pairs[i].Value, offset + 4);
        }
    }

    public int GetSizeForSerialization(SlPlatform platform, int version)
    {
        return 0x4 + Pairs.Count * 0x8;
    }
}