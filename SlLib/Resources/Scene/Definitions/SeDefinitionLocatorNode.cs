using SlLib.Serialization;

namespace SlLib.Resources.Scene.Definitions;

// SE_LOCATOR_
public class SeDefinitionLocatorNode : SeDefinitionTransformNode, ILoadable
{
    public override bool NodeNameIsFilename => false;

    /// <inheritdoc />
    public void Load(ResourceLoadContext context, int offset)
    {
        LoadInternal(context, offset);
    }
}