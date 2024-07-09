using System.Numerics;
using SlLib.Resources.Database;
using SlLib.Serialization;

namespace SlLib.Resources.Scene.Instances;

public class SeFogInstanceNode : SeInstanceNode
{
    public Vector4 FogColour;
    public float FogColourIntensity;
    public float FogStart;
    public float FogEnd;
    public float FogMax;
    public float GroundFogTop;
    public float GroundFogBottom;
    public float GroundFogMax;

    /// <inheritdoc />
    public override void Load(ResourceLoadContext context)
    {
        context.Position = LoadInternal(context, context.Position);

        FogColour = context.ReadFloat4(0x80);
        FogColourIntensity = context.ReadFloat(0x90);
        FogStart = context.ReadFloat(0x98);
        FogEnd = context.ReadFloat(0x9c);
        FogMax = context.ReadFloat(0xa0);
        GroundFogTop = context.ReadFloat(0xa4);
        GroundFogBottom = context.ReadFloat(0xa8);
        GroundFogMax = context.ReadFloat(0xac);
    }

    /// <inheritdoc />
    public override void Save(ResourceSaveContext context, ISaveBuffer buffer)
    {
        base.Save(context, buffer);

        context.WriteFloat4(buffer, FogColour, 0x80);
        context.WriteFloat(buffer, FogColourIntensity, 0x90);
        context.WriteFloat(buffer, FogStart, 0x98);
        context.WriteFloat(buffer, FogEnd, 0x9c);
        context.WriteFloat(buffer, FogMax, 0xa0);
        context.WriteFloat(buffer, GroundFogTop, 0xa4);
        context.WriteFloat(buffer, GroundFogBottom, 0xa8);
        context.WriteFloat(buffer, GroundFogMax, 0xac);
    }

    /// <inheritdoc />
    public override int GetSizeForSerialization(SlPlatform platform, int version) => 0xb0;
}