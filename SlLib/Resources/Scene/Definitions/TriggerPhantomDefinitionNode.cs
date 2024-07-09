using SlLib.Resources.Database;
using SlLib.Serialization;

namespace SlLib.Resources.Scene.Definitions;

public class TriggerPhantomDefinitionNode : SeDefinitionTransformNode
{
    public float WidthRadius;
    public float Height;
    public float Depth;
    public int Shape;
    public int SendChildMessages;

    /// <inheritdoc />
    public override void Load(ResourceLoadContext context)
    {
        base.Load(context);
        
        // old versions of TriggerPhantomInstanceNode extended SeInstanceEntityNode
        // so have to increase the offset by 0x10 to account for that
        int pos = context.Version <= 0xb ? 0x10 : 0x0;

        WidthRadius = context.ReadFloat(pos + 0xd0);
        Height = context.ReadFloat(pos + 0xd4);
        Depth = context.ReadFloat(pos + 0xd8);
        Shape = context.ReadInt32(pos + 0xdc);
        SendChildMessages = context.ReadInt32(pos + 0xe0);
    }

    /// <inheritdoc />
    public override void Save(ResourceSaveContext context, ISaveBuffer buffer)
    {
        base.Save(context, buffer);

        context.WriteFloat(buffer, WidthRadius, 0xd0);
        context.WriteFloat(buffer, Height, 0xd4);
        context.WriteFloat(buffer, Depth, 0xd8);
        context.WriteInt32(buffer, Shape, 0xdc);
        context.WriteInt32(buffer, SendChildMessages, 0xe0);
    }

    /// <inheritdoc />
    public override int GetSizeForSerialization(SlPlatform platform, int version) => 0xf0;
}

// public enum ShapeEnumSet
// {
//     Sphere = 0,
//     Box = 1,
//     CylinderX = 2,
//     CylinderY = 3,
//     CylinderZ = 4,
//
// }