using System.Runtime.Serialization;
using SlLib.Resources.Database;
using SlLib.Resources.Model;
using SlLib.Resources.Model.Commands;
using SlLib.Serialization;

namespace SlLib.Resources;

/// <summary>
///     Resource that contains model data.
/// </summary>
public class SlModelResource : ISumoResource, IWritable
{
    /// <summary>
    ///     Render command buffer.
    /// </summary>
    public readonly List<IRenderCommand> Commands = [];

    /// <summary>
    ///     Cull spheres used for visibility testing.
    /// </summary>
    public readonly List<SlCullSphere> CullSpheres = [];

    /// <summary>
    ///     All render-able meshes in this model.
    /// </summary>
    public readonly List<SlModelSegment> Segments = [];

    /// <summary>
    ///     The vertex declarations used in this model.
    /// </summary>
    public readonly List<SlVertexDeclaration> VertexDeclarations = [];

    /// <summary>
    ///     The vertex streams used in this model.
    /// </summary>
    public readonly List<SlStream> VertexStreams = [];

    /// <summary>
    ///     The index of the first cull sphere parameter in the skeleton's attributes.
    /// </summary>
    public int CullSphereAttributeIndex = -1;

    /// <summary>
    ///     The index of this entity in the skeleton.
    /// </summary>
    public int EntityIndex = -1;

    /// <summary>
    ///     Render flags
    /// </summary>
    public int Flags;

    /// <summary>
    ///     The index stream used in this model.
    /// </summary>
    public SlStream IndexStream = new();

    /// <summary>
    ///     The skeleton instance used by this model.
    /// </summary>
    public SlResPtr<SlSkeleton> Skeleton = SlResPtr<SlSkeleton>.Empty();

    /// <inheritdoc />
    public SlResourceHeader Header { get; set; } = new();

    /// <inheritdoc />
    public void Load(ResourceLoadContext context, int offset)
    {
        Header = context.LoadObject<SlResourceHeader>(offset);

        Skeleton = context.LoadResource<SlSkeleton>(context.ReadInt32(offset + 0xc));
        Flags = context.ReadInt32(offset + 0x10);

        int numSegments = context.ReadInt32(offset + 0x24);
        int segmentData = context.ReadInt32(offset + 0x28);
        for (int i = 0; i < numSegments; ++i)
            Segments.Add(context.LoadReference<SlModelSegment>(segmentData + i * 0x38));

        int commandData = context.ReadInt32(offset + 0x2c);
        int commandOffset = commandData;
        while (context.ReadInt16(commandOffset) != 2)
        {
            int type = context.ReadInt16(commandOffset);
            int size = context.ReadInt16(commandOffset + 2);

            IRenderCommand? command = type switch
            {
                0x00 => new TestVisibilityCommand(),
                0x01 => new RenderSegmentCommand(),
                0x0a => new TestVisibilityNoSphereCommand(),
                0x0b => new CalcBindMatricesCommand(),
                0x0d => new SetDynamicVertexBuffersCommand(),
                _ => null
            };

            // TODO: Add support for missing commands
            // There are a bunch more render commands, especially with models in a track file,
            // the commands supported are enough for rendering character mods, it seems, but should
            // still add support for them at some point.
            if (command == null)
            {
                commandOffset += size;
                continue;
            }

            command.Load(context, commandData, commandOffset);
            Commands.Add(command);
            commandOffset += size;
        }

        int numCullSpheres = context.ReadInt32(offset + 0x30);
        int cullSphereData = context.ReadInt32(offset + 0x34);
        for (int i = 0; i < numCullSpheres; ++i)
            CullSpheres.Add(context.LoadReference<SlCullSphere>(cullSphereData + i * 0x30));

        CullSphereAttributeIndex = context.ReadInt32(offset + 0x38);
        EntityIndex = context.ReadInt32(offset + 0x40);

        int numVertexStreams = context.ReadInt32(offset + 0x48);
        int vertexStreamData = context.ReadInt32(offset + 0x4c);
        for (int i = 0; i < numVertexStreams; ++i)
            VertexStreams.Add(context.LoadReference<SlStream>(vertexStreamData + i * 0x2c));

        int numVertexDecls = context.ReadInt32(offset + 0x50);
        int vertexDeclData = context.ReadInt32(offset + 0x54);
        for (int i = 0; i < numVertexDecls; ++i)
            VertexDeclarations.Add(context.LoadReference<SlVertexDeclaration>(vertexDeclData + i * 0x1c));

        IndexStream = context.LoadPointer<SlStream>(offset + 0x58)!;
    }

