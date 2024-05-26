using SlLib.Resources.Database;
using SlLib.Serialization;

namespace SlLib.Resources.Scene.Definitions;

// SE_ENTITY_
public class SeDefinitionEntityNode : SeDefinitionTransformNode, IResourceSerializable
{
    public override bool NodeNameIsFilename => true;

    /// <summary>
    ///     The model associated with this entity.
    /// </summary>
    public SlResPtr<SlModel> Model = new();

    /// <inheritdoc />
    public void Load(ResourceLoadContext context)
    {
        context.Position = LoadInternal(context, context.Position);
        Model = context.LoadResource<SlModel>(Uid);
    }
}