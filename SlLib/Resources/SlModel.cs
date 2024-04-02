using SlLib.Resources.Database;
using SlLib.Resources.Model;
using SlLib.Serialization;

namespace SlLib.Resources;

public class SlModel : ISumoResource, IWritable
{
    /// <summary>
    ///     Cull sphere used for visibility testing.
    /// </summary>
    public SlCullSphere CullSphere = new();

    /// <summary>
    ///     List of materials that this model uses.
    /// </summary>
    public List<int> Materials = [];

    /// <summary>
    ///     Mesh data
    /// </summary>
    public SlModelResource Resource = new();

    /// <summary>
    ///     The work buffer used by render commands for storing results of operations.
    /// </summary>
    public ArraySegment<byte> WorkArea;

    /// <inheritdoc />
    public SlResourceHeader Header { get; set; } = new();

    /// <inheritdoc />
    public void Load(ResourceLoadContext context, int offset)
    {
        Header = context.LoadObject<SlResourceHeader>(offset);
        Resource = context.LoadPointer<SlModelResource>(offset + 0xc)!;
        CullSphere = context.LoadObject<SlCullSphere>(offset + 0x10);

        int materialCount = context.ReadInt32(offset + 0x40);
        int materialData = context.ReadInt32(offset + 0x44);
        for (int i = 0; i < materialCount; ++i)
            Materials.Add(context.ReadInt32(materialData + i * 4));

        int workAreaSize = context.ReadInt32(offset + 0x54);
        WorkArea = context.LoadBufferPointer(offset + 0x58, workAreaSize);
    }

    /// <inheritdoc />
    public void Save(ResourceSaveContext context)
    {
    }
}