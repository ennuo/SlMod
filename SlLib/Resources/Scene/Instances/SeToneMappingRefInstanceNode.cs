using System.Numerics;
using SlLib.Resources.Database;
using SlLib.Resources.Scene.Definitions;
using SlLib.Serialization;

namespace SlLib.Resources.Scene.Instances;

public class SeToneMappingRefInstanceNode : SeInstanceNode
{
    public float BloomScale;
    public float BloomOffset;
    public float BloomThreshold;
    public float AdaptSpeed;
    public int MotionBlurEnabled;
    public int MotionBlurGameUpdate;
    public float ShoulderStrength;
    public float LinearStrength;
    public float LinearAngle;
    public float ToeStrength;
    public float ToeNumerator;
    public float ToeDenominator;
    public float LinearWhitePointValue;
    public float ExposureMul;
    public float MinimumExposure;
    public float MaximumExposure;
    public float BloomShowOnly;
    public float ExposureKeyValue;
    public float VITAToneMappingValue;
    public float ColourBalance;
    public float ColourGrading;
    public SeDefinitionTextureNode ColourGradingTexture;
    public float MotionBlurBlend;
    public float MotionBlurRange;
    public float StartingBlendDistance;
    public float Radius1;
    public float FalloffMul1;
    public float Radius2;
    public float FalloffMul2;
    public float Range;
    public Vector4 TintColour;
    public float Enabled;
    public float BlurEnabled;
    public float ShowOnly;
    public float PostBlurGaussEnabled;

    /// <inheritdoc />
    public override void Load(ResourceLoadContext context)
    {
        context.Position = LoadInternal(context, context.Position);

        BloomScale = context.ReadFloat(0x84);
        BloomOffset = context.ReadFloat(0x88);
        BloomThreshold = context.ReadFloat(0x8c);
        AdaptSpeed = context.ReadFloat(0xa4);
        MotionBlurEnabled = context.ReadInt32(0xa8);
        MotionBlurGameUpdate = context.ReadInt32(0xac);
        ShoulderStrength = context.ReadFloat(0xb0);
        LinearStrength = context.ReadFloat(0xb4);
        LinearAngle = context.ReadFloat(0xb8);
        ToeStrength = context.ReadFloat(0xbc);
        ToeNumerator = context.ReadFloat(0xc0);
        ToeDenominator = context.ReadFloat(0xc4);
        LinearWhitePointValue = context.ReadFloat(0xc8);
        ExposureMul = context.ReadFloat(0xcc);
        MinimumExposure = context.ReadFloat(0xd0);
        MaximumExposure = context.ReadFloat(0xd4);
        BloomShowOnly = context.ReadFloat(0xd8);
        ExposureKeyValue = context.ReadFloat(0xe4);
        VITAToneMappingValue = context.ReadFloat(0xe8);
        ColourBalance = context.ReadFloat(0xfc);
        ColourGrading = context.ReadFloat(0x11c);
        //ColourGradingTexture = context.Read(0x120);
        MotionBlurBlend = context.ReadFloat(0x138);
        MotionBlurRange = context.ReadFloat(0x13c);
        StartingBlendDistance = context.ReadFloat(0x140);
        Radius1 = context.ReadFloat(0x158);
        FalloffMul1 = context.ReadFloat(0x15c);
        Radius2 = context.ReadFloat(0x160);
        FalloffMul2 = context.ReadFloat(0x164);
        Range = context.ReadFloat(0x178);
        TintColour = context.ReadFloat4(0x188);
        Enabled = context.ReadFloat(0x198);
        BlurEnabled = context.ReadFloat(0x19c);
        ShowOnly = context.ReadFloat(0x1a0);
        PostBlurGaussEnabled = context.ReadFloat(0x1a4);
    }

    /// <inheritdoc />
    public override void Save(ResourceSaveContext context, ISaveBuffer buffer)
    {
        base.Save(context, buffer);

        context.WriteFloat(buffer, BloomScale, 0x84);
        context.WriteFloat(buffer, BloomOffset, 0x88);
        context.WriteFloat(buffer, BloomThreshold, 0x8c);
        context.WriteFloat(buffer, AdaptSpeed, 0xa4);
        context.WriteInt32(buffer, MotionBlurEnabled, 0xa8);
        context.WriteInt32(buffer, MotionBlurGameUpdate, 0xac);
        context.WriteFloat(buffer, ShoulderStrength, 0xb0);
        context.WriteFloat(buffer, LinearStrength, 0xb4);
        context.WriteFloat(buffer, LinearAngle, 0xb8);
        context.WriteFloat(buffer, ToeStrength, 0xbc);
        context.WriteFloat(buffer, ToeNumerator, 0xc0);
        context.WriteFloat(buffer, ToeDenominator, 0xc4);
        context.WriteFloat(buffer, LinearWhitePointValue, 0xc8);
        context.WriteFloat(buffer, ExposureMul, 0xcc);
        context.WriteFloat(buffer, MinimumExposure, 0xd0);
        context.WriteFloat(buffer, MaximumExposure, 0xd4);
        context.WriteFloat(buffer, BloomShowOnly, 0xd8);
        context.WriteFloat(buffer, ExposureKeyValue, 0xe4);
        context.WriteFloat(buffer, VITAToneMappingValue, 0xe8);
        context.WriteFloat(buffer, ColourBalance, 0xfc);
        context.WriteFloat(buffer, ColourGrading, 0x11c);
        //context.Write(buffer, ColourGradingTexture, 0x120);
        context.WriteFloat(buffer, MotionBlurBlend, 0x138);
        context.WriteFloat(buffer, MotionBlurRange, 0x13c);
        context.WriteFloat(buffer, StartingBlendDistance, 0x140);
        context.WriteFloat(buffer, Radius1, 0x158);
        context.WriteFloat(buffer, FalloffMul1, 0x15c);
        context.WriteFloat(buffer, Radius2, 0x160);
        context.WriteFloat(buffer, FalloffMul2, 0x164);
        context.WriteFloat(buffer, Range, 0x178);
        context.WriteFloat4(buffer, TintColour, 0x188);
        context.WriteFloat(buffer, Enabled, 0x198);
        context.WriteFloat(buffer, BlurEnabled, 0x19c);
        context.WriteFloat(buffer, ShowOnly, 0x1a0);
        context.WriteFloat(buffer, PostBlurGaussEnabled, 0x1a4);
    }

    /// <inheritdoc />
    public override int GetSizeForSerialization(SlPlatform platform, int version) => 0x1c8;
}