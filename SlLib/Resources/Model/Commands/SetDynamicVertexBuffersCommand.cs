using System.Runtime.Serialization;
using SlLib.Serialization;

namespace SlLib.Resources.Model.Commands;

public class SetDynamicVertexBuffersCommand : IRenderCommand
{
    public List<(short, short)> Buffers = [];
    public int WorkPass;
    public int Type => 0x0d;
    public int Size => 0x10;

    /// <inheritdoc />
    public void Load(ResourceLoadContext context, int commandBufferOffset, int offset)
    {
        int count = context.ReadInt16(offset + 4);
        if (count != context.ReadInt16(offset + 6))
            throw new SerializationException("Buffer counts don't match!");

        int streamData = commandBufferOffset + context.ReadInt32(offset + 8);
        for (int i = 0; i < count; ++i)
        {
            int address = streamData + i * 4;
            Buffers.Add((context.ReadInt16(address), context.ReadInt16(address + 2)));
        }

        WorkPass = context.ReadInt32(offset + 12);
    }

    /// <inheritdoc />
    public void Save(ResourceSaveContext context, ISaveBuffer commandDataBuffer, ISaveBuffer commandBuffer,
        ISaveBuffer? extraBuffer)
    {
        // Due to how I handle serialization, extraBuffer should never be null here.
        ArgumentNullException.ThrowIfNull(extraBuffer);

        context.WriteInt16(commandBuffer, (short)Buffers.Count, 4);
        context.WriteInt16(commandBuffer, (short)Buffers.Count, 6);
        context.WriteInt32(commandBuffer, extraBuffer.Address - commandDataBuffer.Address, 8);
        for (int i = 0; i < Buffers.Count; ++i)
        {
            (short, short) buffer = Buffers[i];
            context.WriteInt16(extraBuffer, buffer.Item1, i * 4);
            context.WriteInt16(extraBuffer, buffer.Item2, i * 4 + 2);
        }

        context.WriteInt32(commandBuffer, WorkPass, 12);
    }
}