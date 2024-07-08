using SlLib.Resources.Database;
using SlLib.Serialization;

namespace SlLib.Resources.Scene.Definitions;

public class SeDefinitionTextureNode : SeDefinitionNode, IResourceSerializable
{
    /// <summary>
    ///     The texture associated with this node.
    /// </summary>
    public SlResPtr<SlTexture> Texture = new();
    
    /// <inheritdoc />
    public void Load(ResourceLoadContext context)
    {
        context.Position = LoadInternal(context, context.Position);
        Texture = context.LoadResource<SlTexture>(Uid);
    }
}