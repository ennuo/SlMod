using SlLib.Serialization;

namespace SlLib.Resources.Scene.Instances;

public class SeInstanceAnimationStreamNode : SeInstanceNode, IResourceSerializable
{
    // 05 = int
    // 07 = float
    
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
        context.Position = LoadInternal(context, context.Position);
        Playing = context.ReadInt32(0x80);
        PlayLooped = context.ReadBoolean(0x84, wide: true);
        AutoPlay = context.ReadBoolean(0x88, wide: true);
        context.Position += 4;
        LoopCount = context.ReadInt32(0x90);
        StreamWeight = context.ReadFloat(0x94);
        BlendTranslateMode = context.ReadInt32(0x98);
        BlendRotateMode = context.ReadInt32(0x9c);
        BlendScaleMode = context.ReadInt32(0xa0);
        Duration = context.ReadFloat(0xa4);
        
        // + 0x80 = Playing
        // + 0x84 = Play Looped
        // + 0x88 = Auto Play
        // + 0x90 = Loop Count
        // + 0x94 = Stream Weight
        // + 0x98 = Blend Translate Mode
        // + 0x9c = Blend Rotate Mode
        // + 0xa0 = Blend Scale Mode
        // + 0xa4 = Duration
    }
}