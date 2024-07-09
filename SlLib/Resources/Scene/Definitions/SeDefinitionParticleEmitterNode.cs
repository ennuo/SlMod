using System.Numerics;
using SlLib.Resources.Database;
using SlLib.Serialization;

namespace SlLib.Resources.Scene.Definitions;

public class SeDefinitionParticleEmitterNode : SeDefinitionTransformNode
{
    public bool ConstantSpawn
     {
        get => (SlParticleEmitterTimingParamsFlags & (1 << 0)) != 0;
        set
        {
            if (value) SlParticleEmitterTimingParamsFlags &= ~(1 << 0);
            else SlParticleEmitterTimingParamsFlags |= (1 << 0);
        }
    }

    public bool WaitForTrigger
     {
        get => (SlParticleEmitterTimingParamsFlags & (1 << 1)) != 0;
        set
        {
            if (value) SlParticleEmitterTimingParamsFlags &= ~(1 << 1);
            else SlParticleEmitterTimingParamsFlags |= (1 << 1);
        }
    }

    public bool ParticlesPerMetre
     {
        get => (SlParticleEmitterTimingParamsFlags & (1 << 2)) != 0;
        set
        {
            if (value) SlParticleEmitterTimingParamsFlags &= ~(1 << 2);
            else SlParticleEmitterTimingParamsFlags |= (1 << 2);
        }
    }

    public bool SpawnRateNoise
     {
        get => (SlParticleEmitterTimingParamsFlags & (1 << 3)) != 0;
        set
        {
            if (value) SlParticleEmitterTimingParamsFlags &= ~(1 << 3);
            else SlParticleEmitterTimingParamsFlags |= (1 << 3);
        }
    }

    public int SlParticleEmitterTimingParamsFlags;
    public float SpawnRate;
    public float SpawnDuration;
    public float SpawnDurationRandom;
    public int NumSpawns;
    public float SpawnDelay;
    public float SpawnDelayRandom;
    public float InitialDelay;
    public float InitialDelayRandom;
    public float PPM;
    public float SpawnRateNoiseAdd;
    public float SpawnRateNoiseSpeed;
    public Vector4 PositionAngle;
    public Vector4 PositionAngleRandom;
    public Vector4 VelocitySpin;
    public Vector4 VelocitySpinRandom;
    public int EmitterShape;
    public bool LinkPosAndVel
     {
        get => (SpawnParamsFlags & (1 << 0)) != 0;
        set
        {
            if (value) SpawnParamsFlags &= ~(1 << 0);
            else SpawnParamsFlags |= (1 << 0);
        }
    }

    public bool InterFrameInterpolation
     {
        get => (SpawnParamsFlags & (1 << 1)) != 0;
        set
        {
            if (value) SpawnParamsFlags &= ~(1 << 1);
            else SpawnParamsFlags |= (1 << 1);
        }
    }

    public bool EmitterVelocity
     {
        get => (SpawnParamsFlags & (1 << 2)) != 0;
        set
        {
            if (value) SpawnParamsFlags &= ~(1 << 2);
            else SpawnParamsFlags |= (1 << 2);
        }
    }

    public bool NormaliseVelocity
     {
        get => (SpawnParamsFlags & (1 << 3)) != 0;
        set
        {
            if (value) SpawnParamsFlags &= ~(1 << 3);
            else SpawnParamsFlags |= (1 << 3);
        }
    }

    public int SpawnParamsFlags;
    public float RadiusA;
    public float RadiusB;
    public float RadialStart;
    public float RadialSize;
    public float PolarStart;
    public float PolarSize;
    public float EmitterVelocityMul;
    public float OffsetU;
    public float OffsetV;
    public SeDefinitionParticleStyleNode? ParticleStyle;
    public string ParticleStyleName;