    /// <inheritdoc />
    public void Save(ResourceSaveContext context, ISaveBuffer buffer)
    {
        SlSkeleton? skeleton = Skeleton.Instance;
        context.SaveResource(buffer, Skeleton, 0xc);
        context.WriteInt32(buffer, Flags, 0x10);
        if (skeleton != null)
        {
            context.WriteInt32(buffer, skeleton.Joints.Count, 0x14);
            context.WriteInt32(buffer, skeleton.Attributes.Count, 0x1c);
        }

        // Doing all the serialization here in a weird way to make sure the data stays in the same order,
        // For example...
        // Weight/Joint streams of buffers get serialized before any of the vertex/index buffers get saved
        // Dynamic vertex command buffers get serialized before any of the other render commannd data
        // Header is saved after most data, but before vertex streams

        context.WriteInt32(buffer, Segments.Count, 0x24);
        ISaveBuffer segmentData = context.SaveGenericPointer(buffer, 0x28, 0x38 * Segments.Count, 0x10);
        for (int i = 0; i < Segments.Count; ++i)
        {
            ISaveBuffer segmentBuffer = segmentData.At(i * 0x38, 0x38);
            SlModelSegment segment = Segments[i];

            context.WriteInt32(segmentBuffer, (int)segment.PrimitiveType, 0x0);
            context.WriteInt32(segmentBuffer, segment.MaterialIndex, 0x4);
            context.WriteInt32(segmentBuffer, segment.VertexStart, 0x8);
            context.WriteInt32(segmentBuffer, segment.FirstIndex, 0xc);
            context.WriteInt32(segmentBuffer, segment.Sectors.Count, 0x10);

            ISaveBuffer sectorData = context.SaveGenericPointer(segmentBuffer, 0x14, 0x2c * segment.Sectors.Count);
            for (int j = 0; j < segment.Sectors.Count; ++j)
                context.SaveObject(sectorData, segment.Sectors[j], j * 0x2c);

            // Skipping over stream and index buffers for now

            context.SaveBufferPointer(segmentBuffer, segment.WeightBuffer, 0x2c);
            context.SaveBufferPointer(segmentBuffer, segment.JointBuffer, 0x30, 0x10);
        }

        int commandBufferSize = 0x2;
        int dynBufCount = 0x0;
        foreach (IRenderCommand command in Commands)
        {
            commandBufferSize += command.Size;
            if (command is SetDynamicVertexBuffersCommand dynVertexBufferCmd)
                dynBufCount += dynVertexBufferCmd.Buffers.Count;
        }

        ISaveBuffer commandData = context.SaveGenericPointer(buffer, 0x2c, commandBufferSize, 0x10);
        ISaveBuffer? dynData = null;

        // Pre-allocate the dynamic vertex buffer command to preserve original order of data
        if (dynBufCount != 0)
            dynData = context.Allocate(dynBufCount * 0x4, 0x10);

        int commandOffset = 0;
        int dynOffset = 0;
        foreach (IRenderCommand command in Commands)
        {
            ISaveBuffer crumb = commandData.At(commandOffset, command.Size);
            ISaveBuffer? dynCrumb = null;

            // Kind of gross, but whatever
            if (command is SetDynamicVertexBuffersCommand dynVertexBufferCmd)
            {
                int size = dynVertexBufferCmd.Buffers.Count * 4;
                dynCrumb = dynData!.At(dynOffset, size);
                dynOffset += size;
            }

            context.WriteInt16(crumb, (short)command.Type, 0x0);
            context.WriteInt16(crumb, (short)command.Size, 0x2);

            command.Save(context, commandData, crumb, dynCrumb);

            commandOffset += command.Size;
        }

        context.WriteInt16(commandData, 2, commandOffset);

        // Cull sphere related data
        context.WriteInt32(buffer, CullSpheres.Count, 0x30);
        ISaveBuffer cullBuffer = context.SaveGenericPointer(buffer, 0x34, CullSpheres.Count * 0x30, 0x10);
        for (int i = 0; i < CullSpheres.Count; ++i)
            context.SaveObject(cullBuffer, CullSpheres[i], i * 0x30);
        context.WriteInt32(buffer, CullSphereAttributeIndex, 0x38);

        // Not actually sure what this index actually is,
        // it's always -1 it seems? Will have to check all models to see if
        // it's ever not at some point.
        context.WriteInt32(buffer, -1, 0x3c);
        context.WriteInt32(buffer, EntityIndex, 0x40);

        // A bunch of structures reference themselves for whatever reason.
        context.SavePointer(buffer, this, 0x44);
        context.SaveObject(buffer, Header, 0);

        // Now to finally do all the vertex streams and declarations before fixing up the
        // model segments.
        context.WriteInt32(buffer, VertexStreams.Count, 0x48);
        ISaveBuffer vertexData = context.SaveGenericPointer(buffer, 0x4c, VertexStreams.Count * 0x2c);
        for (int i = 0; i < VertexStreams.Count; ++i)
            context.SaveReference(vertexData, VertexStreams[i], i * 0x2c);

        context.WriteInt32(buffer, VertexDeclarations.Count, 0x50);
        ISaveBuffer declData = context.SaveGenericPointer(buffer, 0x54, VertexDeclarations.Count * 0x1c);
        for (int i = 0; i < VertexDeclarations.Count; ++i)
            context.SaveReference(declData, VertexDeclarations[i], i * 0x1c);

        context.SavePointer(buffer, IndexStream, 0x58);

        // Fixup stream references in model segments now that we've serialized everything else
        for (int i = 0; i < Segments.Count; ++i)
        {
            ISaveBuffer segmentBuffer = segmentData.At(i * 0x38, 0x38);
            SlModelSegment segment = Segments[i];

            if (!VertexDeclarations.Contains(segment.Format))
                throw new SerializationException("Model resource contains unregistered vertex declaration!");

            context.SavePointer(segmentBuffer, segment.Format, 0x18);
            for (int j = 0; j < 3; ++j)
            {
                SlStream? stream = segment.VertexStreams[j];
                if (stream == null) continue;

                if (!VertexStreams.Contains(stream))
                    throw new SerializationException("Model resource contains unregistered vertex stream!");

                context.SavePointer(segmentBuffer, stream, 0x1c + j * 4);
            }

            if (segment.IndexStream != IndexStream)
                throw new SerializationException("Model resource contains unregistered index stream!");

            context.SavePointer(segmentBuffer, segment.IndexStream, 0x28);
        }
    }

    /// <inheritdoc />
    public int GetAllocatedSize()
    {
        return 0x60;
    }
}