using SlLib.Serialization;

namespace SlLib.Resources.Scene.Dummies;

public class SeDummyInstanceNode : SeInstanceNode, IResourceSerializable
{
    /// <inheritdoc />
    public void Load(ResourceLoadContext context)
    {
        context.Position = LoadInternal(context, context.Position);
    }
}