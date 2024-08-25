using System.Numerics;

namespace SlLib.Resources.Model;

public class SlModelRenderContext
{
    /// <summary>
    ///     Instances populated from the scene graph
    /// </summary>
    public List<SlModelInstanceData> SceneGraphInstances = new(256);
    
    /// <summary>
    ///     Instances generated from render commands.
    /// </summary>
    public List<SlModelInstanceData> RenderContextInstances = new(256);
    
    /// <summary>
    ///     Instance set that we're currently rendering.
    /// </summary>
    public List<SlModelInstanceData> Instances = new(256);
    
    public SlMaterial2? Material;
    public Matrix4x4 EntityWorldMatrix;
    
    public bool NextSegmentIsVisible = true;
    public bool Wireframe = false;
    
    public void Prepare(bool wireframe = false)
    {
        NextSegmentIsVisible = true;
        Wireframe = wireframe;
        SceneGraphInstances.Clear();
        RenderContextInstances.Clear();
        Instances.Clear();
    }
}