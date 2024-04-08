using SlLib.Resources.Database;
using SlLib.Serialization;

namespace SlLib.Resources.Scene.Definitions;

// SE_ANIM_STREAM_
public class SeDefinitionAnimationStreamNode : SeDefinitionNode, ILoadable
{
    public override bool NodeNameIsFilename => true;

    /// <summary>
    ///     The animation played by this node.
    /// </summary>
    public SlResPtr<SlAnim> Animation = SlResPtr<SlAnim>.Empty();

    /// <summary>
    ///     Whether or not this animation should loop.
    /// </summary>
    public bool PlayLooped;

    /// <summary>
    ///     Whether or not this animation should auto play.
    /// </summary>
    public bool AutoPlay;

    /// <inheritdoc />
    public void Load(ResourceLoadContext context, int offset)
    {
        offset = LoadInternal(context, offset);
        Animation = context.LoadResource<SlAnim>(Uid);
        PlayLooped = context.ReadBoolean(offset + 0x8, true);
        AutoPlay = context.ReadBoolean(offset + 0xc, true);
    }
}