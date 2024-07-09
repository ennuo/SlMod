using System.Numerics;
using SlLib.Resources.Database;
using SlLib.Serialization;

namespace SlLib.Resources.Scene.Definitions;

public class WaterShader4DefinitionNode : ShaderDefinitionBaseNode
{
    public int CubeEnv;
    public int DepthFog;
    public int Normal;
    public int FlowMap;
    public float FlowSpeed;
    public float UScaling;
    public float VScaling;
    public float NormalUVScale;
    public float WhiteWaterUVScale;
    public float NormalZScale;
    public Vector4 WhiteWaterColour;
    public Vector4 SparkleColour;
    public Vector4 Fresnel;
    public Vector4 Refraction;
    public Vector4 VelocityParams;
    public Vector4 WhiteWaterParams;
    public Vector4 SparkleParams;

    /// <inheritdoc />
    public override void Load(ResourceLoadContext context)
    {
        base.Load(context);

        CubeEnv = context.ReadInt32(0x80);
        DepthFog = context.ReadInt32(0x84);
        Normal = context.ReadInt32(0x88);
        FlowMap = context.ReadInt32(0x8c);
        FlowSpeed = context.ReadFloat(0x90);
        UScaling = context.ReadFloat(0x94);
        VScaling = context.ReadFloat(0x98);
        NormalUVScale = context.ReadFloat(0x9c);
        WhiteWaterUVScale = context.ReadFloat(0xa0);
        NormalZScale = context.ReadFloat(0xa4);
        WhiteWaterColour = context.ReadFloat4(0xb0);
        SparkleColour = context.ReadFloat4(0xc0);
        Fresnel = context.ReadFloat4(0xd0);
        Refraction = context.ReadFloat4(0xe0);
        VelocityParams = context.ReadFloat4(0xf0);
        WhiteWaterParams = context.ReadFloat4(0x100);
        SparkleParams = context.ReadFloat4(0x110);
    }

    /// <inheritdoc />
    public override void Save(ResourceSaveContext context, ISaveBuffer buffer)
    {
        base.Save(context, buffer);

        context.WriteInt32(buffer, CubeEnv, 0x80);
        context.WriteInt32(buffer, DepthFog, 0x84);
        context.WriteInt32(buffer, Normal, 0x88);
        context.WriteInt32(buffer, FlowMap, 0x8c);
        context.WriteFloat(buffer, FlowSpeed, 0x90);
        context.WriteFloat(buffer, UScaling, 0x94);
        context.WriteFloat(buffer, VScaling, 0x98);
        context.WriteFloat(buffer, NormalUVScale, 0x9c);
        context.WriteFloat(buffer, WhiteWaterUVScale, 0xa0);
        context.WriteFloat(buffer, NormalZScale, 0xa4);
        context.WriteFloat4(buffer, WhiteWaterColour, 0xb0);
        context.WriteFloat4(buffer, SparkleColour, 0xc0);
        context.WriteFloat4(buffer, Fresnel, 0xd0);
        context.WriteFloat4(buffer, Refraction, 0xe0);
        context.WriteFloat4(buffer, VelocityParams, 0xf0);
        context.WriteFloat4(buffer, WhiteWaterParams, 0x100);
        context.WriteFloat4(buffer, SparkleParams, 0x110);
    }

    /// <inheritdoc />
    public override int GetSizeForSerialization(SlPlatform platform, int version) => 0x1c0;
}
