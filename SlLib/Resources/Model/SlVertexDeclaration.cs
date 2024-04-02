using System.Numerics;
using SlLib.Extensions;
using SlLib.Serialization;

namespace SlLib.Resources.Model;

public class SlVertexDeclaration : ILoadable
{
    private const int MaxStreams = 3;
    private const int MaxUsageIndices = 2;

    /// <summary>
    ///     The attributes used in this vertex declaration sorted by usage -> index
    /// </summary>
    private readonly SlVertexAttribute?[][] _attributes;

    /// <summary>
    ///     The size of each stream in this declaration.
    /// </summary>
    private readonly int[] _streamSizes = [0, 0, 0];

    /// <summary>
    ///     Constructs an empty vertex declaration.
    /// </summary>
    public SlVertexDeclaration()
    {
        // Initialize the attributes lookup
        // First layer is usage, second layer is index
        _attributes = new SlVertexAttribute[SlVertexUsage.Count][];
        for (int i = 0; i < SlVertexUsage.Count; ++i)
            _attributes[i] = new SlVertexAttribute[MaxUsageIndices];
    }

    /// <inheritdoc />
    public void Load(ResourceLoadContext context, int offset)
    {
        int numAttributes = context.ReadInt32(offset + 12);
        offset = context.ReadInt32(offset + 16);
        for (int i = 0; i < numAttributes; ++i, offset += 0x8)
        {
            int streamIndex = context.ReadInt16(offset);
            int streamOffset = context.ReadInt16(offset + 2);
            var type = (SlVertexElementType)context.ReadInt8(offset + 4);
            int count = context.ReadInt8(offset + 5);
            int usage = context.ReadInt8(offset + 6);
            int index = context.ReadInt8(offset + 7);

            AddAttribute(streamIndex, streamOffset, type, count, usage, index);
        }
    }

    /// <summary>
    ///     Fetches the elements from a given usage and index from the vertex streams.
    /// </summary>
    /// <param name="streams">The streams to read vertex data from</param>
    /// <param name="usage">The vertex usage to fetch</param>
    /// <param name="start">The index of the first vertex to fetch</param>
    /// <param name="count">The number of vertices to fetch</param>
    /// <param name="index">The usage index</param>
    /// <returns>Vertex attributes in Vector4 format</returns>
    /// <exception cref="NotSupportedException"></exception>
    public Vector4[]? Get(SlStream?[] streams, int usage, int start, int count, int index = 0)
    {
        SlVertexAttribute? attribute = _attributes[usage][index];
        if (attribute == null) return null;
        SlStream? stream = streams[attribute.Stream];
        if (stream == null) return null;

        byte[] data = stream.Data.Array!;
        int streamSize = _streamSizes[attribute.Stream];
        var elements = new Vector4[count];
        for (int i = 0; i < count; ++i)
        {
            int offset = stream.Data.Offset + (start + i) * streamSize + attribute.Offset;
            elements[i][3] = 1.0f; // Last component should be 1.0 by default.
            switch (attribute.Type)
            {
                case SlVertexElementType.UByte:
                    for (int j = 0; j < attribute.Count; ++j)
                        elements[i][j] = data[offset + j];
                    break;
                case SlVertexElementType.UByteN:
                    for (int j = 0; j < attribute.Count; ++j)
                        elements[i][j] = data[offset + j] / 255.0f;
                    break;
                case SlVertexElementType.Half:
                    for (int j = 0; j < attribute.Count; ++j)
                    {
                        short half = data.ReadInt16(offset + j * 2);
                        elements[i][j] = (float)BitConverter.Int16BitsToHalf(half);
                    }

                    break;
                case SlVertexElementType.Float:
                    for (int j = 0; j < attribute.Count; ++j)
                        elements[i][j] = data.ReadFloat(offset + j * 4);
                    break;
                default:
                    throw new NotSupportedException("Unsupported element type!");
            }
        }

        return elements;
    }

    /// <summary>
    ///     Adds an attribute to this vertex declaration.
    /// </summary>
    /// <param name="stream">The stream index</param>
    /// <param name="offset">The byte offset of the attribute</param>
    /// <param name="type">The type of element in the stream</param>
    /// <param name="count">The number of elements in the stream</param>
    /// <param name="usage">The usage of this attribute</param>
    /// <param name="index">The usage index</param>
    /// <exception cref="ArgumentException">Thrown if an invalid stream, usage, or element type is specified</exception>
    public void AddAttribute(int stream, int offset, SlVertexElementType type, int count, int usage, int index)
    {
        if (stream is < 0 or >= MaxStreams)
            throw new ArgumentException("Stream has invalid index!");
        if (usage is < 0 or >= SlVertexUsage.Count)
            throw new ArgumentException("Stream has unsupported usage!");

        int elementSize = type switch
        {
            SlVertexElementType.UByte => 1,
            SlVertexElementType.UByteN => 1,
            SlVertexElementType.Float => 4,
            SlVertexElementType.Half => 2,
            _ => throw new ArgumentException("Unsupported vertex element type!")
        };

        int size = elementSize * count;
        var attribute = new SlVertexAttribute
        {
            Stream = stream,
            Offset = offset,
            Index = index,
            Size = size,
            Type = type,
            Count = count,
            Usage = usage
        };

        _attributes[usage][index] = attribute;

        int totalSize = offset + size;
        if (totalSize > _streamSizes[stream])
            _streamSizes[stream] = totalSize;
    }
}