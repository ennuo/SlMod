using SlLib.Serialization;

namespace SlLib.Resources.Scene.Definitions;

// hidden in manager list
// SE_COLLISION_
public class SeDefinitionCollisionNode : SeDefinitionTransformNode, IResourceSerializable
{
    /// <inheritdoc />
    public void Load(ResourceLoadContext context)
    {
        context.Position = LoadInternal(context, context.Position);
    }
}