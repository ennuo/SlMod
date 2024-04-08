using System.Numerics;
using SlLib.Serialization;

namespace SlLib.Resources.Scene.Definitions;

// SE_CAMERA_
public class SeDefinitionCameraNode : SeDefinitionTransformNode, ILoadable
{
    public override bool NodeNameIsFilename => false;

    public float VerticalFov = 60.0f;
    public float Aspect = 16.0f / 9.0f;
    public float NearPlane = 0.1f;
    public float FarPlane = 20000.0f;
    public Vector2 OrthographicScale = Vector2.One;

    // Seems like there's basically a single flag
    // Camera_Type; 0 = Orthographic, 1 = Perspective
    public int CameraNodeFlags = 1;

    /// <inheritdoc />
    public void Load(ResourceLoadContext context, int offset)
    {
        offset = LoadInternal(context, offset);
        VerticalFov = context.ReadFloat(offset + 0x00);
        Aspect = context.ReadFloat(offset + 0x04);
        NearPlane = context.ReadFloat(offset + 0x08);
        FarPlane = context.ReadFloat(offset + 0x0c);
        OrthographicScale = context.ReadFloat2(offset + 0x10);
        CameraNodeFlags = context.ReadInt32(offset + 0x18);
        // + 0x1c = 0xbadf00d
    }
}