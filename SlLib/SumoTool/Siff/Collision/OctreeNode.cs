using SlLib.Resources.Database;
using SlLib.Serialization;

namespace SlLib.SumoTool.Siff.Collision;

public class OctreeNode : IResourceSerializable
{
    public int NumTriangles;
    public int TriangleBaseIndex;
    public OctreeChildIndices ChildIndices;
    
    public void Load(ResourceLoadContext context)
    {
        NumTriangles = context.ReadInt32();
        TriangleBaseIndex = context.ReadInt32();
        for (int i = 0; i < 8; ++i)
            ChildIndices[i] = context.ReadInt16();
    }

    public void Save(ResourceSaveContext context, ISaveBuffer buffer)
    {
        context.WriteInt32(buffer, NumTriangles, 0x0);
        context.WriteInt32(buffer, TriangleBaseIndex, 0x4);
        for (int i = 0; i < 8; ++i)
            context.WriteInt16(buffer, ChildIndices[i], 0x8 + (i * 2));
    }

    public int GetSizeForSerialization(SlPlatform platform, int version)
    {
        return 0x18;
    }
}