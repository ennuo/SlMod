using SlLib.Resources.Collision;
using SlLib.Resources.Database;
using SlLib.Serialization;

namespace SlLib.Resources;

public class SlResourceCollision : ISumoResource
{
    public SlResourceHeader Header { get; set; }
    
    public CollisionType Type = CollisionType.Mesh;
    
    public void Load(ResourceLoadContext context)
    {
        // Ignoring the first header, since it's duplicated
        // the first one also doesn't have the name assigned to it.
        context.LoadObject<SlResourceHeader>();
        context.ReadInt32(); // No idea what this is, always 3?

        Header = context.LoadObject<SlResourceHeader>();
        
        int collisionData;
        
        // Same notice as every other file, around Android's version
        // all pointers got moved to before the counts
        if (context.Version >= SlPlatform.Android.DefaultVersion)
        {
            collisionData = context.ReadPointer();
            Type = (CollisionType)context.ReadInt32();
        }
        else
        {
            Type = (CollisionType)context.ReadInt32();
            collisionData = context.ReadPointer();
        }

        if (Type != CollisionType.Mesh)
            throw new NotSupportedException("Only collision meshes are supported!");
    }
}