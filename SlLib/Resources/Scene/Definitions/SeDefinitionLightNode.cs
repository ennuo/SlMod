using SlLib.Resources.Database;
using SlLib.Serialization;

namespace SlLib.Resources.Scene.Definitions;

// SE_LIGHT_
public class SeDefinitionLightNode : SeDefinitionTransformNode
{
    /// <inheritdoc />
    public override void Load(ResourceLoadContext context)
    {
        base.Load(context);
        
        // todo: put data here, no idea if its even used so
        // SeLightData
    }
    
    /// <inheritdoc />
    public override int GetSizeForSerialization(SlPlatform platform, int version) => 0x100;
}