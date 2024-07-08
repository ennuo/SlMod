using System.Numerics;
using System.Runtime.InteropServices;

namespace SeEditor.OpenGL.Buffers;

[StructLayout(LayoutKind.Explicit, Size = 0x30)]
public struct ConstantBufferCommonModifiers
{
    [FieldOffset(0x00)] public Vector4 ColorMul;
    [FieldOffset(0x10)] public Vector4 ColorAdd;
    [FieldOffset(0x20)] public float AlphaRef;
    [FieldOffset(0x30)] public float FogMul;
}