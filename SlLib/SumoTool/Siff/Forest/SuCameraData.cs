using SlLib.Resources.Database;
using SlLib.Serialization;

namespace SlLib.SumoTool.Siff.Forest;

public class SuCameraData : IResourceSerializable
{
    public int Branch;
    public int Type;
    public float Fov;
    
    // SuAnimatedFloatData
    public int AnimatedData_Index;
    public float AnimatedData_Value;
    
    public void Load(ResourceLoadContext context)
    {
        Branch = context.ReadInt32();
        Type = context.ReadInt32();
        Fov = context.ReadFloat();

        AnimatedData_Index = context.ReadInt32();
        AnimatedData_Value = context.ReadFloat();
    }
    
    public void Save(ResourceSaveContext context, ISaveBuffer buffer)
    {
        context.WriteInt32(buffer, Branch, 0x0);
        context.WriteInt32(buffer, Type, 0x4);
        context.WriteFloat(buffer, Fov, 0x8);
        
        context.WriteInt32(buffer, AnimatedData_Index, 0xc);
        context.WriteFloat(buffer, AnimatedData_Value, 0x10);
    }
    
    public int GetSizeForSerialization(SlPlatform platform, int version)
    {
        return 0x14;
    }
}