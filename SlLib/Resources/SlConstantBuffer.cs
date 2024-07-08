using System.Runtime.Serialization;
using SlLib.Resources.Buffer;
using SlLib.Resources.Database;
using SlLib.Serialization;

namespace SlLib.Resources;

/// <summary>
///     Resource that contains a constant buffer for a vertex/fragment program.
/// </summary>
public class SlConstantBuffer : ISumoResource
{
    /// <inheritdoc />
    public SlResourceHeader Header { get; set; } = new();

    /// <summary>
    ///     The index of this constant buffer.
    /// </summary>
    public int Index;

    /// <summary>
    ///     The size of the constant buffer.
    /// </summary>
    public int Size;

    /// <summary>
    ///     The constant buffer data.
    /// </summary>
    public ArraySegment<byte> Data = ArraySegment<byte>.Empty;

    /// <summary>
    ///     Constant buffer flags.
    /// </summary>
    public int Flags;

    public bool IsBigEndian;

    /// <inheritdoc />
    public void Load(ResourceLoadContext context)
    {
        IsBigEndian = context.Platform.IsBigEndian;
        
        Header = context.LoadObject<SlResourceHeader>();
        
        int cbDataPointer;
        bool constantBufferIsFromGpu;
        
        // I don't think constant buffer descriptions existed yet?
        // Or at the very least not in the same capacity
        if (context.Version <= 0x1b)
        {
            Flags = context.ReadInt32();
            context.ReadPointer();
            Index = context.ReadInt32();

            int size = context.ReadInt32();
            if (size != context.ReadInt32())
                throw new SerializationException("Unsupported size parameter in legacy constant buffer!");

            context.ReadPointer(); // ??? 0 generally it seems
            
            cbDataPointer = context.ReadPointer(out constantBufferIsFromGpu);
            if (cbDataPointer != 0 || constantBufferIsFromGpu)
                Data = context.LoadBuffer(cbDataPointer, Size, constantBufferIsFromGpu);
            context.ReadPointer(); // 0x24 - Platform pointer

            return;
        }
        
        ConstantBufferDesc = context.LoadResourcePointer<SlConstantBufferDesc>();
        
        // The constant buffer if given a proper load context, should never be null.
        if (ConstantBufferDesc.Instance == null)
            throw new SerializationException($"Constant buffer description for {Header.Name} was null!");

        // This should technically be remapped into the above resource using the relocations chunk,
        // but you can't exactly do that here, but since we know the constant buffer format,
        // we can just calculate the index of the chunk.
        int descriptorChunkStride = context.Platform.Is64Bit ? 0x38 : 0x20;
        int descriptorChunkIndex = (context.ReadPointer() - 0x20) / descriptorChunkStride;
        Chunk = ConstantBufferDesc.Instance.Chunks[descriptorChunkIndex];
        
        // Around Android's version, they moved the counts below the pointers.
        if (context.Version >= SlPlatform.Android.DefaultVersion)
        {
            cbDataPointer = context.ReadPointer(out constantBufferIsFromGpu); // 0x20 - Buffer data
            Index = context.ReadInt32();
            Size = context.ReadInt32();
            context.ReadPointer();
        }
        else
        {
            Index = context.ReadInt32();
            Size = context.ReadInt32();
            context.ReadPointer(); 
            cbDataPointer = context.ReadPointer(out constantBufferIsFromGpu); // 0x20 - Buffer data
        }
        
        context.ReadPointer(); // 0x24 - Platform pointer
        Flags = context.ReadInt32();
        
        if (cbDataPointer != 0 || constantBufferIsFromGpu)
        {
            Data = context.LoadBuffer(cbDataPointer, Size, constantBufferIsFromGpu);   
        }
    }

    public void Save(ResourceSaveContext context, ISaveBuffer buffer)
    {
        if (ConstantBufferDesc.Instance == null)
            throw new SerializationException("Can't serialize constant buffer with null descriptor!");

        int cbChunkDataIndex = ConstantBufferDesc.Instance.Chunks.IndexOf(Chunk);
        if (cbChunkDataIndex == -1)
            throw new SerializationException("Constant buffer chunk doesn't belong to assigned descriptor!");

        int cbChunkDataOffset = 0x20 + (cbChunkDataIndex * 0x20);
        context.SaveResourcePair(buffer, ConstantBufferDesc, cbChunkDataOffset, 0xc);
        context.WriteInt32(buffer, Index, 0x14);
        context.WriteInt32(buffer, Size, 0x18);
        context.SavePointer(buffer, this, 0x24);
        context.WriteInt32(buffer, Flags, 0x28);
    }

    public int GetSizeForSerialization(SlPlatform platform, int version)
    {
        if (platform.Is64Bit) return 0x50;
        if (version <= 0x1b) return 0x2c;
        return platform == SlPlatform.WiiU ? 0x2c : 0x30;
    }

    // Just because it's a headache to deal with nullability for these ones specifically,
    // since they're meant to be pairs loaded from a buffer.
#nullable disable
    /// <summary>
    ///     The constant buffer that the descriptor chunk comes from.
    /// </summary>
    public SlResPtr<SlConstantBufferDesc> ConstantBufferDesc;

    /// <summary>
    ///     The constant buffer descriptor chunk that describes this buffer.
    /// </summary>
    public SlConstantBufferDescChunk Chunk;
}