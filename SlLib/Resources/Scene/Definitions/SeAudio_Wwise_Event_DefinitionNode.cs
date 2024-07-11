using SlLib.Resources.Database;
using SlLib.Serialization;
using SlLib.Utilities;

namespace SlLib.Resources.Scene.Definitions;

// ReSharper disable once InconsistentNaming
public class SeAudio_Wwise_Event_DefinitionNode : SeDefinitionTransformNode
{
    public string EventName = string.Empty;
    public bool Static;
    public int TriggerType;
    public bool SimultaneousPlay;
    public int GameEvent;
    public float AttenuationScale = 1.0f;
    public bool Environmental = true;
    public bool AreaSound;
    public int EventPicker;
    public int ComboIndex;
    
    private int _prevComboIndex;

    /// <inheritdoc />
    public override void Load(ResourceLoadContext context)
    {
        base.Load(context);

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
        _prevComboIndex = context.ReadInt32(0x120);
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
        context.WriteInt32(buffer, _prevComboIndex, 0x120);
        
        // this is probably that whole SePtrTrifecta field
        // fairly sure these are runtime fields, but just going to try to keep it consistent
        context.WriteInt32(buffer, SlUtil.HashString(EventName), 0xdc);
        if (!string.IsNullOrEmpty(EventName))
        {
            context.WriteInt32(buffer, EventName.Length, 0xe0);
            context.WriteInt32(buffer, EventName.Length + 1, 0xe4);
        }
        context.WriteInt32(buffer, 1, 0xe8);
        context.WriteInt32(buffer, 0xBADF00D, 0xec);
        context.WriteInt32(buffer, 0xBADF00D, 0xf0);
    }

    /// <inheritdoc />
    public override int GetSizeForSerialization(SlPlatform platform, int version) => 0x130;
}