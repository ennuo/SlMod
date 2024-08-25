using System.Numerics;
using SlLib.Resources.Database;
using SlLib.Serialization;
using SlLib.SumoTool.Siff.Fonts;

namespace SlLib.SumoTool.Siff.Entry;

public class Font : IResourceSerializable
{
    public int Hash;
    public List<CharacterInfo> Characters = [];
    public List<KerningInfo> KerningData = [];
    public short Ascender;
    public short Descender;
    public int BaselineAdder;
    public Vector2 Scale;
    
    public void Load(ResourceLoadContext context)
    {
        Hash = context.ReadInt32();
        short numChars = context.ReadInt16();
        short numKern = context.ReadInt16();

        // skip the quick lists because i dont care
        if (!context.IsSSR)
            context.Position += context.Platform.GetPointerSize() * 2;

        Characters = context.LoadArrayPointer<CharacterInfo>(numChars);
        KerningData = context.LoadArrayPointer<KerningInfo>(numKern);

        Ascender = context.ReadInt16();
        Descender = context.ReadInt16();
        
        BaselineAdder = context.ReadInt32();
        
        Scale = context.ReadFloat2();
    }

    public void Save(ResourceSaveContext context, ISaveBuffer buffer)
    {
        context.WriteInt32(buffer, Hash, 0x0);
        context.WriteInt16(buffer, (short)Characters.Count, 0x4);
        context.WriteInt16(buffer, (short)KerningData.Count, 0x6);
        context.SaveReferenceArray(buffer, Characters, 0x8);
        context.SaveReferenceArray(buffer, KerningData, 0xc);
        context.WriteInt16(buffer, Ascender, 0x10);
        context.WriteInt16(buffer, Descender, 0x12);
        context.WriteInt32(buffer, BaselineAdder, 0x14);
        context.WriteFloat2(buffer, Scale, 0x18);
    }
    
    public int GetSizeForSerialization(SlPlatform platform, int version)
    {
        return 0x20;
        // return version == -1 ? 0x20 : 0x28;
    }
}