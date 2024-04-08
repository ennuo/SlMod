using SlLib.Resources.Buffer;
using SlLib.Resources.Database;
using SlLib.Serialization;

namespace SlLib.Resources;

/// <summary>
///     Resource that describes the layout of constant buffers.
/// </summary>
public class SlConstantBufferDesc : ISumoResource
{
    /// <summary>
    ///     Constant buffer descriptors.
    ///     <remarks>
    ///         Each chunk gets referenced specifically by a constant buffer in a material.
    ///     </remarks>
    /// </summary>
    public readonly List<SlConstantBufferDescChunk> Chunks = [];

    /// <inheritdoc />
    public SlResourceHeader Header { get; set; } = new();

    /// <inheritdoc />
    public void Load(ResourceLoadContext context, int offset)
    {
        Header = context.LoadObject<SlResourceHeader>(offset);

        // I have no idea what this field actually is.
        int numPrograms = context.ReadInt8(offset + 0xc);
        int numChunks = context.ReadInt8(offset + 0xd);

        // The data keeps a reference to the start of the member data,
        // but we don't need it for serialization purposes.
        // int memberData = context.ReadInt32(offset + 0x10);

        for (int i = 0; i < numChunks; ++i)
        {
            int address = offset + 0x20 + i * 0x20;

            int memberData = context.ReadInt32(address);
            int stringData = context.ReadInt32(address + 4);
            int typeData = context.ReadInt32(address + 8);

            // The hash data is just a cache of the string hashes of the
            // names of each members, we don't really need to keep track of them,
            // since we can just calculate them later.
            int hashData = context.ReadInt32(address + 12);

            int memberCount = context.ReadInt16(address + 16);
            int bufferSize = context.ReadInt16(address + 18);
            int nameOffset = context.ReadInt32(address + 20);
            int nameHash = context.ReadInt32(address + 24);

            var chunk = new SlConstantBufferDescChunk
            {
                Name = context.ReadString(stringData + nameOffset),
                Size = bufferSize
            };

            for (int j = 0; j < memberCount; ++j)
            {
                address = memberData + j * 0x20;
                nameOffset = context.ReadInt32(address + 4);
                chunk.Members.Add(new SlConstantBufferMember
                {
                    Name = context.ReadString(stringData + nameOffset),
                    Offset = context.ReadInt32(address + 12),
                    Size = context.ReadInt16(address + 16),
                    Components = context.ReadInt16(address + 18),
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