using System.Buffers.Binary;
using System.Numerics;
using System.Runtime.InteropServices;
using SlLib.Extensions;
using SlLib.Resources.Database;
using SlLib.Serialization;

namespace SlLib.Resources.Model;

public class SlVertexDeclaration : IResourceSerializable
{
    private const int MaxStreams = 5;
    private const int MaxUsageIndices = 3;

    /// <summary>
    ///     The attributes used in this vertex declaration sorted by usage -> index
    /// </summary>
    private readonly SlVertexAttribute?[][] _attributes;

    /// <summary>
    ///     The size of each stream in this declaration.
    /// </summary>
    private readonly int[] _streamSizes = new int[MaxStreams];

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

    public SlStream?[] Create(int vertexCount)
    {
        var streams = new SlStream?[3];
        for (int i = 0; i < 3; ++i)
        {
            int size = _streamSizes[i];
            if (size == 0)
            {
                streams[i] = null;
                continue;
            }
            
            streams[i] = new SlStream(vertexCount, size);
        }

        return streams;
    }

    /// <inheritdoc />
    public void Load(ResourceLoadContext context)
    {
        context.Position += (8 + context.Platform.GetPointerSize());
        int numAttributes, attributeData;
        if (context.Version >= SlPlatform.Android.DefaultVersion)
        {
            attributeData = context.ReadPointer();
            numAttributes = context.ReadInt32();
        }
        else
        {
            numAttributes = context.ReadInt32();
            attributeData = context.ReadPointer();
        }

        context.Position = attributeData;
        
        
        for (int i = 0; i < numAttributes; ++i)
        {
            int streamIndex;
            if (context.Version <= 0xb) streamIndex = context.ReadInt16();
            else
            {
                streamIndex = context.ReadInt8();
                int _ = context.ReadInt8(); // Unsure what this is, only have seen it used in the Wii U version.   
            }
            
            int streamOffset = context.ReadInt16();
            var type = (SlVertexElementType)context.ReadInt8();
            int count = context.ReadInt8();
            int usage = context.ReadInt8();
            int index = context.ReadInt8();

            AddAttribute(streamIndex, streamOffset, type, count, usage, index);
        }
    }

    /// <inheritdoc />
    public void Save(ResourceSaveContext context, ISaveBuffer buffer)
    {
        var attributes = GetFlattenedAttributes();

        context.WriteInt32(buffer, 0x1, 0x8); // Always 1?
        context.WriteInt32(buffer, attributes.Count, 0xc);

        ISaveBuffer attributeData = context.SaveGenericPointer(buffer, 0x10, attributes.Count * 0x8);
        for (int i = 0; i < attributes.Count; ++i)
        {
            ISaveBuffer crumb = attributeData.At(i * 8, 8);
            SlVertexAttribute attribute = attributes[i];

            context.WriteInt8(crumb, (byte)attribute.Stream, 0x0);
            context.WriteInt16(crumb, (short)attribute.Offset, 0x2);
            context.WriteInt8(crumb, (byte)attribute.Type, 0x4);
            context.WriteInt8(crumb, (byte)attribute.Count, 0x5);
            context.WriteInt8(crumb, (byte)attribute.Usage, 0x6);
            context.WriteInt8(crumb, (byte)attribute.Index, 0x7);
        }

        // Structure references itself
        context.SavePointer(buffer, this, 0x14);
    }

    /// <inheritdoc />
    public int GetSizeForSerialization(SlPlatform platform, int version)
    {
        if (platform == SlPlatform.WiiU) return 0x4c;
        if (platform == SlPlatform.Android) return 0x24;
        return platform.Is64Bit ? 0x38 : 0x1c;
    }

