using SlLib.Serialization;

namespace SlLib.SumoTool.Siff;

public class TextPack : Dictionary<int, string>, ILoadable
{
    /// <inheritdoc />
    public void Load(ResourceLoadContext context, int offset)
    {
        int numEntries = context.ReadInt32(offset + 8);
        int textData = context.ReadInt32(offset + 16);
        for (int i = 0; i < numEntries; ++i)
        {
            int address = textData + i * 8;

            int hash = context.ReadInt32(address);
            string text = context.ReadStringPointer(address + 4);

            Add(hash, text);
        }
    }
}