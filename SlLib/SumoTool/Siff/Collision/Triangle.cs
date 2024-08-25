using SlLib.Resources.Database;
using SlLib.Serialization;

namespace SlLib.SumoTool.Siff.Collision;

public class Triangle : IResourceSerializable
{
    public short Vertex0, Vertex1, Vertex2;
    public short Flags;
    public int SurfaceType;
    
    public void Load(ResourceLoadContext context)
    {
        Vertex0 = context.ReadInt16();
        Vertex1 = context.ReadInt16();
        Vertex2 = context.ReadInt16();

        Flags = context.ReadInt16();
        
        SurfaceType = context.ReadInt32();
    }

    public void Save(ResourceSaveContext context, ISaveBuffer buffer)
    {
        context.WriteInt16(buffer, Vertex0, 0x0);
        context.WriteInt16(buffer, Vertex1, 0x2);
        context.WriteInt16(buffer, Vertex2, 0x4);
        context.WriteInt16(buffer, Flags, 0x6);
        context.WriteInt32(buffer, SurfaceType, 0x8);
    }

    public int GetSizeForSerialization(SlPlatform platform, int version)
    {
        return 0xc;
    }
}