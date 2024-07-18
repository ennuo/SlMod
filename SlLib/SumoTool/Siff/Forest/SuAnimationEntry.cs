using SlLib.Resources.Database;
using SlLib.Serialization;

namespace SlLib.SumoTool.Siff.Forest;

public class SuAnimationEntry : IResourceSerializable
{
    public SuAnimation? Animation;
    public string AnimName = string.Empty;
    public int Hash;
    
    public void Load(ResourceLoadContext context)
    {
        Animation = context.LoadPointer<SuAnimation>();
        AnimName = context.ReadStringPointer();
        Hash = context.ReadInt32();
    }

    public void Save(ResourceSaveContext context, ISaveBuffer buffer)
    {
        context.SavePointer(buffer, Animation, 0x0);
        context.WriteStringPointer(buffer, AnimName, 0x4);
        context.WriteInt32(buffer, Hash, 0x8);
    }

    public int GetSizeForSerialization(SlPlatform platform, int version)
    {
        return 0xc;
    }
}