using SlLib.Resources.Database;
using SlLib.Serialization;

namespace SlLib.SumoTool.Siff.Forest;

public class SuRenderForest : IResourceSerializable
{
    public string Name = string.Empty;
    
    public List<SuRenderTree> Trees = [];
    public List<SuRenderTextureResource> TextureResources = [];
    public List<SuTreeGroup> Groups = [];
    public List<SuRenderTexture> Textures = [];
    public SuBlindData? BlindData;
    
    public void Load(ResourceLoadContext context)
    {
        Trees = context.LoadPointerArray<SuRenderTree>(context.ReadInt32());
        TextureResources = context.LoadPointerArray<SuRenderTextureResource>(context.ReadInt32());
        Groups = context.LoadArrayPointer<SuTreeGroup>(context.ReadInt32());
        Textures = context.LoadPointerArray<SuRenderTexture>(context.ReadInt32());
        BlindData = context.LoadPointer<SuBlindData>();
    }

    public void Save(ResourceSaveContext context, ISaveBuffer buffer)
    {
        // Going to do some nonsense to get serialization match 1:1 so I can do some integrity checking, so to speak
        
        
        context.WriteInt32(buffer, Trees.Count, 0x0);
        context.WriteInt32(buffer, TextureResources.Count, 0x8);
        context.WriteInt32(buffer, Groups.Count, 0x10);
        context.WriteInt32(buffer, Textures.Count, 0x18);

        ISaveBuffer treeData = context.SaveGenericPointer(buffer, 0x4, Trees.Count * 0x4);
        ISaveBuffer textureData = context.SaveGenericPointer(buffer, 0x14, Textures.Count * 0x4); // Doing this one first to mimick the weird pointer offset for 0 length
        ISaveBuffer textureResourceData = context.SaveGenericPointer(buffer, 0xc, TextureResources.Count * 0x4);
        ISaveBuffer groupData = context.SaveGenericPointer(buffer, 0x14, Groups.Count * 0xc);
        
        for (int i = 0; i < Trees.Count; ++i) context.SavePointer(treeData, Trees[i], i * 4);
        
        
        
        

        
        
        
        
        
        // context.SavePointerArray(buffer, Trees, 0x4, align: 0x10);
        // context.SavePointerArray(buffer, TextureResources, 0xc);
        // context.SaveReferenceArray(buffer, Groups, 0x14);
        // context.SavePointerArray(buffer, Textures, 0x1c);
        // context.SavePointer(buffer, BlindData, 0x20);
        
        
        
        
        
        

        // Old straightforward version
        // context.WriteInt32(buffer, Trees.Count, 0x0);
        // context.SavePointerArray(buffer, Trees, 0x4, align: 0x10);
        // context.WriteInt32(buffer, TextureResources.Count, 0x8);
        // context.SavePointerArray(buffer, TextureResources, 0xc);
        // context.WriteInt32(buffer, Groups.Count, 0x10);
        // context.SaveReferenceArray(buffer, Groups, 0x14);
        // context.WriteInt32(buffer, Textures.Count, 0x18);
        // context.SavePointerArray(buffer, Textures, 0x1c);
        // context.SavePointer(buffer, BlindData, 0x20);
    }
    
    public int GetSizeForSerialization(SlPlatform platform, int version)
    {
        return 0x24;
    }
}