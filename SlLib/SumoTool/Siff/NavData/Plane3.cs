using System.Numerics;
using SlLib.Resources.Database;
using SlLib.Serialization;

namespace SlLib.SumoTool.Siff.NavData;

public struct Plane3 : IResourceSerializable
{
    public Vector3 Normal;
    public float Const;
    
    public void Load(ResourceLoadContext context)
    {
        Normal = context.ReadAlignedFloat3();
        Const = context.ReadFloat();
        context.Position += 0xc;
    }
    
    public void Save(ResourceSaveContext context, ISaveBuffer buffer)
    {
        context.WriteFloat3(buffer, Normal, 0x0);
        context.WriteFloat(buffer, Const, 0x10);
    }
    
    public int GetSizeForSerialization(SlPlatform platform, int version)
    {
        return 0x20;
    }
}