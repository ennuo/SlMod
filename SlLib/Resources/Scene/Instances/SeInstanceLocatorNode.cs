using SlLib.Resources.Database;
using SlLib.Serialization;

namespace SlLib.Resources.Scene.Instances;

public class SeInstanceLocatorNode : SeInstanceTransformNode, IResourceSerializable
{
    /// <inheritdoc />
    public void Load(ResourceLoadContext context)
    {
        context.Position = LoadInternal(context, context.Position);
    }
    
    public void Save(ResourceSaveContext context, ISaveBuffer buffer)
    {
        
    }
    
    public int GetSizeForSerialization(SlPlatform platform, int version) => 0x160;
}