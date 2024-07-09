using SlLib.Resources.Database;
using SlLib.Serialization;

namespace SlLib.Resources.Scene.Instances;

public abstract class SeInstanceTimeLineEventNodeBase : SeInstanceNode
{
    public int IsActive;
    public float Start;
    public float Duration;
    public float End;

    /// <inheritdoc />
    public override void Load(ResourceLoadContext context)
    {
        base.Load(context);

        IsActive = context.ReadInt32(0x80);
        Start = context.ReadFloat(0x84);
        Duration = context.ReadFloat(0x88);
        End = context.ReadFloat(0x8c);
    }

    /// <inheritdoc />
    public override void Save(ResourceSaveContext context, ISaveBuffer buffer)
    {
        base.Save(context, buffer);

        context.WriteInt32(buffer, IsActive, 0x80);
        context.WriteFloat(buffer, Start, 0x84);
        context.WriteFloat(buffer, Duration, 0x88);
        context.WriteFloat(buffer, End, 0x8c);
    }

    /// <inheritdoc />
    public override int GetSizeForSerialization(SlPlatform platform, int version) => 0xa0;
}