﻿using SlLib.Resources.Database;

namespace SlLib.Resources.Scene.Definitions;

public class SeDefinitionSceneNode : SeDefinitionTransformNode
{
    /// <inheritdoc />
    public override int GetSizeForSerialization(SlPlatform platform, int version) => 0xd0;
}