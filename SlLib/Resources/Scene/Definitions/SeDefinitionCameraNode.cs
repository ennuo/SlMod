using System.Numerics;
using SlLib.Resources.Database;
using SlLib.Serialization;

namespace SlLib.Resources.Scene.Definitions;

// SE_CAMERA_
public class SeDefinitionCameraNode : SeDefinitionTransformNode
{
    public float VerticalFov = 60.0f;
    public float Aspect = 16.0f / 9.0f;
    public float NearPlane = 0.1f;
    public float FarPlane = 20000.0f;
    public Vector2 OrthographicScale = Vector2.One;
    
    // Camera_Type; 0 = Orthographic, 1 = Perspective
    // LookAt = 8
    public int CameraNodeFlags = 1;

    /// <inheritdoc />
    public override void Load(ResourceLoadContext context)
    {
        context.Position = LoadInternal(context, context.Position);
        VerticalFov = context.ReadFloat();
        Aspect = context.ReadFloat();
        NearPlane = context.ReadFloat();
        FarPlane = context.ReadFloat();
        OrthographicScale = context.ReadFloat2();
        CameraNodeFlags = context.ReadInt32();
        // + 0x1c = 0xbadf00d
    }
    
    /// <inheritdoc />
    public override void Save(ResourceSaveContext context, ISaveBuffer buffer)
    {
        base.Save(context, buffer);

        context.WriteFloat(buffer, VerticalFov, 0xd0);
        context.WriteFloat(buffer, Aspect, 0xd4);
        context.WriteFloat(buffer, NearPlane, 0xd8);
        context.WriteFloat(buffer, FarPlane, 0xdc);
        context.WriteFloat2(buffer, OrthographicScale, 0xe0);
        context.WriteInt32(buffer, CameraNodeFlags, 0xe8);
    }

    /// <inheritdoc />
    public override int GetSizeForSerialization(SlPlatform platform, int version) => 0xf0;
}