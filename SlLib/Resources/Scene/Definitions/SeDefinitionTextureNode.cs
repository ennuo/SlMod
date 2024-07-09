using SlLib.Resources.Database;
using SlLib.Serialization;

namespace SlLib.Resources.Scene.Definitions;

public class SeDefinitionTextureNode : SeDefinitionNode
{
    /// <summary>
    ///     The texture associated with this node.
    /// </summary>
    public SlResPtr<SlTexture> Texture = new();
    
    /// <inheritdoc />
    public override void Load(ResourceLoadContext context)
    {
        base.Load(context);
        Texture = context.LoadResource<SlTexture>(Uid);
    }
    
    /// <inheritdoc />
    public override int GetSizeForSerialization(SlPlatform platform, int version) => 0x90;
}