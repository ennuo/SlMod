using System.Numerics;
using System.Runtime.InteropServices;

namespace SeEditor.OpenGL.Buffers;

[StructLayout(LayoutKind.Explicit, Size = 0x40)]
public struct ConstantBufferWorldMatrix
{
    [FieldOffset(0x00)] public Matrix4x4 World;
}