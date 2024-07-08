using SlLib.Serialization;

namespace SlLib.Resources.Scene.Definitions;

// SE_LOCATOR_
public class SeDefinitionLocatorNode : SeDefinitionTransformNode, IResourceSerializable
{
    /// <inheritdoc />
    public void Load(ResourceLoadContext context)
    {
        context.Position = LoadInternal(context, context.Position);
    }
}