using System.Runtime.Serialization;
using SlLib.Serialization;

namespace SlLib.Resources.Model.Commands;

public class SetDynamicVertexBuffers2Command : IRenderCommand
{
    public int Type => 0x11;
    public int Size => 0x14;
    
    public short SegmentIndex;
    public List<(short, short)> Buffers = [];
    public int WorkPass;
    
    /// <inheritdoc />
    public void Load(ResourceLoadContext context, int commandBufferOffset, int offset)
    {
        SegmentIndex = context.ReadInt16(offset + 4);
        
        // Doing a bunch of error checking here because I'm not entirely
        // sure what some value sare.
        
        if (context.ReadInt8(offset + 6) != 0x2b)
            throw new SerializationException("Unexpected value in dynamic vertex buffer command!");
        
        int count = context.ReadInt8(offset + 7);
        if (count != context.ReadInt8(offset + 8))
            throw new SerializationException("Buffer counts don't match!");
        
        if (context.ReadInt8(offset + 9) != 0x20)
            throw new SerializationException("Unexpected buffer stride in dynamic vertex buffer command!");
        
        if (context.ReadInt16(offset + 10) != (count * 0x2))
            throw new SerializationException("Dynamic vertex control element count doesn't match buffer count!");
        
        int streamData = commandBufferOffset + context.ReadInt32(offset + 12);
        for (int i = 0; i < count; ++i)
        {
            int address = streamData + i * 4;
            Buffers.Add((context.ReadInt16(address), context.ReadInt16(address + 2)));
        }

        WorkPass = context.ReadInt32(offset + 16);
    }

    /// <inheritdoc />
    public void Save(ResourceSaveContext context, ISaveBuffer commandDataBuffer, ISaveBuffer commandBuffer,
        ISaveBuffer? extraBuffer)
    {
        // I'm not actually supporting re-serializing TSR data right now,
        // do this later.
        throw new NotImplementedException();
    }
}