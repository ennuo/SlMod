using ImGuiNET;
using SeEditor.Editor.Menu;
using SeEditor.Graphics.ImGui;
using SlLib.Resources.Scene;

namespace SeEditor.Editor.Panel;

/// <summary>
///     A menu to view and edit the properties of a node in the scene.
/// </summary>
public class InspectorPanel : IEditorPanel
{
    /// <summary>
    ///     The node that is currently being inspected.
    /// </summary>
    private SeNodeBase? _selected;
    
    public void OnImGuiRender()
    {
        if (_selected == null) return;
        
        bool isActive = (_selected.BaseFlags & 0x1) != 0;
        bool isVisible = (_selected.BaseFlags & 0x2) != 0;
        
        ImGui.Checkbox("###BaseNodeEnabledToggle", ref isActive);
        ImGui.SameLine();
        
        string name = _selected.ShortName;
        ImGui.PushItemWidth(-1.0f);
        ImGui.InputText("##BaseNodeName", ref name, 255);
        ImGui.PopItemWidth();

        ImGui.InputText(ImGuiHelper.DoLabelPrefix("Tag"), ref _selected.Tag, 255);

        ImGuiHelper.DoLabelPrefix("Type");
        ImGui.Text(_selected.Debug_ResourceType.ToString());

        ImGui.Checkbox(ImGuiHelper.DoLabelPrefix("Visible"), ref isVisible);
            
        _selected.BaseFlags = (_selected.BaseFlags & ~1) | (isActive ? 1 : 0);
        _selected.BaseFlags = (_selected.BaseFlags & ~2) | (isVisible ? 2 : 0);
        
        NodeAttributesMenu.Draw(_selected);
    }

    public void OnSelectionChanged()
    {
        _selected = Selection.ActiveNode;
    }
}