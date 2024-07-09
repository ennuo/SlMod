using System.Numerics;
using SlLib.Resources.Database;
using SlLib.Serialization;

namespace SlLib.Resources.Scene.Instances;

public class SeInstanceCameraNode : SeInstanceTransformNode
{
    public Matrix4x4 View;
    public Matrix4x4 Projection;
    public Matrix4x4 ViewProjection;
    public float VerticalFov;
    public float AspectRatio = 16.0f / 9.0f;
    public float NearPlane = 0.1f;
    public float FarPlane = 20000.0f;
    public Vector2 OrthographicScale = Vector2.One;
    public int CameraFlags = 1;

    /// <inheritdoc />
    public override void Load(ResourceLoadContext context)
    {
        base.Load(context);

        View = context.ReadMatrix(0x160);
        Projection = context.ReadMatrix(0x1a0);
        ViewProjection = context.ReadMatrix(0x1e0);
        VerticalFov = context.ReadFloat(0x220);
        AspectRatio = context.ReadFloat(0x224);
        NearPlane = context.ReadFloat(0x228);
        FarPlane = context.ReadFloat(0x22c);
        OrthographicScale = context.ReadFloat2(0x230);
        CameraFlags = context.ReadBitset32(0x238);
    }

    /// <inheritdoc />
    public override void Save(ResourceSaveContext context, ISaveBuffer buffer)
    {
        base.Save(context, buffer);

        context.WriteMatrix(buffer, View, 0x160);
        context.WriteMatrix(buffer, Projection, 0x1a0);
        context.WriteMatrix(buffer, ViewProjection, 0x1e0);
        context.WriteFloat(buffer, VerticalFov, 0x220);
        context.WriteFloat(buffer, AspectRatio, 0x224);
        context.WriteFloat(buffer, NearPlane, 0x228);
        context.WriteFloat(buffer, FarPlane, 0x22c);
        context.WriteFloat2(buffer, OrthographicScale, 0x230);
        context.WriteInt32(buffer, CameraFlags, 0x238);
    }

    /// <inheritdoc />
    public override int GetSizeForSerialization(SlPlatform platform, int version) => 0x2a0;
}
