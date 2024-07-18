using System.Numerics;
using SlLib.Resources.Database;
using SlLib.Serialization;

namespace SlLib.SumoTool.Siff.Logic;

public class Trigger : IResourceSerializable
{
    public int NameHash;
    public int NumAttributes;
    public int AttributeStartIndex;
    public int Flags;
    public Vector4 Position;
    public Vector4 Normal;
    public Vector4 Vertex0, Vertex1, Vertex2, Vertex3;
    
    public void Load(ResourceLoadContext context)
    {
        NameHash = context.ReadInt32();
        NumAttributes = context.ReadInt32();
        AttributeStartIndex = context.ReadInt32();
        Flags = context.ReadInt32();
        Position = context.ReadFloat4();
        Normal = context.ReadFloat4();
        Vertex0 = context.ReadFloat4();
        Vertex1 = context.ReadFloat4();
        Vertex2 = context.ReadFloat4();
        Vertex3 = context.ReadFloat4();
    }
    
    public void Save(ResourceSaveContext context, ISaveBuffer buffer)
    {
        context.WriteInt32(buffer, NameHash, 0x0);
        context.WriteInt32(buffer, NumAttributes, 0x4);
        context.WriteInt32(buffer, AttributeStartIndex, 0x8);
        context.WriteInt32(buffer, Flags, 0xc);
        context.WriteFloat4(buffer, Position, 0x10);
        context.WriteFloat4(buffer, Normal, 0x20);
        context.WriteFloat4(buffer, Vertex0, 0x30);
        context.WriteFloat4(buffer, Vertex1, 0x40);
        context.WriteFloat4(buffer, Vertex2, 0x50);
        context.WriteFloat4(buffer, Vertex3, 0x60);
    }
    
    public int GetSizeForSerialization(SlPlatform platform, int version)
    {
        return 0x70;
    }
}