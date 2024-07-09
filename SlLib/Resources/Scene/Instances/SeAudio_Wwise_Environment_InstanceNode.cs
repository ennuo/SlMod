using SlLib.Resources.Database;
using SlLib.Serialization;

namespace SlLib.Resources.Scene.Instances;

// ReSharper disable once InconsistentNaming
public class SeAudio_Wwise_Environment_InstanceNode : SeInstanceTransformNode
{
    // 0 = sphere, 1 = box
    public int GameEvent;
    public float TransitionLength;
    public float Radius;
    public float Width;
    public float Height;
    public float Length;

    /// <inheritdoc />
    public override void Load(ResourceLoadContext context)
    {
        context.Position = LoadInternal(context, context.Position);

        GameEvent = context.ReadInt32(0x160);
        TransitionLength = context.ReadFloat(0x164);
        Radius = context.ReadFloat(0x16c);
        Width = context.ReadFloat(0x170);
        Height = context.ReadFloat(0x174);
        Length = context.ReadFloat(0x178);
    }

    /// <inheritdoc />
    public override void Save(ResourceSaveContext context, ISaveBuffer buffer)
    {
        base.Save(context, buffer);

        context.WriteInt32(buffer, GameEvent, 0x160);
        context.WriteFloat(buffer, TransitionLength, 0x164);
        context.WriteFloat(buffer, Radius, 0x16c);
        context.WriteFloat(buffer, Width, 0x170);
        context.WriteFloat(buffer, Height, 0x174);
        context.WriteFloat(buffer, Length, 0x178);
    }

    /// <inheritdoc />
    public override int GetSizeForSerialization(SlPlatform platform, int version) => 0x1d0;
}