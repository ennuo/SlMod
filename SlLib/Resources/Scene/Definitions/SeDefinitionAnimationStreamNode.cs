using SlLib.Resources.Database;
using SlLib.Serialization;

namespace SlLib.Resources.Scene.Definitions;

// SE_ANIM_STREAM_
public class SeDefinitionAnimationStreamNode : SeDefinitionNode, IResourceSerializable
{
    /// <summary>
    ///     The animation played by this node.
    /// </summary>
    public SlResPtr<SlAnim> Animation = new();

    /// <summary>
    ///     Whether or not this animation should loop.
    /// </summary>
    public bool PlayLooped;

    /// <summary>
    ///     Whether or not this animation should auto play.
    /// </summary>
    public bool AutoPlay;

    /// <inheritdoc />
    public void Load(ResourceLoadContext context)
    {
        context.Position = LoadInternal(context, context.Position);
        Animation = context.LoadResource<SlAnim>(Uid);
        context.Position += context.Platform.GetPointerSize() * 0x2;
        PlayLooped = context.ReadBoolean(true);
        AutoPlay = context.ReadBoolean(true);
    }
}