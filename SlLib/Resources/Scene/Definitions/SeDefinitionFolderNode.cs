using SlLib.Resources.Database;
using SlLib.Serialization;

namespace SlLib.Resources.Scene.Definitions;

public class SeDefinitionFolderNode : SeDefinitionTransformNode, IResourceSerializable
{
    /// <summary>
    ///     Default folder definition to use by the folder manager.
    /// </summary>
    public static SeDefinitionFolderNode Default = new() { UidName = "DefaultFolder" };
    
    /// <inheritdoc />
    public void Load(ResourceLoadContext context)
    {
        context.Position = LoadInternal(context, context.Position);
    }
}