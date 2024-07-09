using System.Numerics;
using SlLib.Resources.Database;
using SlLib.Serialization;

namespace SlLib.Resources.Scene.Instances;

public class SeInstanceParticleEmitterNode : SeInstanceTransformNode
{
    public Vector4 ScaleMul;
    public Vector4 ColourMul;
    public Vector4 VelocityMul;
    public float LifeMul;
    public float OffsetU;
    public float OffsetV;
    public int MAnimParamsSpawnRateChannel;
    public int MAnimParamsSpawnSpeedChannel;
    public float SpawnRateModifier;
    public float NoiseOffset;

    /// <inheritdoc />
    public override void Load(ResourceLoadContext context)
    {
        base.Load(context);

        ScaleMul = context.ReadFloat4(0x160);
        ColourMul = context.ReadFloat4(0x170);
        VelocityMul = context.ReadFloat4(0x180);
        LifeMul = context.ReadFloat(0x190);
        OffsetU = context.ReadFloat(0x194);
        OffsetV = context.ReadFloat(0x198);
        MAnimParamsSpawnRateChannel = context.ReadInt32(0x1a0);
        MAnimParamsSpawnSpeedChannel = context.ReadInt32(0x1a4);
        SpawnRateModifier = context.ReadFloat(0x200);
        NoiseOffset = context.ReadFloat(0x20c);
    }

    /// <inheritdoc />
    public override void Save(ResourceSaveContext context, ISaveBuffer buffer)
    {
        base.Save(context, buffer);

        context.WriteFloat4(buffer, ScaleMul, 0x160);
        context.WriteFloat4(buffer, ColourMul, 0x170);
        context.WriteFloat4(buffer, VelocityMul, 0x180);
        context.WriteFloat(buffer, LifeMul, 0x190);
        context.WriteFloat(buffer, OffsetU, 0x194);
        context.WriteFloat(buffer, OffsetV, 0x198);
        context.WriteInt32(buffer, MAnimParamsSpawnRateChannel, 0x1a0);
        context.WriteInt32(buffer, MAnimParamsSpawnSpeedChannel, 0x1a4);
        context.WriteFloat(buffer, SpawnRateModifier, 0x200);
        context.WriteFloat(buffer, NoiseOffset, 0x20c);
        
        // Not sure if it's needed to serialize this matrix, but let's just do so anyway
        context.WriteMatrix(buffer, Matrix4x4.Identity, 0x1c0);
    }

    /// <inheritdoc />
    public override int GetSizeForSerialization(SlPlatform platform, int version) => 0x210;
}
