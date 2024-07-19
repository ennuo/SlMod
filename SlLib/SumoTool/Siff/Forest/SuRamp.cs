using SlLib.Resources.Database;
using SlLib.Serialization;

namespace SlLib.SumoTool.Siff.Forest;

public class SuRamp : IResourceSerializable
{
    public int Shift;
    public List<int> Values = [];
    
    public void Load(ResourceLoadContext context)
    {
        Shift = context.ReadInt32();
        for (int i = 0; i < 50; ++i)
            Values.Add(context.ReadInt32());
    }

    public void Save(ResourceSaveContext context, ISaveBuffer buffer)
    {
        context.WriteInt32(buffer, Shift, 0);
        for (int i = 0; i < 50; ++i)
            context.WriteInt32(buffer, Values[i], 0x4 + (i * 4));
    }
    
    public int GetSizeForSerialization(SlPlatform platform, int version)
    {
        return 0x4 + 0x200;
    }
}