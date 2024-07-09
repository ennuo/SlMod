using SlLib.Resources.Database;
using SlLib.Serialization;

namespace SlLib.Resources.Scene.Instances;

public class SeColourGradingInstanceNode : SeInstanceNode
{
    public float RedMultiply;
    public float GreenMultiply;
    public float BlueMultiply;
    public float Blend;

    /// <inheritdoc />
    public override void Load(ResourceLoadContext context)
    {
        context.Position = LoadInternal(context, context.Position);

        RedMultiply = context.ReadFloat(0x84);
        GreenMultiply = context.ReadFloat(0x88);
        BlueMultiply = context.ReadFloat(0x8c);
        Blend = context.ReadFloat(0x90);
    }

    /// <inheritdoc />
    public override void Save(ResourceSaveContext context, ISaveBuffer buffer)
    {
        base.Save(context, buffer);

        context.WriteFloat(buffer, RedMultiply, 0x84);
        context.WriteFloat(buffer, GreenMultiply, 0x88);
        context.WriteFloat(buffer, BlueMultiply, 0x8c);
        context.WriteFloat(buffer, Blend, 0x90);
    }

    /// <inheritdoc />
    public override int GetSizeForSerialization(SlPlatform platform, int version) => 0xa0;
}