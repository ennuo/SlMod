using SlLib.Resources.Database;

namespace SlLib.Resources.Scene.Instances;

// ReSharper disable once InconsistentNaming
public class SeAudio_Wwise_Listener_InstanceNode : SeInstanceTransformNode
{
    /// <inheritdoc />
    public override int GetSizeForSerialization(SlPlatform platform, int version) => 0x1a0;
}