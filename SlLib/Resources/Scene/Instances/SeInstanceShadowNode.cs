using SlLib.Resources.Database;
using SlLib.Serialization;

namespace SlLib.Resources.Scene.Instances;

public class SeInstanceShadowNode : SeInstanceTransformNode
{
    public int CascadDimensions0;
    public int CascadDimensions1;
    public int CascadDimensions2;
    public int CascadDimensions3;
    public float ManualSplitDistance0;
    public float ManualSplitDistance1;
    public float ManualSplitDistance2;
    public float ManualSplitDistance3;
    public float FilterScale0;
    public float FilterScale1;
    public float FilterScale2;
    public float FilterScale3;
    public bool AutoSplitDistances;
    public int NumSplits;
    public float AutoSplitLambda;
    public float AutoSplitMaxDist;
    public float LightDist;
    public float BlendFactor;
    public float BiasOffset;
    public float BiasScale;

    /// <inheritdoc />
    public override void Load(ResourceLoadContext context)
    {
        base.Load(context);

        CascadDimensions0 = context.ReadInt32(0x160);
        CascadDimensions1 = context.ReadInt32(0x164);
        CascadDimensions2 = context.ReadInt32(0x168);
        CascadDimensions3 = context.ReadInt32(0x16c);
        ManualSplitDistance0 = context.ReadFloat(0x170);
        ManualSplitDistance1 = context.ReadFloat(0x174);
        ManualSplitDistance2 = context.ReadFloat(0x178);
        ManualSplitDistance3 = context.ReadFloat(0x17c);
        FilterScale0 = context.ReadFloat(0x180);
        FilterScale1 = context.ReadFloat(0x184);
        FilterScale2 = context.ReadFloat(0x188);
        FilterScale3 = context.ReadFloat(0x18c);
        AutoSplitDistances = context.ReadBoolean(0x190);
        NumSplits = context.ReadInt32(0x194);
        AutoSplitLambda = context.ReadFloat(0x198);
        AutoSplitMaxDist = context.ReadFloat(0x19c);
        LightDist = context.ReadFloat(0x1a0);
        BlendFactor = context.ReadFloat(0x1a4);
        BiasOffset = context.ReadFloat(0x1a8);
        BiasScale = context.ReadFloat(0x1ac);
    }

    /// <inheritdoc />
    public override void Save(ResourceSaveContext context, ISaveBuffer buffer)
    {
        base.Save(context, buffer);

        context.WriteInt32(buffer, CascadDimensions0, 0x160);
        context.WriteInt32(buffer, CascadDimensions1, 0x164);
        context.WriteInt32(buffer, CascadDimensions2, 0x168);
        context.WriteInt32(buffer, CascadDimensions3, 0x16c);
        context.WriteFloat(buffer, ManualSplitDistance0, 0x170);
        context.WriteFloat(buffer, ManualSplitDistance1, 0x174);
        context.WriteFloat(buffer, ManualSplitDistance2, 0x178);
        context.WriteFloat(buffer, ManualSplitDistance3, 0x17c);
        context.WriteFloat(buffer, FilterScale0, 0x180);
        context.WriteFloat(buffer, FilterScale1, 0x184);
        context.WriteFloat(buffer, FilterScale2, 0x188);
        context.WriteFloat(buffer, FilterScale3, 0x18c);
        context.WriteBoolean(buffer, AutoSplitDistances, 0x190);
        context.WriteInt32(buffer, NumSplits, 0x194);
        context.WriteFloat(buffer, AutoSplitLambda, 0x198);
        context.WriteFloat(buffer, AutoSplitMaxDist, 0x19c);
        context.WriteFloat(buffer, LightDist, 0x1a0);
        context.WriteFloat(buffer, BlendFactor, 0x1a4);
        context.WriteFloat(buffer, BiasOffset, 0x1a8);
        context.WriteFloat(buffer, BiasScale, 0x1ac);
    }

    /// <inheritdoc />
    public override int GetSizeForSerialization(SlPlatform platform, int version) => 0x200;
}
