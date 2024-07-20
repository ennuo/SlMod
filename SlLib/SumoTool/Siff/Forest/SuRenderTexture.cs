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

        // more dumb hacks
        if (context.Platform == SlPlatform.Xbox360)
        {
            Flags = Flags switch
            {
                1073872896 => 129,
                1342308352 => 133,
                131072 => 128,
                1342570496 => 389,
                1441792 => 1408,
                1476526080 => 149,
                1343356928 => 1157,
                1342439424 => 261,
                1476788224 => 405,
                1477836800 => 1429,
                1375862784 => 165,
                393216 => 384,
                1208090624 => 145,
                1477705728 => 1301,
                1179648 => 1152,
                
                1342701568 => 517,
                1074135040 => 385,
                268566528 => 132,
                
                1376124928 => 421,
                
                
                _ => Flags
            };
            
        }
        
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