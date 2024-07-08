using SlLib.Resources.Database;
using SlLib.Serialization;

namespace SlLib.Resources.Scene.Definitions;

public class SeFogDefinitionNode : SeDefinitionNode, IResourceSerializable
{
    /// <inheritdoc />
    public void Load(ResourceLoadContext context)
    {
        context.Position = LoadInternal(context, context.Position);
    }
}