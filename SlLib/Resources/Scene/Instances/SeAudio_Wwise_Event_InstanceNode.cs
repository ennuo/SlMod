using SlLib.Resources.Database;
using SlLib.Serialization;

namespace SlLib.Resources.Scene.Instances;

// ReSharper disable once InconsistentNaming
public class SeAudio_Wwise_Event_InstanceNode : SeInstanceTransformNode
{
    public bool ApplyViewCulling;
    public float ViewCullingThreshold;
    public float AttenuationScaleOverride;
    
    /// <inheritdoc />
    public override void Load(ResourceLoadContext context)
    {
        base.Load(context);

        ApplyViewCulling = context.ReadBoolean(0x174, wide: true);
        ViewCullingThreshold = context.ReadFloat(0x178);
        AttenuationScaleOverride = context.ReadFloat(0x184);
    }
    
    /// <inheritdoc />
    public override void Save(ResourceSaveContext context, ISaveBuffer buffer)
    {
        base.Save(context, buffer);
        
        context.WriteBoolean(buffer, ApplyViewCulling, 0x174, wide: true);
        context.WriteFloat(buffer, ViewCullingThreshold, 0x178);
        context.WriteFloat(buffer, AttenuationScaleOverride, 0x184);
        
        // Might just be the attentuation scale, or something else entirely,
        // not an attribute so just leave it to 1.0
        context.WriteFloat(buffer, 1.0f, 0x188); 
    }
    
    /// <inheritdoc />
    public override int GetSizeForSerialization(SlPlatform platform, int version) => 0x190;
}