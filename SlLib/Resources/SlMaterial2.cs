using SlLib.Resources.Database;
using SlLib.Serialization;

namespace SlLib.Resources;

public class SlMaterial2 : ISumoResource
{
    public readonly List<SlConstantBuffer> ConstantBuffers = [];
    public readonly SlConstantBuffer?[] IndexToConstantBuffer = new SlConstantBuffer[8];
    public readonly SlSampler?[] IndexToSampler = new SlSampler[8];

    public readonly List<SlSampler> Samplers = [];

    public int Shader;

    /// <inheritdoc />
    public SlResourceHeader Header { get; set; } = new();

    /// <inheritdoc />
    public void Load(ResourceLoadContext context, int offset)
    {
        Header = context.LoadObject<SlResourceHeader>(offset);

        Shader = context.ReadInt32(offset + 0xc);

        // 0x14 is some count, used in 0x74...
        // 0x18 is pointer to some buffer data, seems based on previous count and data at 0x74?

        int samplerCount = context.ReadInt32(offset + 0x20);
        int samplerData = context.ReadInt32(offset + 0x24);
        for (int i = 0; i < samplerCount; ++i)
        {
            int address = samplerData + i * 0x28;
            Samplers.Add(context.LoadReference<SlSampler>(address));
        }

        for (int i = 0; i < 8; ++i)
            IndexToSampler[i] = context.LoadPointer<SlSampler>(0x2c + i * 4);

        int constantBufferCount = context.ReadInt32(offset + 0x4c);
        int constantBufferData = context.ReadInt32(offset + 0x50);
        for (int i = 0; i < constantBufferCount; ++i)
        {
            int address = constantBufferData + i * 0x30;
            ConstantBuffers.Add(context.LoadReference<SlConstantBuffer>(address));
        }

        for (int i = 0; i < 8; ++i)
            IndexToConstantBuffer[i] = context.LoadPointer<SlConstantBuffer>(0x54 + i * 4);

        // 0x74 is int[based on count @ 0x14]
    }
}