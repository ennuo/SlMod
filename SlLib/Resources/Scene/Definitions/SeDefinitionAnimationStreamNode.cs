using SlLib.Resources.Database;
using SlLib.Serialization;

namespace SlLib.Resources.Scene.Definitions;

// SE_ANIM_STREAM_
public class SeDefinitionAnimationStreamNode : SeDefinitionNode
{
    /// <summary>
    ///     The animation played by this node.
    /// </summary>
    public SlResPtr<SlAnim> Animation = new();

    /// <summary>
    ///     Whether this animation should loop.
    /// </summary>
    public bool PlayLooped;

    /// <summary>
    ///     Whether this animation should autoplay.
    /// </summary>
    public bool AutoPlay;

    /// <inheritdoc />
    public override void Load(ResourceLoadContext context)
    {
        context.Position = LoadInternal(context, context.Position);
        Animation = context.LoadResource<SlAnim>(Uid);
        context.Position += context.Platform.GetPointerSize() * 0x2;
        PlayLooped = context.ReadBoolean(true);
        AutoPlay = context.ReadBoolean(true);
    }
    
    /// <inheritdoc />
    public override void Save(ResourceSaveContext context, ISaveBuffer buffer)
    {
        base.Save(context, buffer);

        context.WriteBoolean(buffer, PlayLooped, 0x88, wide: true);
        context.WriteBoolean(buffer, AutoPlay, 0x8c, wide: true);
    }
    
    /// <inheritdoc />
    public override int GetSizeForSerialization(SlPlatform platform, int version) => 0x90;
}