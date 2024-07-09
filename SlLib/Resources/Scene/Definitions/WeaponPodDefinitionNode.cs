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
        context.Position = LoadInternal(context, context.Position);
        
        SendChildMessages = context.ReadInt32(0xd4);
        RespawnTime = context.ReadFloat(0xd8);
        BoxWidth = context.ReadFloat(0xdc);
        BoxHeight = context.ReadFloat(0xe0);
        BoxDepth = context.ReadFloat(0xe4);
        ModelOffset = context.ReadFloat3(0xf0);
    }
}