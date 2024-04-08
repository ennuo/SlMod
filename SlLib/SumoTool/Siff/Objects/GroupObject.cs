using System.Numerics;
using SlLib.Serialization;

namespace SlLib.SumoTool.Siff.Objects;

public class GroupObject : IObjectDef
{
    public readonly List<int> ObjectHashes = [];
    public Vector2 Anchor;

    public int KeyframeHash;
    public int Layer;
    public int PointerAreaHash;
    public string ObjectType => "GROP";

    public void Load(ResourceLoadContext context, int offset)
    {
        KeyframeHash = context.ReadInt32(offset);
        PointerAreaHash = context.ReadInt32(offset + 4);
        Layer = context.ReadInt32(offset + 16);
        Anchor = context.ReadFloat2(offset + 20);

        int numChildren = context.ReadInt32(offset + 28);
        int childData = context.ReadInt32(offset + 32);
        for (int i = 0; i < numChildren; ++i)
            ObjectHashes.Add(context.ReadInt32(childData + i * 4));
    }
}