using SlLib.Resources.Database;
using SlLib.Serialization;

namespace SlLib.Resources.Scene.Definitions;

// SE_ENTITY_
public class SeDefinitionEntityNode : SeDefinitionTransformNode, ILoadable
{
    public override bool NodeNameIsFilename => true;

    /// <summary>
    ///     The model associated with this entity.
    /// </summary>
    public SlResPtr<SlModel> Model = SlResPtr<SlModel>.Empty();

    /// <inheritdoc />
    public void Load(ResourceLoadContext context, int offset)
    {
        offset = LoadInternal(context, offset);
        Model = context.LoadResource<SlModel>(Uid);
    }
}