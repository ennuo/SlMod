namespace SeEditor.Editor.Panel;

/// <summary>
///     Represents a panel in the editor.
/// </summary>
public interface IEditorPanel
{
    /// <summary>
    ///     Called every render frame.
    /// </summary>
    void OnImGuiRender();
    
    /// <summary>
    ///     Called every time a selection change occurs in the editor.
    /// </summary>
    void OnSelectionChanged()
    {
            
    }
    
    /// <summary>
    ///     Called to update panel logic.
    /// </summary>
    void OnUpdate()
    {
        
    }
}