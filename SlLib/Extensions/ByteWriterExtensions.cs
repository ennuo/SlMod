using System.Buffers.Binary;
using System.Numerics;
using System.Text;

namespace SlLib.Extensions;

public static class ByteWriterExtensions
{
    public static void WriteBoolean(this byte[] b, bool value, int offset)
    {
        b[offset] = (byte)(value ? 1 : 0);
    }

    public static void WriteUint8(this byte[] b, byte value, int offset)
    {
        b[offset] = value;
    }

    public static void WriteInt16(this byte[] b, short value, int offset)
    {
        var span = new Span<byte>(b, offset, sizeof(short));
        BinaryPrimitives.WriteInt16LittleEndian(span, value);
    }

    public static void WriteUint16(this byte[] b, ushort value, int offset)
    {
        var span = new Span<byte>(b, offset, sizeof(ushort));
        BinaryPrimitives.WriteUInt16LittleEndian(span, value);
    }

    public static void WriteInt32(this byte[] b, int value, int offset)
    {
        var span = new Span<byte>(b, offset, sizeof(int));
        BinaryPrimitives.WriteInt32LittleEndian(span, value);
    }

    public static void WriteFloat(this byte[] b, float value, int offset)
    {
        var span = new Span<byte>(b, offset, sizeof(float));
        BinaryPrimitives.WriteSingleLittleEndian(span, value);
    }

    public static void WriteFloat2(this byte[] b, Vector2 value, int offset)
    {
        b.WriteFloat(value.X, offset);
        b.WriteFloat(value.Y, offset + 4);
    }

    public static void WriteFloat3(this byte[] b, Vector3 value, int offset)
    {
        b.WriteFloat(value.X, offset);
        b.WriteFloat(value.Y, offset + 4);
        b.WriteFloat(value.Z, offset + 8);
    }

    public static void WriteFloat4(this byte[] b, Vector4 value, int offset)
    {
        b.WriteFloat(value.X, offset);
        b.WriteFloat(value.Y, offset + 4);
        b.WriteFloat(value.Z, offset + 8);
        b.WriteFloat(value.W, offset + 12);
    }

    public static void WriteMatrix(this byte[] b, Matrix4x4 value, int offset)
    {
        for (int i = 0; i < 4; ++i)
        for (int j = 0; j < 4; ++j)
            b.WriteFloat(value[i, j], offset + i * 16 + j * 4);
    }

    public static void WriteString(this byte[] b, string value, int offset)
    {
        if (string.IsNullOrEmpty(value))
        {
            b[offset] = 0;
            return;
        }

        int size = value.Length;
        Encoding.ASCII.GetBytes(value, 0, size, b, offset);
        b[offset + size] = 0;
    }

    public static void WriteBoolean(this ArraySegment<byte> b, bool value, int offset)
    {
        b[offset] = (byte)(value ? 1 : 0);
    }

    public static void WriteInt8(this ArraySegment<byte> b, byte value, int offset)
    {
        b[offset] = value;
    }

    public static void WriteInt16(this ArraySegment<byte> b, short value, int offset)
    {
        WriteInt16(b.Array!, value, b.Offset + offset);
    }

    public static void WriteInt32(this ArraySegment<byte> b, int value, int offset)
    {
        WriteInt32(b.Array!, value, b.Offset + offset);
    }

    public static void WriteFloat(this ArraySegment<byte> b, float value, int offset)
    {
        WriteFloat(b.Array!, value, b.Offset + offset);
    }

    public static void WriteFloat2(this ArraySegment<byte> b, Vector2 value, int offset)
    {
        WriteFloat2(b.Array!, value, b.Offset + offset);
    }

    public static void WriteFloat3(this ArraySegment<byte> b, Vector3 value, int offset)
    {
        WriteFloat3(b.Array!, value, b.Offset + offset);
    }

    public static void WriteFloat4(this ArraySegment<byte> b, Vector4 value, int offset)
    {
        WriteFloat4(b.Array!, value, b.Offset + offset);
    }

    public static void WriteMatrix(this ArraySegment<byte> b, Matrix4x4 value, int offset)
    {
        WriteMatrix(b.Array!, value, b.Offset + offset);
    }

    public static void WriteString(this ArraySegment<byte> b, string value, int offset)
    {
        WriteString(b.Array!, value, b.Offset + offset);
    }
}