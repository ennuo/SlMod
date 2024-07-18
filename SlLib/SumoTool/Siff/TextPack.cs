using System.Runtime.Serialization;
using SlLib.Resources.Database;
using SlLib.Serialization;

namespace SlLib.SumoTool.Siff;

public class TextPack : Dictionary<int, string>, IResourceSerializable
{
    /// <inheritdoc />
    public void Load(ResourceLoadContext context)
    {
        context.Position += context.Platform.GetPointerSize() * 2;
        int numEntries = context.ReadInt32();

        int textData = context.Position;
        
        // quick lookup hashes
        if (!context.IsSSR)
        {
            context.ReadPointer();
            textData = context.ReadPointer();
        }
        
        context.Position = textData;
        for (int i = 0; i < numEntries; ++i)
        {
            int hash = context.ReadInt32();
            string text = context.ReadStringPointer();
            Add(hash, text);
        }
    }

    public void Save(ResourceSaveContext context, ISaveBuffer buffer)
    {
        if (!context.IsSSR) throw new SerializationException("Only SSR serialization of text packs is supported!");
        
        context.WriteInt32(buffer, Count, 0x8);
        
        for (int i = 0; i < Count; ++i)
        {
            int offset = 0xc + (i * 0x4);
            var pair = this.ElementAt(i);
            
            context.WriteInt32(buffer, pair.Key, offset);
            context.WriteStringPointer(buffer, pair.Value, offset + 4);
        }
    }

    public int GetSizeForSerialization(SlPlatform platform, int version)
    {
        if (version == -1)
            return 0xc + (Count * 0x8);
        return platform.Is64Bit ? 0x24 : 0x14;
    }
}