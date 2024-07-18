using SlLib.Resources.Database;
using SlLib.Serialization;

namespace SlLib.SumoTool.Siff.Forest;

public class SuRenderTexture : IResourceSerializable
{
    public int Flags;
    public SuRenderTextureResource? TextureResource;
    public int Param0, Param1, Param2;
    
    public void Load(ResourceLoadContext context)
    {
        Flags = context.ReadInt32();
        TextureResource = context.LoadPointer<SuRenderTextureResource>();
        Param0 = context.ReadInt32();
        Param1 = context.ReadInt32();
        Param2 = context.ReadInt32();
    }

    public void Save(ResourceSaveContext context, ISaveBuffer buffer)
    {
        context.WriteInt32(buffer, Flags, 0x0);
        context.SavePointer(buffer, TextureResource, 0x4, deferred: true);
        context.WriteInt32(buffer, Param0, 0x8);
        context.WriteInt32(buffer, Param1, 0xc);
        context.WriteInt32(buffer, Param2, 0x10);
    }

    public int GetSizeForSerialization(SlPlatform platform, int version)
    {
        return 0x14;
    }
}