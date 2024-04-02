﻿namespace SlLib.Resources.Database;

public enum SlResourceType
{
    Invalid = -1,

    AfterburnerWaterShaderDefinitionNode = 0x0d666f38,
    CameoObjectDefinitionNode = 0x0b36c6c6,
    CameoObjectInstanceNode = 0x038b1494,
    CameraShakeParamsDef = 0x0662c7a4,
    CameraShakeParamsInstance = 0x0297e028,
    CatchupRespotDefinitionNode = 0x004c7f0e,
    CatchupRespotInstanceNode = 0x0b02330c,
    DistanceRespotDefinitionNode = 0x020a69dd,
    DriftZoneDefinitionNode = 0x05c0ce08,
    DynamicObjectDefinitionNode = 0x010c3cdc,
    DynamicObjectInstanceNode = 0x02f981b0,
    JumboMapDefinitionNode = 0x0106e70a,
    LavaShader1DefinitionNode = 0x0a3c9040,
    NavPathDefinitionNode = 0x0f6192be,
    NavPathNodeDefinitionNode = 0x02f557e8,
    ScreenPortalDefNode = 0x06f17efc,
    ScreenSplashDefNode = 0x0541997f,
    SeAudioWwiseEnvironmentDefinitionNode = 0x0333d666,
    SeAudioWwiseEnvironmentInstanceNode = 0x01e5d2a5,
    SeAudioWwiseEventDefinitionNode = 0x0225fee4,
    SeAudioWwiseEventInstanceNode = 0x08474f62,
    SeAudioWwiseEventVolumeDefinitionNode = 0x086bb7a5,
    SeAudioWwiseEventVolumeInstanceNode = 0x039ece3f,
    SeDefinitionAnimationStreamNode = 0x09830022,
    SeDefinitionAnimatorLocatorNode = 0x0cc38861,
    SeDefinitionAnimatorNode = 0x07e3f80a,
    SeDefinitionAreaNode = 0x06ee508d,
    SeDefinitionCameraNode = 0x095ed9fa,
    SeDefinitionCollisionNode = 0x004d9bcb,
    SeDefinitionEntityNode = 0x0941231b,
    SeDefinitionEntityShadowNode = 0x0c0bc366,
    SeDefinitionFolderNode = 0x04adad02,
    SeDefinitionLensFlareNode = 0x05e85e3d,
    SeDefinitionLightNode = 0x02369c75,
    SeDefinitionLocatorNode = 0x0b692bdd,
    SeDefinitionParticleAffectorBasicNode = 0x06da5672,
    SeDefinitionParticleAffectorTurbulanceNode = 0x0b34cfe1,
    SeDefinitionParticleEmitterNode = 0x0454183d,
    SeDefinitionParticleReferenceNode = 0x0611c148,
    SeDefinitionParticleStyleNode = 0x0e090cf7,
    SeDefinitionParticleSystemNode = 0x0ec545d9,
    SeDefinitionRampNodeVector4 = 0x099ec06e,
    SeDefinitionShadowNode = 0x018b55a9,
    SeDefinitionSkyNode = 0x0a7bde7d,
    SeDefinitionSplineNode = 0x06f74eee,
    SeDefinitionTextureNode = 0x007b0aae,
    SeDefinitionTimeLineEventBaseNode = 0x057a9d2f,
    SeDefinitionTimeLineEventNode = 0x04a4d87d,
    SeDefinitionTimeLineFlowControlEvent = 0x0abf471c,
    SeDefinitionTimelineNode = 0x00005252,
    SeDefinitionTimeLinePerformAction = 0x0b0f80bc,
    SeFogDefinitionNode = 0x03afe8d6,
    SeFogInstanceNode = 0x0ebcd905,
    SeGiDefinitionCameraVolume = 0x03400f7c,
    SeGiDefinitionVolume = 0x03f048a4,
    SeGiInstanceCameraVolume = 0x0ee4e33c,
    SeGiInstanceVolume = 0x08178833,
    SeInstanceAnimationStreamNode = 0x01b63e0c,
    SeInstanceAnimatorLocatorNode = 0x0c42078c,
    SeInstanceAnimatorNode = 0x0cec5b91,
    SeInstanceAreaNode = 0x0941df26,
    SeInstanceCameraNode = 0x040a85de,
    SeInstanceCollisionNode = 0x08fe4509,
    SeInstanceEntityDecalsNode = 0x04766ead,
    SeInstanceEntityNode = 0x0e874eed,
    SeInstanceEntityShadowNode = 0x0768a066,
    SeInstanceFolderNode = 0x0efd30b1,
    SeInstanceLightNode = 0x028a6c56,
    SeInstanceLocatorNode = 0x0e8f4954,
    SeInstanceParticleAffectorBasicNode = 0x0d752cf2,
    SeInstanceParticleAffectorTurbulanceNode = 0x056bf00f,
    SeInstanceParticleEmitterNode = 0x07c9d2f5,
    SeInstanceParticleReferenceNode = 0x01fbc74c,
    SeInstanceParticleSystemNode = 0x0d3ee0e1,
    SeInstanceShadowNode = 0x0c8d6b23,
    SeInstanceSkyNode = 0x09f5f7e8,
    SeInstanceSplineNode = 0x0906e993,
    SeInstanceTimeLineEventNode = 0x02eda6b7,
    SeInstanceTimeLineFlowControlEvent = 0x0f8e07cd,
    SeInstanceTimelineNode = 0x0eca1997,
    SeInstanceTimeLinePerformAction = 0x045595c2,
    SeProject = 0x02a8c3a5,
    SeProjectEnd = 0x01728227,
    SeToneMappingRefDefinitionNode = 0x05eb8a6e,
    SeToneMappingRefInstanceNode = 0x0b834868,
    SeWorkspace = 0x04076b1a,
    SeWorkspaceEnd = 0x0cd5ce82,
    SlAnim = 0x0a8f3129,
    SlConstantBufferDesc = 0x0e905cf4,
    SlMaterial2 = 0x0109c979,
    SlModel = 0x031912ca,
    SlPvsData = 0x064ac36b,
    SlResourceCollision = 0x0bb762d5,
    SlShader = 0x0e9b7a77,
    SlSkeleton = 0x00b85a11,
    SlTexture = 0x0c82d1d6,
    SurfaceWaveSwapDefinitionNode = 0x0dd5bc26,
    SurfaceWaveSwapInstanceNode = 0x0d5e8560,
    TidalWaveGenDefinitionNode = 0x005d5345,
    TidalWaveGenInstanceNode = 0x0eb3d8df,
    TrafficManagerDefinitionNode = 0x01e9774e,
    TrafficManagerInstanceNode = 0x04549a22,
    TriggerPhantomDefinitionNode = 0x0377fe27,
    TriggerPhantomInstanceNode = 0x01947262,
    Water13DefNode = 0x0a2fb64c,
    Water13InstanceNode = 0x0a0bfb05,
    Water13Renderable = 0x007ef20d,
    Water13Simulation = 0x0ebcf239,
    Water13SurfaceWavesDefNode = 0x0b921225,
    WaterShader2DefinitionNode = 0x0ccc71c6,
    WaterShader4DefinitionNode = 0x02c6a9ab,
    WaterTrailDefinitionNode = 0x05ad3d67,
    WaveGenDefinitionNode = 0x03b1d580,
    WaveGenInstanceNode = 0x0998cff7,
    WeaponPodDefinitionNode = 0x0c39b215,
    WeaponPodInstanceNode = 0x01e95dc8
}