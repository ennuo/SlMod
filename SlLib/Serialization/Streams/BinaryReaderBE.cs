using System.Buffers.Binary;

namespace SlLib.Serialization.Streams;

public class BinaryReaderBE(Stream input) : BinaryReader(input)
{
    public override short ReadInt16()
    {
        return BinaryPrimitives.ReadInt16BigEndian(ReadBytes(2));
    }

    public override ushort ReadUInt16()
    {
        return BinaryPrimitives.ReadUInt16BigEndian(ReadBytes(2));
    }

    public override int ReadInt32()
    {
        return BinaryPrimitives.ReadInt32BigEndian(ReadBytes(4));
    }

    public override uint ReadUInt32()
    {
        return BinaryPrimitives.ReadUInt32BigEndian(ReadBytes(4));
    }

    public override long ReadInt64()
    {
        return BinaryPrimitives.ReadInt64BigEndian(ReadBytes(8));
    }

    public override ulong ReadUInt64()
    {
        return BinaryPrimitives.ReadUInt64BigEndian(ReadBytes(8));
    }

    public override Half ReadHalf()
    {
        return BinaryPrimitives.ReadHalfBigEndian(ReadBytes(2));
    }

    public override float ReadSingle()
    {
        return BinaryPrimitives.ReadSingleBigEndian(ReadBytes(4));
    }

    public override double ReadDouble()
    {
        return BinaryPrimitives.ReadDoubleBigEndian(ReadBytes(8));
    }
}