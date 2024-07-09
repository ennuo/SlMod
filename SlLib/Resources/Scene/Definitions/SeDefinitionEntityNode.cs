using SlLib.Resources.Database;
using SlLib.Serialization;

namespace SlLib.Resources.Scene.Definitions;

// SE_ENTITY_
public class SeDefinitionEntityNode : SeDefinitionTransformNode
{
    public override string Prefix => "SE_ENTITY_";
    public override string Extension => ".model";
    
    /// <summary>
    ///     The model associated with this entity.
    /// </summary>
    public SlResPtr<SlModel> Model = new();
    
    /// <summary>
    ///     Loads this definition node from a buffer.
    /// </summary>
    /// <param name="context">The current load context</param>
    /// <param name="offset">The offset in the buffer to laod from</param>
    /// <returns>The offset of the next class base</returns>
    protected override int LoadInternal(ResourceLoadContext context, int offset)
    {
        offset = base.LoadInternal(context, offset);
        
        Model = context.LoadResource<SlModel>(Uid);
        
        // lod flags
        
        return offset + 0x10;
    }
    
    /// <inheritdoc />
    public override int GetSizeForSerialization(SlPlatform platform, int version) => 0xe0;
}