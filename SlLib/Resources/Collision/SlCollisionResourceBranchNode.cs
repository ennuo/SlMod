using System.Numerics;
using SlLib.Resources.Database;
using SlLib.Serialization;

namespace SlLib.Resources.Collision;

public class SlCollisionResourceBranchNode : IResourceSerializable
{
    public Vector4 Center;
    public Vector4 Extents;
    public int First = -1;
    public int Next = -1;
    public int Leaf = -1;
    
    // 1 = floor
    // 4 = wall
    public int Flags = 1;
    
    public void Load(ResourceLoadContext context)
    {
        Center = context.ReadFloat4();
        Extents = context.ReadFloat4();
        First = context.ReadInt32();
        Next = context.ReadInt32();
        Leaf = context.ReadInt32();
        Flags = context.ReadInt32();
    }
    
    public void Save(ResourceSaveContext context, ISaveBuffer buffer)
    {
        context.WriteFloat4(buffer, Center, 0x0);
        context.WriteFloat4(buffer, Extents, 0x10);
        context.WriteInt32(buffer, First, 0x20);
        context.WriteInt32(buffer, Next, 0x24);
        context.WriteInt32(buffer, Leaf, 0x28);
        context.WriteInt32(buffer, Flags, 0x2c);
    }
    
    public int GetSizeForSerialization(SlPlatform platform, int version)
    {
        return 0x30;
    }
}