using System.Numerics;
using SlLib.Resources.Database;
using SlLib.Serialization;

namespace SlLib.SumoTool.Siff.Forest;

public struct SuCollisionTriangle : IResourceSerializable
{
    public Vector4 A;
    public Vector4 B;
    public Vector4 C;
    
    public void Load(ResourceLoadContext context)
    {
        A = context.ReadFloat4();
        B = context.ReadFloat4();
        C = context.ReadFloat4();
    }

    public void Save(ResourceSaveContext context, ISaveBuffer buffer)
    {
        context.WriteFloat4(buffer, A, 0x0);
        context.WriteFloat4(buffer, B, 0x10);
        context.WriteFloat4(buffer, C, 0x20);
    }
    
    public int GetSizeForSerialization(SlPlatform platform, int version)
    {
        return 0x30;
    }
}