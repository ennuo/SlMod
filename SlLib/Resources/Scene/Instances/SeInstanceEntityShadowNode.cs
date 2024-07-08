﻿using SlLib.Serialization;

namespace SlLib.Resources.Scene.Instances;

public class SeInstanceEntityShadowNode : SeInstanceEntityNode, IResourceSerializable
{
    /// <inheritdoc />
    public new void Load(ResourceLoadContext context)
    {
        context.Position = LoadInternal(context, context.Position);
    }
}