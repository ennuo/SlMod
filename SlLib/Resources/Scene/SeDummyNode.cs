using SlLib.Resources.Scene.Definitions;
using SlLib.Serialization;

namespace SlLib.Resources.Scene;

public class SeDummyNode : SeGraphNode, IResourceSerializable
{
    public override bool NodeNameIsFilename => false;
    
    /// <inheritdoc />
    public void Load(ResourceLoadContext context)
    {
        context.Position = LoadInternal(context, context.Position);
    }
}