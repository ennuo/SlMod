using System.Runtime.Serialization;
using SlLib.Resources.Collision;
using SlLib.Resources.Database;
using SlLib.Serialization;

namespace SlLib.Resources;

public class SlResourceCollision : ISumoResource
{
    public SlResourceHeader Header { get; set; } = new();
    
    public CollisionType Type = CollisionType.Mesh;
    public SlResourceMesh Mesh = new();
    
    public void Load(ResourceLoadContext context)
    {
        // Ignoring the first header, since it's duplicated
        // the first one also doesn't have the name assigned to it.
        Header = context.LoadObject<SlResourceHeader>();
        context.ReadInt32(); // No idea what this is, always 3?
        
        Header.Id = context.ReadInt32();
        Header.Name = context.ReadStringPointer();
        Type = (CollisionType)context.ReadInt32();
        if (Type != CollisionType.Mesh)
            throw new NotSupportedException("Only collision meshes are supported!");
        Mesh = context.LoadPointer<SlResourceMesh>() ??
               throw new SerializationException("SlResourceMesh for collision cannot be NULL!");
    }

    public void Save(ResourceSaveContext context, ISaveBuffer buffer)
    {
        // Dumb hack to not write the name here
        string name = Header.Name;
        Header.Name = string.Empty;
        context.SaveObject(buffer, Header, 0x0);
        Header.Name = name;
        
        // Still no idea what this is, always 3, could just be version or something, but there's already a verison tag?
        context.WriteInt32(buffer, 0x3, 0xc);
        
        context.WriteInt32(buffer, Header.Id, 0x10);
        context.WriteInt32(buffer, 0x0, 0x18); // Collision type = Mesh
        context.SavePointer(buffer, Mesh, 0x1c);
        context.WriteStringPointer(buffer, name, 0x14);
    }

    public int GetSizeForSerialization(SlPlatform platform, int version)
    {
        return 0x20;
    }
}