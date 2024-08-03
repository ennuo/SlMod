using System.Numerics;
using System.Text;

namespace SlLib.Extensions;

public static class ByteReaderExtensions
{
    public static bool ReadBoolean(this byte[] b, int offset)
    {
        return b[offset] != 0;
    }

    public static byte ReadUint8(this byte[] b, int offset)
    {
        return b[offset];
    }

    public static short ReadInt16(this byte[] b, int offset)
    {
        short value = 0;
        value |= (short)(b[offset + 0] << 0);
        value |= (short)(b[offset + 1] << 8);

        return value;
    }

    public static ushort ReadUint16(this byte[] b, int offset)
    {
        ushort value = 0;
        value |= (ushort)(b[offset + 0] << 0);
        value |= (ushort)(b[offset + 1] << 8);

        return value;
    }

    public static int ReadInt32(this byte[] b, int offset)
    {
        int value = 0;
        value |= b[offset + 0] << 0;
        value |= b[offset + 1] << 8;
        value |= b[offset + 2] << 16;
        value |= b[offset + 3] << 24;

        return value;
    }
    
    public static int ReadInt32BigEndian(this byte[] b, int offset)
    {
        int value = 0;
        value |= b[offset + 0] << 24;
        value |= b[offset + 1] << 16;
        value |= b[offset + 2] << 8;
        value |= b[offset + 3] << 0;
        
        return value;
    }

    public static float ReadFloat(this byte[] b, int offset)
    {
        int value = b.ReadInt32(offset);
        return BitConverter.Int32BitsToSingle(value);
    }

    public static Vector2 ReadFloat2(this byte[] b, int offset)
    {
        return new Vector2(b.ReadFloat(offset), b.ReadFloat(offset + 4));
    }

    public static Vector3 ReadFloat3(this byte[] b, int offset)
    {
        return new Vector3(b.ReadFloat(offset), b.ReadFloat(offset + 4), b.ReadFloat(offset + 8));
    }

    public static Vector4 ReadFloat4(this byte[] b, int offset)
    {
        return new Vector4(b.ReadFloat(offset), b.ReadFloat(offset + 4), b.ReadFloat(offset + 8),
            b.ReadFloat(offset + 12));
    }

    public static Matrix4x4 ReadMatrix(this byte[] b, int offset)
    {
        var matrix = new Matrix4x4();
        for (int i = 0; i < 4; ++i)
        for (int j = 0; j < 4; ++j)
            matrix[i, j] = b.ReadFloat(offset + (i * 16) + (j * 4));
        return matrix;
    }

    public static string ReadString(this byte[] b, int offset)
    {
        int terminator = offset;
        while (b[terminator] != 0) terminator++;

        if (offset == terminator) return string.Empty;

        return Encoding.ASCII.GetString(b, offset, terminator - offset);
    }

    public static bool ReadBoolean(this ArraySegment<byte> b, int offset)
    {
        return b[offset] != 0;
    }

    public static byte ReadInt8(this ArraySegment<byte> b, int offset)
    {
        return b[offset];
    }

    public static short ReadInt16(this ArraySegment<byte> b, int offset)
    {
        return b.Array!.ReadInt16(b.Offset + offset);
    }

    public static int ReadInt32(this ArraySegment<byte> b, int offset)
    {
        return b.Array!.ReadInt32(b.Offset + offset);
    }
    
    public static int ReadInt32BigEndian(this ArraySegment<byte> b, int offset)
    {
        return b.Array!.ReadInt32BigEndian(b.Offset + offset);
    }

    public static float ReadFloat(this ArraySegment<byte> b, int offset)
    {
        return b.Array!.ReadFloat(b.Offset + offset);
    }

    public static Vector2 ReadFloat2(this ArraySegment<byte> b, int offset)
    {
        return b.Array!.ReadFloat2(b.Offset + offset);
    }

    public static Vector3 ReadFloat3(this ArraySegment<byte> b, int offset)
    {
        return b.Array!.ReadFloat3(b.Offset + offset);
    }

    public static Vector4 ReadFloat4(this ArraySegment<byte> b, int offset)
    {
        return b.Array!.ReadFloat4(b.Offset + offset);
    }

    public static Matrix4x4 ReadMatrix(this ArraySegment<byte> b, int offset)
    {
        return b.Array!.ReadMatrix(b.Offset + offset);
    }

    public static string ReadString(this ArraySegment<byte> b, int offset)
    {
        return b.Array!.ReadString(b.Offset + offset);
    }

    public static string ReadTerminatedUnicodeString(this BinaryReader reader)
    {
        var result = new StringBuilder();
        while (true)
        {
            char c = (char)reader.ReadInt16();
            if (c == 0) break;
            result.Append(c);
        }
        
        return result.ToString();
    }
}