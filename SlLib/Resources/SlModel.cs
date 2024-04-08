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
    public readonly List<SlResPtr<SlMaterial2>> Materials = [];

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
            Materials.Add(context.LoadResourcePointer<SlMaterial2>(materialData + i * 4));

        int workAreaSize = context.ReadInt32(offset + 0x54);
        WorkArea = context.LoadBufferPointer(offset + 0x58, workAreaSize, out _);
    }

    /// <inheritdoc />
    public void Save(ResourceSaveContext context, ISaveBuffer buffer)
    {
        // As per usual, most of this is serialized in a weird order to make sure
        // that the original file can be re-created 1:1.

        context.SaveObject(buffer, CullSphere, 0x10);

        // Should I add helper methods for dealing with arrays?
        context.WriteInt32(buffer, Materials.Count, 0x40);
        ISaveBuffer materialData = context.SaveGenericPointer(buffer, 0x44, Materials.Count * 4);
        for (int i = 0; i < Materials.Count; ++i)
            context.SaveResource(materialData, Materials[i], i * 4);

        // Not sure what this actually is, always seems to be 1?
        context.WriteInt32(buffer, 1, 0x50);

        context.WriteInt32(buffer, WorkArea.Count, 0x54);
        context.SaveBufferPointer(buffer, WorkArea, 0x58, 0x10);

        context.SavePointer(buffer, this, 0x5c);

        // Saving the header last because the tag string is always
        // after all the data for whatever reason, do they serialize backwards or something?
        context.SaveObject(buffer, Header, 0x0);

        context.SavePointer(buffer, Resource, 0xc);
    }

    /// <inheritdoc />
    public int GetAllocatedSize()
    {
        return 0x60;
    }
}