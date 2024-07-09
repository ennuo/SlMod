using SlLib.Resources.Database;
using SlLib.Serialization;

namespace SlLib.Resources.Scene.Definitions;

public class SeAudio_Wwise_Event_DefinitionNode : SeDefinitionTransformNode
{
    public string EventName;
    public bool Static;
    public int TriggerType;
    public bool SimultaneousPlay;
    public int GameEvent;
    public float AttenuationScale;
    public bool Environmental;
    public bool AreaSound;
    public int EventPicker;
    public int ComboIndex;
    public int PrevComboIndex;

    /// <inheritdoc />
    public override void Load(ResourceLoadContext context)
    {
        context.Position = LoadInternal(context, context.Position);

        EventName = context.ReadStringPointer(0xd4);
        Static = context.ReadBoolean(0xf4);
        TriggerType = context.ReadInt32(0xfc);
        SimultaneousPlay = context.ReadBoolean(0x100);
        GameEvent = context.ReadInt32(0x104);
        AttenuationScale = context.ReadFloat(0x110);
        Environmental = context.ReadBoolean(0x114);
        AreaSound = context.ReadBoolean(0x118);
        EventPicker = context.ReadInt32(0x11c);
        ComboIndex = context.ReadInt32(0x11c);
        PrevComboIndex = context.ReadInt32(0x120);
    }

    /// <inheritdoc />
    public override void Save(ResourceSaveContext context, ISaveBuffer buffer)
    {
        base.Save(context, buffer);

        context.WriteStringPointer(buffer, EventName, 0xd4);
        context.WriteBoolean(buffer, Static, 0xf4);
        context.WriteInt32(buffer, TriggerType, 0xfc);
        context.WriteBoolean(buffer, SimultaneousPlay, 0x100);
        context.WriteInt32(buffer, GameEvent, 0x104);
        context.WriteFloat(buffer, AttenuationScale, 0x110);
        context.WriteBoolean(buffer, Environmental, 0x114);
        context.WriteBoolean(buffer, AreaSound, 0x118);
        context.WriteInt32(buffer, EventPicker, 0x11c);
        context.WriteInt32(buffer, ComboIndex, 0x11c);
        context.WriteInt32(buffer, PrevComboIndex, 0x120);
    }

    /// <inheritdoc />
    public override int GetSizeForSerialization(SlPlatform platform, int version) => 0x130;
}