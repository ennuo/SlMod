using SlLib.Resources.Buffer;
using SlLib.Resources.Database;
using SlLib.Serialization;

namespace SlLib.Resources;

/// <summary>
///     Resource that describes the layout of constant buffers.
/// </summary>
public class SlConstantBufferDesc : ISumoResource
{
    /// <inheritdoc />
    public SlResourceHeader Header { get; set; } = new();

    /// <summary>
    ///     Constant buffer descriptors.
    ///     <remarks>
    ///         Each chunk gets referenced specifically by a constant buffer in a material.
    ///     </remarks>
    /// </summary>
    public readonly List<SlConstantBufferDescChunk> Chunks = [];

    /// <inheritdoc />
    public void Load(ResourceLoadContext context)
    {
        int chunkStride = context.Platform.Is64Bit ? 0x38 : 0x20;
        int chunkStart = context.Position + 0x20;
        
        Header = context.LoadObject<SlResourceHeader>();
        int numChunks = (context.ReadInt32() >> 8) & 0xff;
        
        // The data keeps a reference to the start of the member data,
        // but we don't need it for serialization purposes.
        // int memberData = context.ReadInt32(offset + 0x10);
        
        for (int i = 0; i < numChunks; ++i)
        {
            context.Position = chunkStart + (chunkStride * i);
            
            int memberData = context.ReadPointer();
            int stringData = context.ReadPointer();
            int typeData = context.ReadPointer();
            
            // The hash data is just a cache of the string hashes of the
            // names of each members, we don't really need to keep track of them,
            // since we can just calculate them later.
            int hashData = context.ReadPointer();

            int memberCount = context.ReadInt16();
            int bufferSize = context.ReadInt16();
            int nameOffset = context.ReadInt32();
            int nameHash = context.ReadInt32();
            context.ReadInt16();
            context.ReadInt16();
            
            var chunk = new SlConstantBufferDescChunk
            {
                Name = context.ReadString(stringData + nameOffset),
                Size = bufferSize
            };

            for (int j = 0; j < memberCount; ++j)
            {
                int address = memberData + j * 0x20;
                nameOffset = context.ReadInt32(address + 4);
                chunk.Members.Add(new SlConstantBufferMember
                {
                    Name = context.ReadString(stringData + nameOffset),
                    Offset = context.ReadInt32(address + 12),
                    Size = context.ReadInt16(address + 16),
                    Components = context.ReadInt8(address + 18),
                    MaxComponents = context.ReadInt16(address + 20),
                    ArrayDataStride = context.ReadInt8(address + 26),
                    ArrayElementStride = context.ReadInt8(address + 27),
                    Rows = context.ReadInt8(address + 28),
                    Columns = context.ReadInt8(address + 29),
                    Dimensions = context.ReadInt8(address + 31)
                });
            }

            Chunks.Add(chunk);
        }
    }
}