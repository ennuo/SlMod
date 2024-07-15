using System.Numerics;
using System.Runtime.InteropServices;

namespace SeEditor.Renderer.Buffers;

[StructLayout(LayoutKind.Explicit, Size = 0x100)]
public struct ConstantBufferViewProjection
{
    [FieldOffset(0x00)] public Matrix4x4 View;
    [FieldOffset(0x40)] public Matrix4x4 Projection;
    [FieldOffset(0x80)] public Matrix4x4 ViewProjection;
    [FieldOffset(0xc0)] public Matrix4x4 ViewInverse;
}