using SlLib.Resources.Database;
using SlLib.Serialization;

namespace SlLib.SumoTool.Siff.Forest;

public struct StreamOverride : IResourceSerializable
{
    public int Hash;
    public int Index;

    public void Load(ResourceLoadContext context)
    {
        Hash = context.ReadInt32();
        Index = context.ReadInt32();
    }

    public void Save(ResourceSaveContext context, ISaveBuffer buffer)
    {
        context.WriteInt32(buffer, Hash, 0x0);
        context.WriteInt32(buffer, Index, 0x4);
    }

    public int GetSizeForSerialization(SlPlatform platform, int version)
    {
        return 0x8;
    }
}