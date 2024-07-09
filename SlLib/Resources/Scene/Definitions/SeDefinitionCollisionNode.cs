using SlLib.Resources.Database;
using SlLib.Serialization;

namespace SlLib.Resources.Scene.Definitions;

// hidden in manager list
// SE_COLLISION_
public class SeDefinitionCollisionNode : SeDefinitionTransformNode
{
    /// <inheritdoc />
    public override void Save(ResourceSaveContext context, ISaveBuffer buffer)
    {
        base.Save(context, buffer);
        context.WriteInt32(buffer, 0xBADF00D, 0xd8);
        context.WriteInt32(buffer, 0xBADF00D, 0xdc);
    }
    
    /// <inheritdoc />
    public override int GetSizeForSerialization(SlPlatform platform, int version) => 0xe0;
}