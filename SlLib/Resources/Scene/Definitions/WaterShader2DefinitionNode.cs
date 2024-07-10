using System.Numerics;
using SlLib.Resources.Database;
using SlLib.Serialization;

namespace SlLib.Resources.Scene.Definitions;

public class WaterShader2DefinitionNode : ShaderDefinitionBaseNode
{
    public Vector4 ShallowColour;
    public Vector4 DeepColour;
    public Vector4 FoamColour;
    public Vector4 Fresnel;
    public Vector4 Refraction;
    public Vector4 NormalUVScale;
    public Vector4 NormalUVSpeed;
    public Vector4 FoamUVScale;
    public Vector4 FoamUVSpeed;
    public Vector4 NormalZScale;
    public int FoamMap;
    public int CubeEnv;
    public int Normal;
    public int Foam;

    /// <inheritdoc />
    public override void Load(ResourceLoadContext context)
    {
        context.Position = LoadInternal(context, context.Position);

        ShallowColour = context.ReadFloat4(0x80);
        DeepColour = context.ReadFloat4(0x90);
        FoamColour = context.ReadFloat4(0xa0);
        Fresnel = context.ReadFloat4(0xb0);
        Refraction = context.ReadFloat4(0xc0);
        NormalUVScale = context.ReadFloat4(0xd0);
        NormalUVSpeed = context.ReadFloat4(0xe0);
        FoamUVScale = context.ReadFloat4(0xf0);
        FoamUVSpeed = context.ReadFloat4(0x100);
        NormalZScale = context.ReadFloat4(0x110);
        FoamMap = context.ReadInt32(0x120);
        CubeEnv = context.ReadInt32(0x124);
        Normal = context.ReadInt32(0x128);
        Foam = context.ReadInt32(0x12c);
    }

    /// <inheritdoc />
    public override void Save(ResourceSaveContext context, ISaveBuffer buffer)
    {
        base.Save(context, buffer);

        context.WriteFloat4(buffer, ShallowColour, 0x80);
        context.WriteFloat4(buffer, DeepColour, 0x90);
        context.WriteFloat4(buffer, FoamColour, 0xa0);
        context.WriteFloat4(buffer, Fresnel, 0xb0);
        context.WriteFloat4(buffer, Refraction, 0xc0);
        context.WriteFloat4(buffer, NormalUVScale, 0xd0);
        context.WriteFloat4(buffer, NormalUVSpeed, 0xe0);
        context.WriteFloat4(buffer, FoamUVScale, 0xf0);
        context.WriteFloat4(buffer, FoamUVSpeed, 0x100);
        context.WriteFloat4(buffer, NormalZScale, 0x110);
        context.WriteInt32(buffer, FoamMap, 0x120);
        context.WriteInt32(buffer, CubeEnv, 0x124);
        context.WriteInt32(buffer, Normal, 0x128);
        context.WriteInt32(buffer, Foam, 0x12c);
    }

    /// <inheritdoc />
    public override int GetSizeForSerialization(SlPlatform platform, int version) => 0x1e0;
}
