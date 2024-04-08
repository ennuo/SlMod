using System.Runtime.Serialization;
using System.Text.Json.Serialization;
using SlLib.Resources.Buffer;
using SlLib.Resources.Database;
using SlLib.Serialization;

namespace SlLib.Resources;

/// <summary>
///     Resource that contains a constant buffer for a vertex/fragment program.
/// </summary>
public class SlConstantBuffer : ISumoResource
{
    /// <summary>
    ///     The index of this constant buffer.
    /// </summary>
    public int Index;

    /// <summary>
    ///     The size of the constant buffer.
    /// </summary>
    public int Size;

    public int Unknown_0x28;

    /// <inheritdoc />
    public SlResourceHeader Header { get; set; } = new();

    /// <inheritdoc />
    public void Load(ResourceLoadContext context, int offset)
    {
        Header = context.LoadObject<SlResourceHeader>(offset);

        ConstantBufferDesc = context.LoadResourcePointer<SlConstantBufferDesc>(offset + 0xc);

        // The constant buffer if given a proper load context, should never be null.
        if (ConstantBufferDesc.Instance == null)
            throw new SerializationException($"Constant buffer description for {Header.Name} was null!");

        // This should technically be remapped into the above resource using the relocations chunk,
        // but you can't exactly do that here, but since we know the constant buffer format,
        // we can just calculate the index of the chunk.
        int descriptorChunkIndex = (context.ReadInt32(offset + 0x10) - 0x20) / 0x20;
        Chunk = ConstantBufferDesc.Instance.Chunks[descriptorChunkIndex];

        Index = context.ReadInt32(offset + 0x14);
        Size = context.ReadInt32(offset + 0x18);
        Unknown_0x28 = context.ReadInt32(offset + 0x28);

        // 0x0->0xc SlResourceHeader
        // 0xc->0x14 SlResPtrPair<SlConstantBufferDesc, SlConstantBufferDescChunk>
        // 0x14 - int - BufferIndex
        // 0x18 - int - Constant Data Size
        // 0x20 - float* - Constant Buffer
        // 0x24 -> SlConstantBuffer* pointer back to this
        // 0x28 -> int
    }

    // Just because it's a headache to deal with nullability for these ones specifically,
    // since they're meant to be pairs loaded from a buffer.
#nullable disable
    /// <summary>
    ///     The constant buffer that the descriptor chunk comes from.
    /// </summary>
    [JsonIgnore] public SlResPtr<SlConstantBufferDesc> ConstantBufferDesc;

    /// <summary>
    ///     The constant buffer descriptor chunk that describes this buffer.
    /// </summary>
    public SlConstantBufferDescChunk Chunk;
}