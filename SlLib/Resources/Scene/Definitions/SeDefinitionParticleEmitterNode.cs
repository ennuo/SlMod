using SlLib.Serialization;

namespace SlLib.Resources.Scene.Definitions;

public class SeDefinitionParticleEmitterNode : SeDefinitionTransformNode
{
    /// <inheritdoc />
    public override void Load(ResourceLoadContext context)
    {
        context.Position = LoadInternal(context, context.Position);
        
        // starts @ 0xd0?
        
        // ConstantSpawn @ 0xd0
        // SpawnRate @ 0xd4
        // SpawnDuration @ 0xd8
        // SpawnDuration_Random @ 0xdc
        
        // NumSpawns @ 0xe0
        
        // SpawnDelay @ 0xe4
        // SpawnDelay_Random @ 0xe8
        
        // InitialDelay @ 0xec
        // InitialDelay_Random @ 0xf0
        
        // 0xf4 = ???
        
        // SpawnRateNoiseAdd @ 0xf8
        // SpawnRateNoiseSpeed @ 0xfc
        
        // PositionAngle @ 0x100
        // PositionAngle_Random @ 0x110
        
        // VelocitySpin @ 0x120
        // VelocitySpin_Random @ 0x130
        
        // EmitterShape @ 0x140
        // InterFrameInterpolation @ 0x144
        
        // Radius_A @ 0x148
        // Radius_B @ 0x14c
        // Radial_Start @ 0x150
        // Radial_Size @ 0x154
        // Polar_Start @ 0x158
        // Polar_Size @ 0x15c
        // EmitterVelocityMul @ 0x160
        // OffsetU @ 0x164
        // OffsetV @ 0x168
        // ParticleStyle @ 0x188
        // m_localStyleUIDHash @ 0x198
        // ParticleSyleName @ 0x19c
        
        
        
        
        
    }
}