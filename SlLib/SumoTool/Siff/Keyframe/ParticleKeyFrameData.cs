using System.Numerics;
using SlLib.Resources.Database;
using SlLib.Serialization;

namespace SlLib.SumoTool.Siff.Keyframe;

public class ParticleKeyFrameData : KeyframeData
{
    public int ParticleType;
    public int ParticlePerSecond;
    public int ParticleLifetime;
    public int ParticleDecayTime;
    public float ParticleStartSize;
    public float ParticleEndSize;
    public float ParticleSpeed;
    public float ParticleGravity;
    public float ParticleDrag;
    public Vector2 ParticleMinVel;
    public Vector2 ParticleMaxVel;
    public float StartRotation;
    public float EndRotation;

    public override void Load(ResourceLoadContext context)
    {
        base.Load(context);
        ParticleType = context.ReadInt32();
        ParticlePerSecond = context.ReadInt32();
        ParticleLifetime = context.ReadInt32();
        ParticleDecayTime = context.ReadInt32();
        ParticleStartSize = context.ReadFloat();
        ParticleEndSize = context.ReadFloat();
        ParticleSpeed = context.ReadFloat();
        ParticleGravity = context.ReadFloat();
        ParticleDrag = context.ReadFloat();
        ParticleMinVel = context.ReadFloat2();
        ParticleMaxVel = context.ReadFloat2();
        StartRotation = context.ReadFloat();
        EndRotation = context.ReadFloat();
    }
    
    public override void Save(ResourceSaveContext context, ISaveBuffer buffer)
    {
        base.Save(context, buffer);
        context.WriteInt32(buffer, ParticleType, 40);
        context.WriteInt32(buffer, ParticlePerSecond, 44);
        context.WriteInt32(buffer, ParticleLifetime, 48);
        context.WriteInt32(buffer, ParticleDecayTime, 52);
        context.WriteFloat(buffer, ParticleStartSize, 56);
        context.WriteFloat(buffer, ParticleEndSize, 60);
        context.WriteFloat(buffer, ParticleSpeed, 64);
        context.WriteFloat(buffer, ParticleGravity, 68);
        context.WriteFloat(buffer, ParticleDrag, 72);
        context.WriteFloat2(buffer, ParticleMinVel, 76);
        context.WriteFloat2(buffer, ParticleMaxVel, 84);
        context.WriteFloat(buffer, StartRotation, 92);
        context.WriteFloat(buffer, EndRotation, 96);
    }
    
    public override int GetSizeForSerialization(SlPlatform platform, int version)
    {
        return 0x64;
    }
}