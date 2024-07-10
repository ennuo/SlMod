using System.Numerics;
using SlLib.Resources.Database;
using SlLib.Serialization;

namespace SlLib.Resources.Scene.Definitions;

public class WaterShader3DefinitionNode : ShaderDefinitionBaseNode
{
    public int CubeEnv;
    public int WhiteWater;
    public int Normal;
    public int FlowMap;
    public float FlowSpeed;
    public float UScaling;
    public float VScaling;
    public float NormalUVScale;
    public float WhiteWaterUVScale1;
    public float NormalZScale;
    public Vector4 ShallowColour;
    public Vector4 DeepColour;
    public Vector4 SparkleColour;
    public Vector4 Fresnel;
    public Vector4 Refraction;
    public Vector4 VelocityParams;
    public Vector4 WhiteWaterParams;
    public Vector4 SparkleParams;

    /// <inheritdoc />
    public override void Load(ResourceLoadContext context)
    {
        context.Position = LoadInternal(context, context.Position);

        CubeEnv = context.ReadInt32(0x80);
        WhiteWater = context.ReadInt32(0x84);
        Normal = context.ReadInt32(0x88);
        FlowMap = context.ReadInt32(0x8c);
        FlowSpeed = context.ReadFloat(0x90);
        UScaling = context.ReadFloat(0x94);
        VScaling = context.ReadFloat(0x98);
        NormalUVScale = context.ReadFloat(0x9c);
        WhiteWaterUVScale1 = context.ReadFloat(0xa0);
        NormalZScale = context.ReadFloat(0xa8);
        ShallowColour = context.ReadFloat4(0xc0);
        DeepColour = context.ReadFloat4(0xd0);
        SparkleColour = context.ReadFloat4(0xe0);
        Fresnel = context.ReadFloat4(0xf0);
        Refraction = context.ReadFloat4(0x100);
        VelocityParams = context.ReadFloat4(0x110);
        WhiteWaterParams = context.ReadFloat4(0x120);
        SparkleParams = context.ReadFloat4(0x130);
    }

    /// <inheritdoc />
    public override void Save(ResourceSaveContext context, ISaveBuffer buffer)
    {
        base.Save(context, buffer);

        context.WriteInt32(buffer, CubeEnv, 0x80);
        context.WriteInt32(buffer, WhiteWater, 0x84);
        context.WriteInt32(buffer, Normal, 0x88);
        context.WriteInt32(buffer, FlowMap, 0x8c);
        context.WriteFloat(buffer, FlowSpeed, 0x90);
        context.WriteFloat(buffer, UScaling, 0x94);
        context.WriteFloat(buffer, VScaling, 0x98);
        context.WriteFloat(buffer, NormalUVScale, 0x9c);
        context.WriteFloat(buffer, WhiteWaterUVScale1, 0xa0);
        context.WriteFloat(buffer, NormalZScale, 0xa8);
        context.WriteFloat4(buffer, ShallowColour, 0xc0);
        context.WriteFloat4(buffer, DeepColour, 0xd0);
        context.WriteFloat4(buffer, SparkleColour, 0xe0);
        context.WriteFloat4(buffer, Fresnel, 0xf0);
        context.WriteFloat4(buffer, Refraction, 0x100);
        context.WriteFloat4(buffer, VelocityParams, 0x110);
        context.WriteFloat4(buffer, WhiteWaterParams, 0x120);
        context.WriteFloat4(buffer, SparkleParams, 0x130);
    }

    /// <inheritdoc />
    public override int GetSizeForSerialization(SlPlatform platform, int version) => 0x1e0;
}
