using SlLib.Serialization;

namespace SlLib.Resources.Scene.Definitions;

// SE_LIGHT_
public class SeDefinitionLightNode : SeDefinitionTransformNode, IResourceSerializable
{
    /// <inheritdoc />
    public void Load(ResourceLoadContext context)
    {
        context.Position = LoadInternal(context, context.Position);
        
        // SeLightData
    }
}