using System.Numerics;
using SlLib.Resources.Database;
using SlLib.Serialization;

namespace SlLib.Resources.Scene.Definitions;

public class SeDefinitionParticleAffectorTurbulanceNode : SeDefinitionParticleAffectorNode
{
    public Vector4 Magnitude;
    public Vector4 Frequency;
    public Vector4 Speed;
    public Vector4 Offset;

    /// <inheritdoc />
    public override void Load(ResourceLoadContext context)
    {
        context.Position = LoadInternal(context, context.Position);

        Magnitude = context.ReadFloat4(0x110);
        Frequency = context.ReadFloat4(0x120);
        Speed = context.ReadFloat4(0x130);
        Offset = context.ReadFloat4(0x140);
    }

    /// <inheritdoc />
    public override void Save(ResourceSaveContext context, ISaveBuffer buffer)
    {
        base.Save(context, buffer);

        context.WriteFloat4(buffer, Magnitude, 0x110);
        context.WriteFloat4(buffer, Frequency, 0x120);
        context.WriteFloat4(buffer, Speed, 0x130);
        context.WriteFloat4(buffer, Offset, 0x140);
    }

    /// <inheritdoc />
    public override int GetSizeForSerialization(SlPlatform platform, int version) => 0x150;
}