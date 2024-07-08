using SlLib.Serialization;

namespace SlLib.Resources.Scene.Definitions;

public class SeDefinitionSkyNode : SeDefinitionEntityNode, IResourceSerializable
{
    /// <inheritdoc />
    public new void Load(ResourceLoadContext context)
    {
        context.Position = LoadInternal(context, context.Position);
    }
}