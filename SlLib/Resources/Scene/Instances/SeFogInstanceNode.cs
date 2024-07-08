using System.Numerics;
using SlLib.Serialization;

namespace SlLib.Resources.Scene.Instances;

public class SeFogInstanceNode : SeInstanceNode, IResourceSerializable
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
    public void Load(ResourceLoadContext context)
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
}