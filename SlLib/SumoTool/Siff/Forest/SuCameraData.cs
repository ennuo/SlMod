using SlLib.Resources.Database;
using SlLib.Serialization;

namespace SlLib.SumoTool.Siff.Forest;

public class SuCameraData : IResourceSerializable
{
    public int Branch;
    public int Type;
    public float Fov;
    public int[] AnimatedData = new int[2];
    
    public void Load(ResourceLoadContext context)
    {
        Branch = context.ReadInt32();
        Type = context.ReadInt32();
        Fov = context.ReadFloat();

        AnimatedData[0] = context.ReadInt32();
        AnimatedData[1] = context.ReadInt32();
    }
    
    public void Save(ResourceSaveContext context, ISaveBuffer buffer)
    {
        context.WriteInt32(buffer, Branch, 0x0);
        context.WriteInt32(buffer, Type, 0x4);
        context.WriteFloat(buffer, Fov, 0x8);
        
        context.WriteInt32(buffer, AnimatedData[0], 0xc);
        context.WriteInt32(buffer, AnimatedData[1], 0x10);
    }
    
    public int GetSizeForSerialization(SlPlatform platform, int version)
    {
        return 0x14;
    }
}