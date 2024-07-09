using SlLib.Resources.Database;

namespace SlLib.Resources.Scene.Instances;

public class SeInstanceSceneNode : SeInstanceTransformNode
{
    /// <summary>
    ///     Default folder definition to use by the folder manager.
    /// </summary>
    public static SeInstanceSceneNode Default = new() { UidName = "DefaultScene" };
    
    /// <inheritdoc />
    public override int GetSizeForSerialization(SlPlatform platform, int version) => 0x170;
}