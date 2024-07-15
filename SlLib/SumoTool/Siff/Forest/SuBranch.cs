using System.Runtime.Serialization;
using SlLib.Resources.Database;
using SlLib.Serialization;
using SlLib.Utilities;

namespace SlLib.SumoTool.Siff.Forest;

public class SuBranch : IResourceSerializable
{
    /// <summary>
    ///     The index of the parent of this branch.
    /// </summary>
    public short Parent = -1;

    /// <summary>
    ///     The index of the first child of this branch.
    /// </summary>
    public short Child = -1;

    /// <summary>
    ///     The index of the next child of this branch.
    /// </summary>
    public short Sibling = -1;

    /// <summary>
    ///     Branch flags.
    /// </summary>
    public short Flags;

    /// <summary>
    ///     The name of this branch.
    /// </summary>
    public string Name = string.Empty;

    /// <summary>
    ///     Blind data contained by this branch.
    /// </summary>
    public SuBlindData? BlindData;

    /// <summary>
    ///     Level of detail data held by this branch, if (flags & 16)
    /// </summary>
    public SuLodBranch? Lod;

    /// <summary>
    ///     The mesh data held by this branch if (flags & 8)
    /// </summary>
    public SuRenderMesh? Mesh;
    
    public void Load(ResourceLoadContext context)
    {
        Parent = context.ReadInt16();
        Child = context.ReadInt16();
        Sibling = context.ReadInt16();
        Flags = context.ReadInt16();
        int hash = context.ReadInt32();
        Name = context.ReadStringPointer();

        BlindData = context.LoadPointer<SuBlindData>();
        
        bool isLodBranch = (Flags & 16) != 0;
        bool hasMeshData = (Flags & 8) != 0;

        if (isLodBranch && hasMeshData)
            throw new SerializationException("SuBranch cannot contain both LOD and mesh data!");
        
        // These flags shouldn't be ticked at the same time,
        // LOD branch contains multiple render mesh definitions.
        if (isLodBranch) Lod = context.LoadObject<SuLodBranch>();
        else if (hasMeshData) Mesh = context.LoadPointer<SuRenderMesh>();
    }

    public void Save(ResourceSaveContext context, ISaveBuffer buffer)
    {
        context.WriteInt16(buffer, Parent, 0x0);
        context.WriteInt16(buffer, Child, 0x2);
        context.WriteInt16(buffer, Sibling, 0x4);
        context.WriteInt16(buffer, Flags, 0x6);
        context.WriteInt32(buffer, SlUtil.SumoHash(Name), 0x8);
        context.WriteStringPointer(buffer, Name, 0xc);
        context.SavePointer(buffer, BlindData, 0x10);
        
        bool isLodBranch = (Flags & 16) != 0;
        bool hasMeshData = (Flags & 8) != 0;
        if (isLodBranch && hasMeshData)
            throw new SerializationException("SuBranch cannot contain both LOD and mesh data!");

        if (isLodBranch)
        {
            if (Lod == null)
                throw new SerializationException("SuLodBranch cannot be NULL if LOD flags are ticked!");
            context.SaveObject(buffer, Lod, 0x14);
        }
        else if (hasMeshData) context.SavePointer(buffer, Mesh, 0x14);
    }
    
    public int GetSizeForSerialization(SlPlatform platform, int version)
    {
        // Technically supposed to extend from SuBranch, but don't know if something is a branch
        // until we read the flags, so whatever, it's also the only subclass.
        if (Lod != null)
            return 0x70;
        
        // Extra pointer only contained if the branch contains mesh data?
        return (Flags & 8) != 0 ? 0x18 : 0x14;
    }
}