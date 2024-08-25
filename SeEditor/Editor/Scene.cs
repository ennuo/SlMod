using SlLib.Resources.Database;
using SlLib.Resources.Scene;
using SlLib.Resources.Scene.Instances;
using SlLib.Serialization;
using SlLib.SumoTool.Siff;

namespace SeEditor.Editor;

public class Scene
{
    /// <summary>
    ///     The editor camera for this scene.
    /// </summary>
    public SceneCamera Camera = new();

    /// <summary>
    ///     The database that contains this scene's resources and nodes.
    /// </summary>
    public SlResourceDatabase Database = new(SlPlatform.Win32);
    
    // /// <summary>
    // ///     The root node of this scene.
    // /// </summary>
    // public SeGraphNode Root = new SeInstanceSceneNode { UidName = "DefaultScene" };
    //
    // /// <summary>
    // ///     Flattened list of all nodes in this scene.
    // /// </summary>
    // public List<SeGraphNode> Nodes = [];
    //
    // /// <summary>
    // ///     Flattened list of all instances in this scene.
    // /// </summary>
    // public List<SeInstanceNode> Instances = [];
    //
    // /// <summary>
    // ///     Flattened list of all definitions in this scene.
    // /// </summary>
    // public List<SeDefinitionNode> Definitions = [];
    //
    // /// <summary>
    // ///     Flattened list of all resources in this scene.
    // /// </summary>
    // public List<ISumoResource> Resources = [];

    /// <summary>
    ///     The navigation instance loaded in this scene.
    /// </summary>
    public Navigation? Navigation;
    
    /// <summary>
    ///     The name of the source file this scene was loaded from.
    /// </summary>
    public string SourceFileName = string.Empty;
}