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

    public int GetSizeForSerialization(SlPlatform platform, int version)
    {
        return 0xc;
    }
}