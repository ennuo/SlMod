using SlLib.Resources.Database;
using SlLib.Serialization;

namespace SlLib.Resources.Scene.Definitions;

public class SeDefinitionParticleAffectorNode : SeDefinitionTransformNode
{
    public float Force;
    public float ForceRand;
    public float RandomSpeed;
    public int FalloffMode;
    public float FalloffPower;
    public float RadiusA;
    public int AffectorSetID;

    /// <inheritdoc />
    public override void Load(ResourceLoadContext context)
    {
        base.Load(context);

        Force = context.ReadFloat(0xd0);
        ForceRand = context.ReadFloat(0xd4);
        RandomSpeed = context.ReadFloat(0xd8);
        FalloffMode = context.ReadInt32(0xdc);
        FalloffPower = context.ReadFloat(0xe0);
        RadiusA = context.ReadFloat(0xe4);
        AffectorSetID = context.ReadInt32(0x100);
    }

    /// <inheritdoc />
    public override void Save(ResourceSaveContext context, ISaveBuffer buffer)
    {
        base.Save(context, buffer);

        context.WriteFloat(buffer, Force, 0xd0);
        context.WriteFloat(buffer, ForceRand, 0xd4);
        context.WriteFloat(buffer, RandomSpeed, 0xd8);
        context.WriteInt32(buffer, FalloffMode, 0xdc);
        context.WriteFloat(buffer, FalloffPower, 0xe0);
        context.WriteFloat(buffer, RadiusA, 0xe4);
        context.WriteInt32(buffer, AffectorSetID, 0x100);
    }

    /// <inheritdoc />
    public override int GetSizeForSerialization(SlPlatform platform, int version) => 0x110;
}