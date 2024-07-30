using SlLib.Resources.Database;
using SlLib.Resources.Scene;
using SlLib.SumoTool.Siff;

namespace SeEditor.Editor;

public class SceneManager
{
    public static SceneManager Instance = new();
    
    public SceneCamera Camera = new();
    public SlResourceDatabase Database = new(SlPlatform.Win32);
    public List<SeGraphNode> Selected = [];
    public Navigation? Navigation;
    public List<SeGraphNode> Clipboard = [];
    
    public bool RenderNavigationOnly;
    public bool DisableRendering;
}