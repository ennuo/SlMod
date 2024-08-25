using System.Numerics;
using SlLib.Resources.Database;
using SlLib.Serialization;

namespace SlLib.SumoTool.Siff.Objects;

public class TextureObject : IObjectDef
{
    public string ObjectType => "TXTR";

    public int TextureHash;
    public int TextureEffectHash;
    public int KeyframeHash;
    public int PointerAreaHash;
    public int ScissorHash;

    public bool IsBilinear = true;
    public int BlendType;
    public int Layer { get; set; } = 100;
    public Vector2 Anchor { get; set; }

    public void Load(ResourceLoadContext context)
    {
        TextureHash = context.ReadInt32();
        TextureEffectHash = context.ReadInt32();
        KeyframeHash = context.ReadInt32();
        PointerAreaHash = context.ReadInt32();
        ScissorHash = context.ReadInt32();

        context.Position += context.Platform.GetPointerSize() * 0x5;

        IsBilinear = context.ReadBoolean(true);
        BlendType = context.ReadInt32();
        Layer = context.ReadInt32();
        Anchor = context.ReadFloat2();
    }

    public void Save(ResourceSaveContext context, ISaveBuffer buffer)
    {
        context.WriteInt32(buffer, TextureHash, 0x0);
        context.WriteInt32(buffer, TextureEffectHash, 0x4);
        context.WriteInt32(buffer, KeyframeHash, 0x8);
        context.WriteInt32(buffer, PointerAreaHash, 0xc);
        context.WriteInt32(buffer, ScissorHash, 0x10);
        
        context.WriteBoolean(buffer, IsBilinear, 0x28, wide: true);
        context.WriteInt32(buffer, BlendType, 0x2c);
        context.WriteInt32(buffer, Layer, 0x30);
        context.WriteFloat2(buffer, Anchor, 0x34);
    }

    public int GetSizeForSerialization(SlPlatform platform, int version)
    {
        return 0x28 + platform.GetPointerSize() * 0x5;
    }
}