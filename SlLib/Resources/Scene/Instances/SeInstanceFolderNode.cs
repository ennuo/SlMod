using SlLib.Resources.Database;

namespace SlLib.Resources.Scene.Instances;

public class SeInstanceFolderNode : SeInstanceTransformNode
{
    /// <inheritdoc />
    public override int GetSizeForSerialization(SlPlatform platform, int version) => 0x160;
}