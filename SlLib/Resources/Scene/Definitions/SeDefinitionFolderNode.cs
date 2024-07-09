using SlLib.Resources.Database;
using SlLib.Serialization;

namespace SlLib.Resources.Scene.Definitions;

public class SeDefinitionFolderNode : SeDefinitionTransformNode
{
    /// <summary>
    ///     Default folder definition to use by the folder manager.
    /// </summary>
    public static readonly SeDefinitionFolderNode Default = new() { UidName = "DefaultFolder" };
    
    /// <inheritdoc />
    public override int GetSizeForSerialization(SlPlatform platform, int version) => 0xd0;
}