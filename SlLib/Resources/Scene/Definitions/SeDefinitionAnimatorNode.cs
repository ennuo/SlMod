using SlLib.Resources.Database;
using SlLib.Serialization;

namespace SlLib.Resources.Scene.Definitions;

// SE_ANIMATOR_
// CharacterMayaFile
// assets/defaults/character/{CharacterName}/scenes/{CharacterMayaFile}.mb:se_entity_{CharacterEntity}|se_animator_{CharacterEntity}.skeleton

public class SeDefinitionAnimatorNode : SeDefinitionTransformNode, IResourceSerializable
{
    public override string Prefix => "SE_ANIMATOR_";
    public override string Extension => ".skeleton";
    
    /// <summary>
    ///     The model associated with this entity.
    /// </summary>
    public SlResPtr<SlSkeleton> Skeleton = new();

    /// <inheritdoc />
    public void Load(ResourceLoadContext context)
    {
        context.Position = LoadInternal(context, context.Position);
        Skeleton = context.LoadResource<SlSkeleton>(Uid);
    }
}