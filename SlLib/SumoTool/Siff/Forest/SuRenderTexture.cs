using SlLib.Resources.Database;
using SlLib.Serialization;

namespace SlLib.SumoTool.Siff.Forest;

public class SuRenderTexture : IResourceSerializable
{
    public int Flags;
    public SuRenderTextureResource? TextureResource;
    public float MipmapBias; // dont know if thats actually what this is
    
    public void Load(ResourceLoadContext context)
    {
        Flags = context.ReadInt32();
        TextureResource = context.LoadPointer<SuRenderTextureResource>();
        context.Position += 8; // seems to usually just be 0
        MipmapBias = context.ReadFloat();
    }

    public void Save(ResourceSaveContext context, ISaveBuffer buffer)
    {
        context.WriteInt32(buffer, Flags, 0x0);
        context.SavePointer(buffer, TextureResource, 0x4);
        context.WriteFloat(buffer, MipmapBias, 0x10);
    }

    public int GetSizeForSerialization(SlPlatform platform, int version)
    {
        return 0x14;
    }
}