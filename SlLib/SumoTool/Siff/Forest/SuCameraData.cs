using SlLib.Resources.Database;
using SlLib.Serialization;

namespace SlLib.SumoTool.Siff.Forest;

public class SuCameraData : IResourceSerializable
{
    public int Branch;
    public int Type;
    public float Fov;
    // SuAnimatedFloatData
    
    public void Load(ResourceLoadContext context)
    {
        Branch = context.ReadInt32();
        Type = context.ReadInt32();
        Fov = context.ReadFloat();
    }
    
    public void Save(ResourceSaveContext context, ISaveBuffer buffer)
    {
        context.WriteInt32(buffer, Branch, 0x0);
        context.WriteInt32(buffer, Type, 0x4);
        context.WriteFloat(buffer, Fov, 0x8);
    }
    
    public int GetSizeForSerialization(SlPlatform platform, int version)
    {
        return 0x14;
    }
}