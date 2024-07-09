using SlLib.Resources.Database;
using SlLib.Serialization;

namespace SlLib.Resources.Scene.Definitions;

// SE_ANIMATOR_
// CharacterMayaFile
// assets/defaults/character/{CharacterName}/scenes/{CharacterMayaFile}.mb:se_entity_{CharacterEntity}|se_animator_{CharacterEntity}.skeleton

public class SeDefinitionAnimatorNode : SeDefinitionTransformNode
{
    public override string Prefix => "SE_ANIMATOR_";
    public override string Extension => ".skeleton";
    
    /// <summary>
    ///     The model associated with this entity.
    /// </summary>
    public SlResPtr<SlSkeleton> Skeleton = new();

    /// <inheritdoc />
    public override void Load(ResourceLoadContext context)
    {
        base.Load(context);
        Skeleton = context.LoadResource<SlSkeleton>(Uid);
    }
    
    /// <inheritdoc />
    public override int GetSizeForSerialization(SlPlatform platform, int version) => 0xe0;
}