    public void Set(SlStream?[] streams, int usage, Vector4[] elements, int start = 0, int index = 0)
    {
        if (!HasAttribute(usage)) return;
        
        SlVertexAttribute? attribute = _attributes[usage][index];
        ArgumentNullException.ThrowIfNull(attribute);

        SlStream? stream = streams[attribute.Stream];
        ArgumentNullException.ThrowIfNull(stream);
        
        byte[] data = stream.Data.Array!;
        int streamSize = _streamSizes[attribute.Stream];
        for (int i = 0; i < elements.Length; ++i)
        {
            int offset = stream.Data.Offset + (start + i) * streamSize + attribute.Offset;
            switch (attribute.Type)
            {
                case SlVertexElementType.UByte:
                    for (int j = 0; j < attribute.Count; ++j)
                        data[offset + j] = (byte)elements[i][j];
                    break;
                case SlVertexElementType.UByteN:
                    for (int j = 0; j < attribute.Count; ++j)
                        data[offset + j] = (byte)(elements[i][j] * 255.0f);
                    break;
                case SlVertexElementType.Half:
                    for (int j = 0; j < attribute.Count; ++j)
                    {
                        short half = BitConverter.HalfToInt16Bits((Half)elements[i][j]);
                        data.WriteInt16(half, offset + (j * 2));
                    }
                    
                    break;
                case SlVertexElementType.Float:
                    for (int j = 0; j < attribute.Count; ++j)
                        data.WriteFloat(elements[i][j], offset + (j * 4));
                    break;
                default:
                    throw new NotSupportedException("Unsupported element type!");
            }
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
    public Vector4[] Get(SlStream?[] streams, int usage, int start, int count, int index = 0)
    {
        SlVertexAttribute? attribute = _attributes[usage][index];
        ArgumentNullException.ThrowIfNull(attribute);

        SlStream? stream = streams[attribute.Stream];
        ArgumentNullException.ThrowIfNull(stream);

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
    ///     Swaps the endianness of the vertex streams for a specified platform.
    /// </summary>
    /// <param name="streams">Streams backed by this declaration</param>
    /// <param name="platform">Target platform</param>
    public void SwapEndiannessForPlatform(SlStream?[] streams, SlPlatform platform)
    {
        for (int i = 0; i < streams.Length; ++i)
        {
            SlStream? stream = streams[i];
            if (stream == null) continue;

            // Generally speaking, I don't think it should be possible for an
            // individual stream to not match the target endianness, but just
            // making sure it doesn't happen.
            if (stream.IsBigEndian != platform.IsBigEndian)
                SwapStreamEndianness(streams, i);
        }
    }

    /// <summary>
    ///     Swaps the endianness of a vertex stream.
    /// </summary>
    /// <param name="streams">Streams backed by this declaration</param>
    /// <param name="index">Index of stream to swap endianness of</param>
    private void SwapStreamEndianness(IReadOnlyList<SlStream?> streams, int index)
    {
        SlStream? stream = streams[index];
        ArgumentNullException.ThrowIfNull(stream);
        stream.IsBigEndian = !stream.IsBigEndian;
        
        var attributes = GetFlattenedAttributes();
        int numVerts = stream.Data.Count / _streamSizes[index];
        foreach (SlVertexAttribute attribute in attributes)
        {
            // Don't swap the attribute if it isn't in this stream
            if (attribute.Stream != index) continue;

            // No reason to swap single bytes
            if (attribute.Type is SlVertexElementType.UByte or SlVertexElementType.UByteN) continue;

            int streamSize = _streamSizes[attribute.Stream];
            for (int i = 0; i < numVerts; ++i)
            {
                int offset = i * streamSize + attribute.Offset;
                var data = stream.Data.AsSpan(offset, attribute.Size);
                switch (attribute.Type)
                {
                    case SlVertexElementType.Half:
                        var shortSpan = MemoryMarshal.Cast<byte, short>(data);
                        BinaryPrimitives.ReverseEndianness(shortSpan, shortSpan);
                        break;
                    case SlVertexElementType.Float:
                        var intSpan = MemoryMarshal.Cast<byte, int>(data);
                        BinaryPrimitives.ReverseEndianness(intSpan, intSpan);
                        break;
                    default:
                        throw new NotSupportedException("Unsupported element type!");
                }
            }
        }
    }

    /// <summary>
    ///     Checks if an attribute is contained in this vertex declaration.
    /// </summary>
    /// <param name="usage">The attribute usage</param>
    /// <param name="index">The attribute index</param>
    /// <returns>True, if the attribute is contained in the vertex declaration</returns>
    public bool HasAttribute(int usage, int index = 0)
    {
        return _attributes[usage][index] != null;
    }

    public SlVertexElementType GetAttributeType(int usage, int index = 0)
    {
        return _attributes[usage][index]!.Type;
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

    /// <summary>
    ///     Gets a flattened array of the vertex format attributes in this declaration.
    ///     Sorted by stream and usage in ascending order
    /// </summary>
    /// <returns>Vertex format attributes</returns>
    public List<SlVertexAttribute> GetFlattenedAttributes()
    {
        List<SlVertexAttribute> attributes = [];

        // Wonder if there's some like LINQ version of this?
        for (int i = 0; i < _attributes.Length; ++i)
        for (int j = 0; j < _attributes[i].Length; ++j)
        {
            SlVertexAttribute? attribute = _attributes[i][j];
            if (attribute != null)
                attributes.Add(attribute);
        }

        attributes.Sort((a, z) =>
        {
            return (a.Stream << 16) + (a.Usage << 8) + a.Index -
                   ((z.Stream << 16) + (z.Usage << 8) + z.Index);
        });

        return attributes;
    }
}