using SlLib.Serialization;

namespace SlLib.Resources.Scene.Dummies;

public class SeDummyDefinitionNode : SeDefinitionNode, IResourceSerializable
{
    /// <inheritdoc />
    public void Load(ResourceLoadContext context)
    {
        context.Position = LoadInternal(context, context.Position);
    }
}