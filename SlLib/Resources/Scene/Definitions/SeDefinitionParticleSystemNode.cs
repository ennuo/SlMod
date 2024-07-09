using SlLib.Resources.Database;
using SlLib.Serialization;

namespace SlLib.Resources.Scene.Definitions;

public class SeDefinitionParticleSystemNode : SeDefinitionTransformNode
{
    public bool WorldSpace
    {
        get => (SystemFlagsBitField & (1 << 0)) != 0;
        set
        {
            if (value) SystemFlagsBitField &= ~(1 << 0);
            else SystemFlagsBitField |= (1 << 0);
        }
    }

    public bool MaxClipSizeDisabled
    {
        get => (SystemFlagsBitField & (1 << 1)) != 0;
        set
        {
            if (value) SystemFlagsBitField &= ~(1 << 1);
            else SystemFlagsBitField |= (1 << 1);
        }
    }
    
    public bool ForceOpaquePass
    {
        get => (SystemFlagsBitField & (1 << 6)) != 0;
        set
        {
            if (value) SystemFlagsBitField &= ~(1 << 6);
            else SystemFlagsBitField |= (1 << 6);
        }
    }

    public int SystemFlagsBitField;
    public float MaxClipSize;

    /// <inheritdoc />
    public override void Load(ResourceLoadContext context)
    {
        base.Load(context);
        
        // DrawOrder is in the bitfield
        
        SystemFlagsBitField = context.ReadBitset32(0xd0);
        MaxClipSize = context.ReadFloat(0xdc);
    }

    /// <inheritdoc />
    public override void Save(ResourceSaveContext context, ISaveBuffer buffer)
    {
        base.Save(context, buffer);
        
        context.WriteInt32(buffer, SystemFlagsBitField, 0xd0);
        context.WriteFloat(buffer, MaxClipSize, 0xdc);
    }

    /// <inheritdoc />
    public override int GetSizeForSerialization(SlPlatform platform, int version) => 0xe0;
}