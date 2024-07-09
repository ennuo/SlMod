using System.Numerics;
using SlLib.Resources.Database;
using SlLib.Serialization;

namespace SlLib.Resources.Scene.Definitions;

public class WeaponPodDefinitionNode : SeDefinitionTransformNode
{
    public int SendChildMessages;
    public float RespawnTime = 3.0f;
    public float BoxWidth = 1.3f;
    public float BoxHeight = 1.3f;
    public float BoxDepth = 0.75f;
    public Vector3 ModelOffset = new Vector3(0.4f, 0.0f, 1.0f);
    
    /// <inheritdoc />
    public override void Load(ResourceLoadContext context)
    {
        base.Load(context);
        
        // old versions extended SeDefinitionEntityNode
        // so have to increase the offset by 0x10 to account for that
        int pos = context.Version <= 0xb ? 0x10 : 0x0;
        
        SendChildMessages = context.ReadInt32(pos + 0xd4);
        RespawnTime = context.ReadFloat(pos + 0xd8);
        BoxWidth = context.ReadFloat(pos + 0xdc);
        BoxHeight = context.ReadFloat(pos + 0xe0);
        BoxDepth = context.ReadFloat(pos + 0xe4);
        ModelOffset = context.ReadFloat3(pos + 0xf0);
    }
    
    /// <inheritdoc />
    public override void Save(ResourceSaveContext context, ISaveBuffer buffer)
    {
        base.Save(context, buffer);

        context.WriteInt32(buffer, SendChildMessages, 0xd4);
        context.WriteFloat(buffer, RespawnTime, 0xd8);
        context.WriteFloat(buffer, BoxWidth, 0xdc);
        context.WriteFloat(buffer, BoxHeight, 0xe0);
        context.WriteFloat(buffer, BoxDepth, 0xe4);
        context.WriteFloat3(buffer, ModelOffset, 0xf0);
        
        // ???
        context.WriteFloat(buffer, 0.4f, 0xec);
    }

    /// <inheritdoc />
    public override int GetSizeForSerialization(SlPlatform platform, int version) => 0x100;
}