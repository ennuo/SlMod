using System.Numerics;
using System.Runtime.InteropServices;

namespace SeEditor.Renderer.Buffers;

[StructLayout(LayoutKind.Explicit, Size = 0x280)]
public struct ConstantBufferLightData
{
    [FieldOffset(0x00)] public Vector4 FogParams;
    [FieldOffset(0x10)] public Vector3 FogColor;
    [FieldOffset(0x20)] public Vector3 LightAmbient;
}