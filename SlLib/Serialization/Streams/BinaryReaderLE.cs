using System.Buffers.Binary;

namespace SlLib.Serialization.Streams;

public class BinaryReaderLE(Stream input) : BinaryReader(input)
{
    public override short ReadInt16()
    {
        return BinaryPrimitives.ReadInt16LittleEndian(ReadBytes(2));
    }

    public override ushort ReadUInt16()
    {
        return BinaryPrimitives.ReadUInt16LittleEndian(ReadBytes(2));
    }

    public override int ReadInt32()
    {
        return BinaryPrimitives.ReadInt32LittleEndian(ReadBytes(4));
    }

    public override uint ReadUInt32()
    {
        return BinaryPrimitives.ReadUInt32LittleEndian(ReadBytes(4));
    }

    public override long ReadInt64()
    {
        return BinaryPrimitives.ReadInt64LittleEndian(ReadBytes(8));
    }

    public override ulong ReadUInt64()
    {
        return BinaryPrimitives.ReadUInt64LittleEndian(ReadBytes(8));
    }

    public override Half ReadHalf()
    {
        return BinaryPrimitives.ReadHalfLittleEndian(ReadBytes(2));
    }

    public override float ReadSingle()
    {
        return BinaryPrimitives.ReadSingleLittleEndian(ReadBytes(4));
    }

    public override double ReadDouble()
    {
        return BinaryPrimitives.ReadDoubleLittleEndian(ReadBytes(8));
    }
}