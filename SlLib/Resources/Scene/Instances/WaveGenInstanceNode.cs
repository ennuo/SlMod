using SlLib.Resources.Database;
using SlLib.Serialization;

namespace SlLib.Resources.Scene.Instances;

public class WaveGenInstanceNode : SeInstanceTransformNode
{
    public float InitialAmplitude;
    public float InitialRadius;
    public float Width;
    public float FallOff;
    public float RadiusRate;
    public float LifeTime;
    public float DecelerateRate;
    public float Frequency;
    // public float AmplitudeRate;

    /// <inheritdoc />
    public override void Load(ResourceLoadContext context)
    {
        base.Load(context);

        InitialAmplitude = context.ReadFloat(0x160);
        InitialRadius = context.ReadFloat(0x164);
        Width = context.ReadFloat(0x168);
        FallOff = context.ReadFloat(0x16c);
        RadiusRate = context.ReadFloat(0x170);
        LifeTime = context.ReadFloat(0x174);
        DecelerateRate = context.ReadFloat(0x178);
        Frequency = context.ReadFloat(0x17c);
    }

    /// <inheritdoc />
    public override void Save(ResourceSaveContext context, ISaveBuffer buffer)
    {
        base.Save(context, buffer);

        context.WriteFloat(buffer, InitialAmplitude, 0x160);
        context.WriteFloat(buffer, InitialRadius, 0x164);
        context.WriteFloat(buffer, Width, 0x168);
        context.WriteFloat(buffer, FallOff, 0x16c);
        context.WriteFloat(buffer, RadiusRate, 0x170);
        context.WriteFloat(buffer, LifeTime, 0x174);
        context.WriteFloat(buffer, DecelerateRate, 0x178);
        context.WriteFloat(buffer, Frequency, 0x17c);
    }

    /// <inheritdoc />
    public override int GetSizeForSerialization(SlPlatform platform, int version) => 0x190;
}