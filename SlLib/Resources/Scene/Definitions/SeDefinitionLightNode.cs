using SlLib.Resources.Database;
using SlLib.Serialization;

namespace SlLib.Resources.Scene.Definitions;

// SE_LIGHT_
public class SeDefinitionLightNode : SeDefinitionTransformNode
{
    /// <inheritdoc />
    public override void Load(ResourceLoadContext context)
    {
        context.Position = LoadInternal(context, context.Position);
        
        // SeLightData
    }
    
    /// <inheritdoc />
    public override int GetSizeForSerialization(SlPlatform platform, int version) => 0x100;
}