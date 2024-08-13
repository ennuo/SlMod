using SlLib.Resources.Database;
using SlLib.Serialization;

namespace SlLib.Resources.Collision;

public class SlResourceMesh : IResourceSerializable
{
    public List<SlCollisionMaterial> Materials = [];
    public List<SlResourceMeshSection> Sections = [];
    
    public void Load(ResourceLoadContext context)
    {
        Materials = context.LoadPointerArray<SlCollisionMaterial>(context.ReadInt32());
        Sections = context.LoadPointerArray<SlResourceMeshSection>(context.ReadInt32());
    }
    
    public void Save(ResourceSaveContext context, ISaveBuffer buffer)
    {
        context.WriteInt32(buffer, Materials.Count, 0x0);
        context.WriteInt32(buffer, Sections.Count, 0x8);
        
        // Sneak the pointers into the same buffer as the data
        ISaveBuffer materialData = context.SaveGenericPointer(buffer, 0x4, (0x4 + 0x10) * Materials.Count, align: 1);
        for (int i = 0; i < Materials.Count; ++i)
        {
            int pointerOffset = i * 4;
            int dataOffset = (0x4 * Materials.Count) + (i * 0x10);
            context.SaveReference(materialData, Materials[i], dataOffset);
            context.SavePointer(materialData, Materials[i], pointerOffset);
        }
        
        ISaveBuffer sectionData = context.SaveGenericPointer(buffer, 0xc, (0x4 + 0x18) * Sections.Count, align: 1);
        for (int i = 0; i < Sections.Count; ++i)
        {
            int pointerOffset = i * 4;
            int dataOffset = (0x4 * Sections.Count) + (i * 0x18);
            context.SaveReference(sectionData, Sections[i], dataOffset);
            context.SavePointer(sectionData, Sections[i], pointerOffset);
        }
    }
    
    public int GetSizeForSerialization(SlPlatform platform, int version)
    {
        return 0x10;
    }
}