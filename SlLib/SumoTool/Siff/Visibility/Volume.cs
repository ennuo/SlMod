using System.Numerics;
using SlLib.Resources.Database;
using SlLib.Serialization;

namespace SlLib.SumoTool.Siff.Visibility;

public class Volume : IResourceSerializable
{
    public Vector3 AxesXs;
    public Vector3 AxesYs;
    public Vector3 AxesZs;
    public Vector4 Center;
    public Vector3 Extents;
    
    public void Load(ResourceLoadContext context)
    {
        AxesXs = context.ReadAlignedFloat3();
        AxesYs = context.ReadAlignedFloat3();
        AxesZs = context.ReadAlignedFloat3();
        Center = context.ReadFloat4();
        Extents = context.ReadAlignedFloat3();
    }

    public void Save(ResourceSaveContext context, ISaveBuffer buffer)
    {
        context.WriteFloat3(buffer, AxesXs, 0x0);
        context.WriteFloat3(buffer, AxesYs, 0x10);
        context.WriteFloat3(buffer, AxesZs, 0x20);
        context.WriteFloat4(buffer, Center, 0x30);
        context.WriteFloat3(buffer, Extents, 0x40);
    }
    
    public int GetSizeForSerialization(SlPlatform platform, int version)
    {
        return 0x50;
    }
}