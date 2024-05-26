using System.Buffers.Binary;

namespace SlLib.Serialization.Streams;

public class BinaryWriterBE(Stream input) : BinaryWriter(input)
{
    // Writes a two-byte signed integer to this stream. The current position of
    // the stream is advanced by two.
    //
    public override void Write(short value)
    {
        Span<byte> buffer = stackalloc byte[sizeof(short)];
        BinaryPrimitives.WriteInt16BigEndian(buffer, value);
        OutStream.Write(buffer);
    }

    // Writes a two-byte unsigned integer to this stream. The current position
    // of the stream is advanced by two.
    //
    public override void Write(ushort value)
    {
        Span<byte> buffer = stackalloc byte[sizeof(ushort)];
        BinaryPrimitives.WriteUInt16BigEndian(buffer, value);
        OutStream.Write(buffer);
    }

    // Writes a four-byte signed integer to this stream. The current position
    // of the stream is advanced by four.
    //
    public override void Write(int value)
    {
        Span<byte> buffer = stackalloc byte[sizeof(int)];
        BinaryPrimitives.WriteInt32BigEndian(buffer, value);
        OutStream.Write(buffer);
    }

    // Writes a four-byte unsigned integer to this stream. The current position
    // of the stream is advanced by four.
    //
    public override void Write(uint value)
    {
        Span<byte> buffer = stackalloc byte[sizeof(uint)];
        BinaryPrimitives.WriteUInt32BigEndian(buffer, value);
        OutStream.Write(buffer);
    }

    // Writes an eight-byte signed integer to this stream. The current position
    // of the stream is advanced by eight.
    //
    public override void Write(long value)
    {
        Span<byte> buffer = stackalloc byte[sizeof(long)];
        BinaryPrimitives.WriteInt64BigEndian(buffer, value);
        OutStream.Write(buffer);
    }

    // Writes an eight-byte unsigned integer to this stream. The current
    // position of the stream is advanced by eight.
    //
    public override void Write(ulong value)
    {
        Span<byte> buffer = stackalloc byte[sizeof(ulong)];
        BinaryPrimitives.WriteUInt64BigEndian(buffer, value);
        OutStream.Write(buffer);
    }

    // Writes a float to this stream. The current position of the stream is
    // advanced by four.
    //
    public override void Write(float value)
    {
        Span<byte> buffer = stackalloc byte[sizeof(float)];
        BinaryPrimitives.WriteSingleBigEndian(buffer, value);
        OutStream.Write(buffer);
    }

    // Writes a half to this stream. The current position of the stream is
    // advanced by two.
    //
    public override void Write(Half value)
    {
        Span<byte> buffer = stackalloc byte[sizeof(ushort) /* = sizeof(Half) */];
        BinaryPrimitives.WriteHalfBigEndian(buffer, value);
        OutStream.Write(buffer);
    }
}