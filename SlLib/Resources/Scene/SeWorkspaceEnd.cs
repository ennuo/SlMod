using SlLib.Serialization;

namespace SlLib.Resources.Scene;

public class SeWorkspaceEnd : SeDefinitionNode, IResourceSerializable
{
    /// <inheritdoc />
    public override void Load(ResourceLoadContext context)
    {
        context.Position = LoadInternal(context, context.Position);
    }
}