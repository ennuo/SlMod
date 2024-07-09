using SlLib.Serialization;

namespace SlLib.Resources.Scene.Definitions;

public class TriggerPhantomDefinitionNode : SeDefinitionTransformNode
{
    public float WidthRadius = 1.0f;
    public float Height = 1.0f;
    public float Depth = 1.0f;
    public bool SendChildMessages;
    
    // Sphere, Box, CylinderX, CylinderY, CylinderZ
    public int Shape;
    
    /// <inheritdoc />
    public override void Load(ResourceLoadContext context)
    {
        context.Position = LoadInternal(context, context.Position);
        WidthRadius = context.ReadFloat(0xd0);
        Height = context.ReadFloat(0xd4);
        Depth = context.ReadFloat(0xd8);
        Shape = context.ReadInt32(0xdc);
        SendChildMessages = context.ReadBoolean(0xe0, wide: true);
    }
}