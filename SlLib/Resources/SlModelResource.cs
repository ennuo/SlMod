using SlLib.Resources.Database;
using SlLib.Resources.Model;
using SlLib.Resources.Model.Commands;
using SlLib.Serialization;

namespace SlLib.Resources;

public class SlModelResource : ISumoResource
{
    /// <summary>
    ///     Render command buffer.
    /// </summary>
    public List<IRenderCommand> Commands = [];

    /// <summary>
    ///     The index of the first cull sphere parameter in the skeleton's attributes.
    /// </summary>
    public int CullSphereAttributeIndex = -1;

    /// <summary>
    ///     Cull spheres used for visibility testing.
    /// </summary>
    public List<SlCullSphere> CullSpheres = [];

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
    ///     All render-able meshes in this model.
    /// </summary>
    public List<SlModelSegment> Segments = [];

    /// <summary>
    ///     The skeleton instance used by this model.
    /// </summary>
    public SlSkeleton? Skeleton;

    /// <summary>
    ///     The vertex declarations used in this model.
    /// </summary>
    public List<SlVertexDeclaration> VertexDeclarations = [];

    /// <summary>
    ///     The vertex streams used in this model.
    /// </summary>
    public List<SlStream> VertexStreams = [];

    /// <inheritdoc />
    public SlResourceHeader Header { get; set; } = new();

    public void Load(ResourceLoadContext context, int offset)
    {
        Header = context.LoadObject<SlResourceHeader>(offset);

        Skeleton = context.LoadResource<SlSkeleton>(context.ReadInt32(offset + 12));
        Flags = context.ReadInt32(offset + 16);

        int numSegments = context.ReadInt32(offset + 36);
        int segmentData = context.ReadInt32(offset + 40);
        for (int i = 0; i < numSegments; ++i)
            Segments.Add(context.LoadReference<SlModelSegment>(segmentData + i * 0x38));

        int commandData = context.ReadInt32(offset + 44);
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

        int numCullSpheres = context.ReadInt32(offset + 48);
        int cullSphereData = context.ReadInt32(offset + 52);
        for (int i = 0; i < numCullSpheres; ++i)
            CullSpheres.Add(context.LoadReference<SlCullSphere>(cullSphereData + i * 48));

        CullSphereAttributeIndex = context.ReadInt32(offset + 56);
        EntityIndex = context.ReadInt32(offset + 60);

        int numVertexStreams = context.ReadInt32(offset + 72);
        int vertexStreamData = context.ReadInt32(offset + 76);
        for (int i = 0; i < numVertexStreams; ++i)
            VertexStreams.Add(context.LoadReference<SlStream>(vertexStreamData + i * 0x2c));

        int numVertexDecls = context.ReadInt32(offset + 80);
        int vertexDeclData = context.ReadInt32(offset + 84);
        for (int i = 0; i < numVertexDecls; ++i)
            VertexDeclarations.Add(context.LoadReference<SlVertexDeclaration>(vertexDeclData + i * 0x1c));

        IndexStream = context.LoadPointer<SlStream>(offset + 88)!;
    }
}