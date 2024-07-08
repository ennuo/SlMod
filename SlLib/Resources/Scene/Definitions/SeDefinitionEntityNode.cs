using SlLib.Resources.Database;
using SlLib.Serialization;

namespace SlLib.Resources.Scene.Definitions;

// SE_ENTITY_
public class SeDefinitionEntityNode : SeDefinitionTransformNode, IResourceSerializable
{
    public override string Prefix => "SE_ENTITY_";
    public override string Extension => ".model";
    
    /// <summary>
    ///     The model associated with this entity.
    /// </summary>
    public SlResPtr<SlModel> Model = new();
    
    /// <inheritdoc />
    public void Load(ResourceLoadContext context)
    {
        context.Position = LoadInternal(context, context.Position);
    }
    
    /// <summary>
    ///     Loads this definition node from a buffer.
    /// </summary>
    /// <param name="context">The current load context</param>
    /// <param name="offset">The offset in the buffer to laod from</param>
    /// <returns>The offset of the next class base</returns>
    protected new int LoadInternal(ResourceLoadContext context, int offset)
    {
        offset = base.LoadInternal(context, offset);
        
        Model = context.LoadResource<SlModel>(Uid);
        
        // lod flags
        
        return offset + 0x10;
    }
}