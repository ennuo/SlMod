using System.Runtime.Serialization;
using SlLib.Serialization;

namespace SlLib.Resources.Model.Commands;

public class SetDynamicVertexBuffersCommand : IRenderCommand
{
    public List<(int, int)> Buffers = [];
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
}