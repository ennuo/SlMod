using SlLib.Resources.Database;
using SlLib.Serialization;

namespace SlLib.Resources.Scene.Instances;

public class SeToneMappingInstanceNode : SeInstanceNode
{
    public bool BloomEnabled;
    public float MiddleGray;
    public float LightPrePassFilterCutoff;
    public float BloomMultiplier;
    public float PattanaikAdaptRate;
    public float LightPrePassOffset;
    public int OutputTexture;
    public float BlurKernelSigmaAmount;
    public float BlueShiftScale;
    public float BlueShiftOffset;
    public int BlurPassCount;

    /// <inheritdoc />
    public override void Load(ResourceLoadContext context)
    {
        context.Position = LoadInternal(context, context.Position);

        BloomEnabled = context.ReadBoolean(0x84);
        MiddleGray = context.ReadFloat(0x88);
        LightPrePassFilterCutoff = context.ReadFloat(0x8c);
        BloomMultiplier = context.ReadFloat(0x90);
        PattanaikAdaptRate = context.ReadFloat(0x94);
        LightPrePassOffset = context.ReadFloat(0x98);
        OutputTexture = context.ReadInt32(0x9c);
        BlurKernelSigmaAmount = context.ReadFloat(0xa0);
        BlueShiftScale = context.ReadFloat(0xa4);
        BlueShiftOffset = context.ReadFloat(0xa8);
        BlurPassCount = context.ReadInt32(0xac);
    }

    /// <inheritdoc />
    public override void Save(ResourceSaveContext context, ISaveBuffer buffer)
    {
        base.Save(context, buffer);

        context.WriteBoolean(buffer, BloomEnabled, 0x84);
        context.WriteFloat(buffer, MiddleGray, 0x88);
        context.WriteFloat(buffer, LightPrePassFilterCutoff, 0x8c);
        context.WriteFloat(buffer, BloomMultiplier, 0x90);
        context.WriteFloat(buffer, PattanaikAdaptRate, 0x94);
        context.WriteFloat(buffer, LightPrePassOffset, 0x98);
        context.WriteInt32(buffer, OutputTexture, 0x9c);
        context.WriteFloat(buffer, BlurKernelSigmaAmount, 0xa0);
        context.WriteFloat(buffer, BlueShiftScale, 0xa4);
        context.WriteFloat(buffer, BlueShiftOffset, 0xa8);
        context.WriteInt32(buffer, BlurPassCount, 0xac);
    }

    /// <inheritdoc />
    public override int GetSizeForSerialization(SlPlatform platform, int version) => 0xb0;
}
