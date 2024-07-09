using SlLib.Serialization;

namespace SlLib.Resources.Scene;

public class SeProjectEnd : SeDefinitionNode, IResourceSerializable
{
    /// <inheritdoc />
    public override void Load(ResourceLoadContext context)
    {
        context.Position = LoadInternal(context, context.Position);
    }
}