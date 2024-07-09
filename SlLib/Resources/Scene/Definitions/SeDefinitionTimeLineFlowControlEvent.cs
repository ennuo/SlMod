using SlLib.Resources.Database;
using SlLib.Serialization;

namespace SlLib.Resources.Scene.Definitions;

public class SeDefinitionTimeLineFlowControlEvent : SeDefinitionTimeLineEventBaseNode
{
    public bool AutoLoopCountReset;
    public int LoopCount;

    /// <inheritdoc />
    public override void Load(ResourceLoadContext context)
    {
        base.Load(context);

        AutoLoopCountReset = context.ReadBoolean(0x90);
        LoopCount = context.ReadInt32(0x94);
    }

    /// <inheritdoc />
    public override void Save(ResourceSaveContext context, ISaveBuffer buffer)
    {
        base.Save(context, buffer);

        context.WriteBoolean(buffer, AutoLoopCountReset, 0x90);
        context.WriteInt32(buffer, LoopCount, 0x94);
    }

    /// <inheritdoc />
    public override int GetSizeForSerialization(SlPlatform platform, int version) => 0x98;
}