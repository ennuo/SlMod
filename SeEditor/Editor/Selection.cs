using SlLib.Resources.Scene;

namespace SeEditor.Editor;

public static class Selection
{
    /// <summary>
    ///     The current selected node. 
    /// </summary>
    public static SeGraphNode? ActiveNode { get; set; }

    /// <summary>
    ///     The nodes currently held in the clipboard.
    /// </summary>
    public static List<SeGraphNode> Clipboard { get; set; } = [];
    
}