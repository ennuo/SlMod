using System.Numerics;
using SlLib.Resources.Database;
using SlLib.Serialization;

namespace SlLib.Resources.Scene.Instances;

public class SeInstanceLightNode : SeInstanceTransformNode
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
        base.Load(context);
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
    
    /// <inheritdoc />
    public override void Save(ResourceSaveContext context, ISaveBuffer buffer)
    {
        base.Save(context, buffer);
        
        context.WriteInt32(buffer, LightDataFlags, 0x160);
        context.WriteFloat(buffer, IntensityMultiplier, 0x164);
        context.WriteFloat(buffer, InnerRadius, 0x168);
        context.WriteFloat(buffer, OuterRadius, 0x16c);
        context.WriteFloat3(buffer, Color, 0x170);
        context.WriteFloat(buffer, SpecularMultiplier, 0x17c);
        context.WriteFloat(buffer, InnerConeAngle, 0x180);
        context.WriteFloat(buffer, OuterConeAngle, 0x184);
        context.WriteFloat(buffer, Falloff, 0x18c);
    }
    
    /// <inheritdoc />
    public override int GetSizeForSerialization(SlPlatform platform, int version) => 0x1a0;
}