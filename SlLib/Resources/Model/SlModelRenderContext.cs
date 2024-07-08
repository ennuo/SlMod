using System.Numerics;

namespace SlLib.Resources.Model;

public class SlModelRenderContext
{
    /// <summary>
    ///     Instances populated from the scene graph
    /// </summary>
    public List<SlModelInstanceData> SceneGraphInstances = [];
    
    /// <summary>
    ///     Instances generated from render commands.
    /// </summary>
    public List<SlModelInstanceData> RenderContextInstances = [];
    
    /// <summary>
    ///     Instance set that we're currently rendering.
    /// </summary>
    public List<SlModelInstanceData> Instances = [];
    
    public SlMaterial2? Material;
    public List<Matrix4x4> BindMatrices = [];
    
    public Matrix4x4 EntityWorldMatrix;
    
    public bool NextSegmentIsVisible = true;
    public bool Wireframe = false;
}