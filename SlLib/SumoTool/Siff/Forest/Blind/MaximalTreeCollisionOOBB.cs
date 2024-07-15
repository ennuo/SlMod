using System.Numerics;
using SlLib.Resources.Database;
using SlLib.Serialization;

namespace SlLib.SumoTool.Siff.Forest.Blind;

public class MaximalTreeCollisionOOBB : IResourceSerializable
{
    public Matrix4x4 ObbTransform;
    public Vector4 Extents;
    
    public void Load(ResourceLoadContext context)
    {
        ObbTransform = context.ReadMatrix();
        Extents = context.ReadFloat4();
    }

    public void Save(ResourceSaveContext context, ISaveBuffer buffer)
    {
        context.WriteMatrix(buffer, ObbTransform, 0x0);
        context.WriteFloat4(buffer, Extents, 0x40);
    }

    public int GetSizeForSerialization(SlPlatform platform, int version)
    {
        return 0x50;
    }
}