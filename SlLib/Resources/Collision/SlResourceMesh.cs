using SlLib.Serialization;

namespace SlLib.Resources.Collision;

public class SlResourceMesh : IResourceSerializable
{
    public List<SlCollisionMaterial> Materials = [];
    public List<SlResourceMeshSection> Sections = [];
    
    public void Load(ResourceLoadContext context)
    {
        
    }
}