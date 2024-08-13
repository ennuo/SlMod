using SlLib.Enums;
using SlLib.Resources.Database;
using SlLib.Serialization;

namespace SlLib.Resources.Collision;

public class SlCollisionMaterial : IResourceSerializable
{
    public CollisionFlags Flags;
    public SurfaceType Type;
    
    // This is just the color used in the editor,
    // setting it is basically pointless, but going to keep the field anyway
    public int Color;
    
    public string Name = string.Empty;
    
    public void Load(ResourceLoadContext context)
    {
        Flags = (CollisionFlags)context.ReadInt32();
        Type = (SurfaceType)context.ReadInt32();
        Color = context.ReadInt32();
        Name = context.ReadStringPointer();
    }

    public void Save(ResourceSaveContext context, ISaveBuffer buffer)
    {
        context.WriteInt32(buffer, (int)Flags, 0x0);
        context.WriteInt32(buffer, (int)Type, 0x4);
        context.WriteInt32(buffer, Color, 0x8);
        context.WriteStringPointer(buffer, Name, 0xc);
    }
    
    public int GetSizeForSerialization(SlPlatform platform, int version)
    {
        return 0x10;
    }
}