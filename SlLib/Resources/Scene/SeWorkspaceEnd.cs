﻿using SlLib.Resources.Database;

namespace SlLib.Resources.Scene;

public class SeWorkspaceEnd : SeDefinitionNode
{
    /// <inheritdoc />
    public override int GetSizeForSerialization(SlPlatform platform, int version) => 0x80;
}