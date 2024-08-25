using System.Runtime.Serialization;
using SlLib.Resources.Database;
using SlLib.Serialization;
using SlLib.SumoTool.Siff.Entry;

namespace SlLib.SumoTool.Siff;

public class FontPack : IResourceSerializable
{
    public List<Font> Fonts = [];
    
    public void Load(ResourceLoadContext context)
    {
        context.Position += context.Platform.GetPointerSize() * 2;
        int numEntries = context.ReadInt32();
        if (!context.IsSSR)
        {
            int hashData = context.ReadPointer();
            int tableData = context.ReadPointer();
            context.Position = tableData;
            for (int i = 0; i < numEntries; ++i)
                Fonts.Add(context.LoadPointer<Font>() ?? throw new SerializationException("Font entry was NULL!"));
        }
        else
        {
            for (int i = 0; i < numEntries; ++i)
                Fonts.Add(context.LoadObject<Font>());   
        }
    }
    
    public void Save(ResourceSaveContext context, ISaveBuffer buffer)
    {
        context.WriteInt32(buffer, Fonts.Count, 0x8);
        for (int i = 0; i < Fonts.Count; ++i)
            context.SaveObject(buffer, Fonts[i], 0xc + (i * 0x20));
    }
    
    public int GetSizeForSerialization(SlPlatform platform, int version)
    {
        return 0xc + (0x20 * Fonts.Count);
    }
}