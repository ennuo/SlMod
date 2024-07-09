using System.Numerics;
using SlLib.Resources.Database;
using SlLib.Serialization;

namespace SlLib.Resources.Scene.Instances;

public class SeInstanceCameraNode : SeInstanceTransformNode, IResourceSerializable
{
    public Matrix4x4 ViewMatrix;
    public Matrix4x4 ProjectionMatrix;
    public Matrix4x4 ViewProjectionMatrix;
    
    /// <inheritdoc />
    public override void Load(ResourceLoadContext context)
    {
        context.Position = LoadInternal(context, context.Position);
        ViewMatrix = context.ReadMatrix();
        ProjectionMatrix = context.ReadMatrix();
        ViewProjectionMatrix = context.ReadMatrix();
    }
}