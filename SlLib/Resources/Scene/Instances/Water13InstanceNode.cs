using SlLib.Resources.Database;
using SlLib.Serialization;

namespace SlLib.Resources.Scene.Instances;

public class Water13InstanceNode : SeInstanceTransformNode
{
    public float PhysicsVelocityX;
    public float PhysicsVelocityZ;
    public float FillSlope;
    public float FillRate;
    public float FillDelay;
    public float DrainRate;
    public float DrainDelay;
    
    public int FlagSerialise;
    public int Version;
    public int SurfaceWavesUid;
    public int ShaderDefinitionUid;

    /// <inheritdoc />
    public override void Load(ResourceLoadContext context)
    {
        base.Load(context);

        PhysicsVelocityX = context.ReadFloat(0x170);
        PhysicsVelocityZ = context.ReadFloat(0x174);
        FillSlope = context.ReadFloat(0x184);
        FillRate = context.ReadFloat(0x18c);
        FillDelay = context.ReadFloat(0x190);
        DrainRate = context.ReadFloat(0x194);
        DrainDelay = context.ReadFloat(0x198);
        FlagSerialise = context.ReadInt32(0x1a8);
        Version = context.ReadInt32(0x1b8);
        SurfaceWavesUid = context.ReadInt32(0x1d0);
        ShaderDefinitionUid = context.ReadInt32(0x1d4);
    }

    /// <inheritdoc />
    public override void Save(ResourceSaveContext context, ISaveBuffer buffer)
    {
        base.Save(context, buffer);

        context.WriteFloat(buffer, PhysicsVelocityX, 0x170);
        context.WriteFloat(buffer, PhysicsVelocityZ, 0x174);
        context.WriteFloat(buffer, FillSlope, 0x184);
        context.WriteFloat(buffer, FillRate, 0x18c);
        context.WriteFloat(buffer, FillDelay, 0x190);
        context.WriteFloat(buffer, DrainRate, 0x194);
        context.WriteFloat(buffer, DrainDelay, 0x198);
        context.WriteInt32(buffer, FlagSerialise, 0x1a8);
        context.WriteInt32(buffer, Version, 0x1b8);
        context.WriteInt32(buffer, SurfaceWavesUid, 0x1d0);
        context.WriteInt32(buffer, ShaderDefinitionUid, 0x1d4);
        
        // ???
        context.WriteFloat(buffer, 1.0f, 0x1a0);
        context.WriteInt32(buffer, 1, 0x1d8);
        context.WriteFloat(buffer, 10000.0f, 0x19c);
        
    }

    /// <inheritdoc />
    public override int GetSizeForSerialization(SlPlatform platform, int version) => 0x220;
    
    public bool AlwaysFilled
    {
        get => (FlagSerialise & (1 << 0)) != 0;
        set
        {
            if (value) FlagSerialise &= ~(1 << 0);
            else FlagSerialise |= (1 << 0);
        }
    }

    public bool UseFlowSpeed
    {
        get => (FlagSerialise & (1 << 1)) != 0;
        set
        {
            if (value) FlagSerialise &= ~(1 << 1);
            else FlagSerialise |= (1 << 1);
        }
    }
}
