using SlLib.Resources.Database;
using SlLib.Serialization;

namespace SlLib.Resources.Scene.Instances;

public class SeInstanceAnimatorNode : SeInstanceTransformNode, IResourceSerializable
{
    /// <inheritdoc />
    public void Load(ResourceLoadContext context)
    {
        context.Position = LoadInternal(context, context.Position);
    }
}