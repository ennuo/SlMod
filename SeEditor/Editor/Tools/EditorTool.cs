using SlLib.Resources.Scene;

namespace SeEditor.Editor.Tools;

public abstract class EditorTool
{
    /// <summary>
    ///     The node this tool is currently editing, if any.
    /// </summary>
    public SeGraphNode? Target;
    
    /// <summary>
    ///     Called once per frame to update the scene.
    /// </summary>
    public abstract void OnRender();
    
    /// <summary>
    ///     Called once per render frame to update the IMGUI layout.
    /// </summary>
    public virtual void OnGUI()
    {
        
    }
}