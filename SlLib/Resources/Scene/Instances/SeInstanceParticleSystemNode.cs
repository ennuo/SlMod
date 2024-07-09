using System.Numerics;
using SlLib.Resources.Database;
using SlLib.Serialization;

namespace SlLib.Resources.Scene.Instances;

public class SeInstanceParticleSystemNode : SeInstanceTransformNode
{
    public bool RenderGroup0
     {
        get => (SystemFlags & (1 << 0)) != 0;
        set
        {
            if (value) SystemFlags &= ~(1 << 0);
            else SystemFlags |= (1 << 0);
        }
    }

    public bool RenderGroup1
     {
        get => (SystemFlags & (1 << 1)) != 0;
        set
        {
            if (value) SystemFlags &= ~(1 << 1);
            else SystemFlags |= (1 << 1);
        }
    }

    public bool RenderGroup2
     {
        get => (SystemFlags & (1 << 2)) != 0;
        set
        {
            if (value) SystemFlags &= ~(1 << 2);
            else SystemFlags |= (1 << 2);
        }
    }

    public bool RenderGroup3
     {
        get => (SystemFlags & (1 << 3)) != 0;
        set
        {
            if (value) SystemFlags &= ~(1 << 3);
            else SystemFlags |= (1 << 3);
        }
    }
    
    public int SystemFlags;
    public Vector4 ColourAdd;
    public Vector4 ColourMul;

    /// <inheritdoc />
    public override void Load(ResourceLoadContext context)
    {
        base.Load(context);
        
        SystemFlags = context.ReadBitset32(0x16c);
        ColourAdd = context.ReadFloat4(0x170);
        ColourMul = context.ReadFloat4(0x180);
    }

    /// <inheritdoc />
    public override void Save(ResourceSaveContext context, ISaveBuffer buffer)
    {
        base.Save(context, buffer);
        
        context.WriteInt32(buffer, SystemFlags, 0x16c);
        context.WriteFloat4(buffer, ColourAdd, 0x170);
        context.WriteFloat4(buffer, ColourMul, 0x180);
    }

    /// <inheritdoc />
    public override int GetSizeForSerialization(SlPlatform platform, int version) => 0x1b0;
}
