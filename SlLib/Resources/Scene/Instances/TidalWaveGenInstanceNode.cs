using SlLib.Resources.Database;
using SlLib.Serialization;

namespace SlLib.Resources.Scene.Instances;

public class TidalWaveGenInstanceNode : SeInstanceTransformNode
{
    public float Amplitude;
    public float LeadingWidth;
    public float TrailingWidth;
    public float LeadingPow;
    public float TrailingPow;
    public float Speed;
    public float AttackTime;
    public float SustainTime;
    public float DecayTime;
    public float Direction;
    public float Frequency;

    /// <inheritdoc />
    public override void Load(ResourceLoadContext context)
    {
        context.Position = LoadInternal(context, context.Position);

        Amplitude = context.ReadFloat(0x160);
        LeadingWidth = context.ReadFloat(0x164);
        TrailingWidth = context.ReadFloat(0x168);
        LeadingPow = context.ReadFloat(0x16c);
        TrailingPow = context.ReadFloat(0x170);
        Speed = context.ReadFloat(0x174);
        AttackTime = context.ReadFloat(0x178);
        SustainTime = context.ReadFloat(0x17c);
        DecayTime = context.ReadFloat(0x180);
        Direction = context.ReadFloat(0x184);
        Frequency = context.ReadFloat(0x188);
    }

    /// <inheritdoc />
    public override void Save(ResourceSaveContext context, ISaveBuffer buffer)
    {
        base.Save(context, buffer);

        context.WriteFloat(buffer, Amplitude, 0x160);
        context.WriteFloat(buffer, LeadingWidth, 0x164);
        context.WriteFloat(buffer, TrailingWidth, 0x168);
        context.WriteFloat(buffer, LeadingPow, 0x16c);
        context.WriteFloat(buffer, TrailingPow, 0x170);
        context.WriteFloat(buffer, Speed, 0x174);
        context.WriteFloat(buffer, AttackTime, 0x178);
        context.WriteFloat(buffer, SustainTime, 0x17c);
        context.WriteFloat(buffer, DecayTime, 0x180);
        context.WriteFloat(buffer, Direction, 0x184);
        context.WriteFloat(buffer, Frequency, 0x188);
    }

    /// <inheritdoc />
    public override int GetSizeForSerialization(SlPlatform platform, int version) => 0x190;
}
