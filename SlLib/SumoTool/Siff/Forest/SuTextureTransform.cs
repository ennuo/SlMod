using SlLib.Resources.Database;
using SlLib.Serialization;

namespace SlLib.SumoTool.Siff.Forest;

public class SuTextureTransform : IResourceSerializable
{
    public float PosU, PosV;
    public float Angle;
    public float ScaleU, ScaleV;
    
    public void Load(ResourceLoadContext context)
    {
        PosU = context.ReadFloat();
        PosV = context.ReadFloat();
        Angle = context.ReadFloat();
        ScaleU = context.ReadFloat();
        ScaleV = context.ReadFloat();
    }

    public void Save(ResourceSaveContext context, ISaveBuffer buffer)
    {
        context.WriteFloat(buffer, PosU, 0x0);
        context.WriteFloat(buffer, PosV, 0x4);
        context.WriteFloat(buffer, Angle, 0x8);
        context.WriteFloat(buffer, ScaleU, 0xc);
        context.WriteFloat(buffer, ScaleV, 0x10);
    }

    public int GetSizeForSerialization(SlPlatform platform, int version)
    {
        return 0x14;
    }
}