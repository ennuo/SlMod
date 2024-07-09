using System.Numerics;
using SlLib.Resources.Database;
using SlLib.Serialization;

namespace SlLib.Resources.Scene.Instances;

public class SeInstanceLightNode : SeInstanceTransformNode, IResourceSerializable
{
    public enum SeLightType : int
    {
        Ambient,
        Directional,
        Point,
        Spot
    }
    
    public int LightDataFlags;
    public SeLightType LightType;
    
    public float SpecularMultiplier;
    public float IntensityMultiplier;
    public Vector3 Color;

    // Positional Lights
    public float InnerRadius;
    public float OuterRadius;
    public float Falloff;
    
    // Spot Lights
    public float InnerConeAngle;
    public float OuterConeAngle;
    
    /// <inheritdoc />
    public override void Load(ResourceLoadContext context)
    {
        context.Position = LoadInternal(context, context.Position);
        LightDataFlags = context.ReadInt32(0x160);
        IntensityMultiplier = context.ReadFloat(0x164);
        InnerRadius = context.ReadFloat(0x168);
        OuterRadius = context.ReadFloat(0x16c);
        Color = context.ReadFloat3(0x170);
        SpecularMultiplier = context.ReadFloat(0x17c);
        InnerConeAngle = context.ReadFloat(0x180);
        OuterConeAngle = context.ReadFloat(0x184);
        Falloff = context.ReadFloat(0x18c);

        LightType = (SeLightType)(LightDataFlags & 7);



    }
}