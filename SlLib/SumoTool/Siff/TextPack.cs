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
        int hashTable = context.ReadPointer();
        int textData = context.ReadPointer();

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
        throw new NotImplementedException();
    }

    public int GetSizeForSerialization(SlPlatform platform, int version)
    {
        return platform.Is64Bit ? 0x24 : 0x14;
    }
}