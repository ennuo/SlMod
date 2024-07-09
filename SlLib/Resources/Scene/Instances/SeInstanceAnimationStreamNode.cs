using SlLib.Resources.Database;
using SlLib.Serialization;

namespace SlLib.Resources.Scene.Instances;

public class SeInstanceAnimationStreamNode : SeInstanceNode
{
    public int Playing;
    public bool PlayLooped;
    public bool AutoPlay;
    public int LoopCount;
    public float StreamWeight;
    public int BlendTranslateMode;
    public int BlendRotateMode;
    public int BlendScaleMode;
    public float Duration;

    /// <inheritdoc />
    public override void Load(ResourceLoadContext context)
    {
        base.Load(context);

        Playing = context.ReadInt32(0x80);
        PlayLooped = context.ReadBoolean(0x84, wide: true);
        AutoPlay = context.ReadBoolean(0x88, wide: true);
        LoopCount = context.ReadInt32(0x90);
        StreamWeight = context.ReadFloat(0x94);
        BlendTranslateMode = context.ReadInt32(0x98);
        BlendRotateMode = context.ReadInt32(0x9c);
        BlendScaleMode = context.ReadInt32(0xa0);
        Duration = context.ReadFloat(0xa4);
    }

    /// <inheritdoc />
    public override void Save(ResourceSaveContext context, ISaveBuffer buffer)
    {
        base.Save(context, buffer);

        context.WriteInt32(buffer, Playing, 0x80);
        context.WriteBoolean(buffer, PlayLooped, 0x84, wide: true);
        context.WriteBoolean(buffer, AutoPlay, 0x88, wide: true);
        context.WriteInt32(buffer, LoopCount, 0x90);
        context.WriteFloat(buffer, StreamWeight, 0x94);
        context.WriteInt32(buffer, BlendTranslateMode, 0x98);
        context.WriteInt32(buffer, BlendRotateMode, 0x9c);
        context.WriteInt32(buffer, BlendScaleMode, 0xa0);
        context.WriteFloat(buffer, Duration, 0xa4);
    }

    /// <inheritdoc />
    public override int GetSizeForSerialization(SlPlatform platform, int version) => 0xb0;
}