    /// <inheritdoc />
    public override void Load(ResourceLoadContext context)
    {
        base.Load(context);

        SlParticleEmitterTimingParamsFlags = context.ReadInt32(0xd0);
        SpawnRate = context.ReadFloat(0xd4);
        SpawnDuration = context.ReadFloat(0xd8);
        SpawnDurationRandom = context.ReadFloat(0xdc);
        NumSpawns = context.ReadInt32(0xe0);
        SpawnDelay = context.ReadFloat(0xe4);
        SpawnDelayRandom = context.ReadFloat(0xe8);
        InitialDelay = context.ReadFloat(0xec);
        InitialDelayRandom = context.ReadFloat(0xf0);
        PPM = context.ReadFloat(0xf4);
        SpawnRateNoiseAdd = context.ReadFloat(0xf8);
        SpawnRateNoiseSpeed = context.ReadFloat(0xfc);
        PositionAngle = context.ReadFloat4(0x100);
        PositionAngleRandom = context.ReadFloat4(0x110);
        VelocitySpin = context.ReadFloat4(0x120);
        VelocitySpinRandom = context.ReadFloat4(0x130);
        EmitterShape = context.ReadInt32(0x140);
        SpawnParamsFlags = context.ReadInt32(0x144);
        RadiusA = context.ReadFloat(0x148);
        RadiusB = context.ReadFloat(0x14c);
        RadialStart = context.ReadFloat(0x150);
        RadialSize = context.ReadFloat(0x154);
        PolarStart = context.ReadFloat(0x158);
        PolarSize = context.ReadFloat(0x15c);
        EmitterVelocityMul = context.ReadFloat(0x160);
        OffsetU = context.ReadFloat(0x164);
        OffsetV = context.ReadFloat(0x168);
        ParticleStyle = (SeDefinitionParticleStyleNode?)context.LoadNode(context.ReadInt32(0x198));
        ParticleStyleName = context.ReadStringPointer(0x19c);
    }

    /// <inheritdoc />
    public override void Save(ResourceSaveContext context, ISaveBuffer buffer)
    {
        base.Save(context, buffer);

        context.WriteInt32(buffer, SlParticleEmitterTimingParamsFlags, 0xd0);
        context.WriteFloat(buffer, SpawnRate, 0xd4);
        context.WriteFloat(buffer, SpawnDuration, 0xd8);
        context.WriteFloat(buffer, SpawnDurationRandom, 0xdc);
        context.WriteInt32(buffer, NumSpawns, 0xe0);
        context.WriteFloat(buffer, SpawnDelay, 0xe4);
        context.WriteFloat(buffer, SpawnDelayRandom, 0xe8);
        context.WriteFloat(buffer, InitialDelay, 0xec);
        context.WriteFloat(buffer, InitialDelayRandom, 0xf0);
        context.WriteFloat(buffer, PPM, 0xf4);
        context.WriteFloat(buffer, SpawnRateNoiseAdd, 0xf8);
        context.WriteFloat(buffer, SpawnRateNoiseSpeed, 0xfc);
        context.WriteFloat4(buffer, PositionAngle, 0x100);
        context.WriteFloat4(buffer, PositionAngleRandom, 0x110);
        context.WriteFloat4(buffer, VelocitySpin, 0x120);
        context.WriteFloat4(buffer, VelocitySpinRandom, 0x130);
        context.WriteInt32(buffer, EmitterShape, 0x140);
        context.WriteInt32(buffer, SpawnParamsFlags, 0x144);
        context.WriteFloat(buffer, RadiusA, 0x148);
        context.WriteFloat(buffer, RadiusB, 0x14c);
        context.WriteFloat(buffer, RadialStart, 0x150);
        context.WriteFloat(buffer, RadialSize, 0x154);
        context.WriteFloat(buffer, PolarStart, 0x158);
        context.WriteFloat(buffer, PolarSize, 0x15c);
        context.WriteFloat(buffer, EmitterVelocityMul, 0x160);
        context.WriteFloat(buffer, OffsetU, 0x164);
        context.WriteFloat(buffer, OffsetV, 0x168);
        context.WriteInt32(buffer, ParticleStyle?.Uid ?? 0, 0x198);
        context.WriteStringPointer(buffer, ParticleStyleName, 0x19c);
    }

    /// <inheritdoc />
    public override int GetSizeForSerialization(SlPlatform platform, int version) => 0x1c0;
}

public enum EmitterShapeEnumSet
{
	Default = 0,
	Eliptical = 1,
	BoxFrameXY = 2,

}

public enum ParticleStyleEnumSet
{
	SeDefinitionParticleStyleNode = 93476000,

}
