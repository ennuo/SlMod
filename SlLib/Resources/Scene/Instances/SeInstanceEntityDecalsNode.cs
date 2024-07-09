using SlLib.Serialization;

namespace SlLib.Resources.Scene.Instances;

public class SeInstanceEntityDecalsNode : SeInstanceEntityNode, IResourceSerializable
{
    /// <inheritdoc />
    public override void Load(ResourceLoadContext context)
    {
        context.Position = LoadInternal(context, context.Position);
    }
}