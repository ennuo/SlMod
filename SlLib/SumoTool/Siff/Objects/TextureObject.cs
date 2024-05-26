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

    public bool IsBilinear;
    public int BlendType;
    public int Layer;
    public Vector2 Anchor;

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

    public int GetSizeForSerialization(SlPlatform platform, int version)
    {
        return 0x28 + platform.GetPointerSize() * 0x5;
    }
}