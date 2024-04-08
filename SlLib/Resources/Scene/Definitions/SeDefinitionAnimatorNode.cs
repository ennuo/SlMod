using SlLib.Resources.Database;
using SlLib.Serialization;

namespace SlLib.Resources.Scene.Definitions;

// SE_ANIMATOR_
// CharacterMayaFile
// assets/defaults/character/{CharacterName}/scenes/{CharacterMayaFile}.mb:se_entity_{CharacterEntity}|se_animator_{CharacterEntity}.skeleton

public class SeDefinitionAnimatorNode : SeDefinitionTransformNode, ILoadable
{
    public override bool NodeNameIsFilename => true;

    /// <summary>
    ///     The model associated with this entity.
    /// </summary>
    public SlResPtr<SlSkeleton> Skeleton = SlResPtr<SlSkeleton>.Empty();

    /// <inheritdoc />
    public void Load(ResourceLoadContext context, int offset)
    {
        offset = LoadInternal(context, offset);
        Skeleton = context.LoadResource<SlSkeleton>(Uid);
    }
}