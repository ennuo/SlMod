using SlLib.Serialization;

namespace SlLib.Resources.Scene.Definitions;

public class SeDefinitionEntityShadowNode : SeDefinitionEntityNode, IResourceSerializable
{
    /// <inheritdoc />
    public new void Load(ResourceLoadContext context)
    {
        context.Position = LoadInternal(context, context.Position);
    }
}