using System.Numerics;
using SlLib.Resources.Database;
using SlLib.Serialization;

namespace SlLib.Resources.Scene.Instances;

public class SeInstanceParticleReferenceNode : SeInstanceTransformNode
{
    public int DrawOrderOffset;
    public int FlagsBitfield;
    public Vector4 ColourAdd;
    public Vector4 ColourMul;

    /// <inheritdoc />
    public override void Load(ResourceLoadContext context)
    {
        base.Load(context);

        DrawOrderOffset = context.ReadInt32(0x164);
        FlagsBitfield = context.ReadInt32(0x164);
        ColourAdd = context.ReadFloat4(0x1b0);
        ColourMul = context.ReadFloat4(0x1c0);
    }

    /// <inheritdoc />
    public override void Save(ResourceSaveContext context, ISaveBuffer buffer)
    {
        base.Save(context, buffer);

        context.WriteInt32(buffer, DrawOrderOffset, 0x164);
        context.WriteInt32(buffer, FlagsBitfield, 0x164);
        context.WriteFloat4(buffer, ColourAdd, 0x1b0);
        context.WriteFloat4(buffer, ColourMul, 0x1c0);
    }

    /// <inheritdoc />
    public override int GetSizeForSerialization(SlPlatform platform, int version) => 0x1d0;
}