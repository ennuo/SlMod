using ImGuiNET;
using SeEditor.Graphics.ImGui;
using SlLib.Resources.Scene.Instances;

namespace SeEditor.Editor.Menu;

public static class SeAttributeMenu
{
    public static void Draw(SeInstanceLightNode node)
    {
        if (!ImGui.CollapsingHeader("Light", ImGuiTreeNodeFlags.DefaultOpen)) return;

        ImGui.InputInt(ImGuiHelper.DoLabelPrefix("Light Data Flags"), ref node.LightDataFlags);
        
        // replace with combo
        ImGui.Text(node.LightType.ToString());


        ImGui.DragFloat(ImGuiHelper.DoLabelPrefix("Specular Multiplier"), ref node.SpecularMultiplier);
        ImGui.DragFloat(ImGuiHelper.DoLabelPrefix("Intensity Multiplier"), ref node.IntensityMultiplier);
        ImGui.ColorEdit3(ImGuiHelper.DoLabelPrefix("Color"), ref node.Color);
        
        if (node.LightType == SeInstanceLightNode.SeLightType.Point)
        {
            ImGui.DragFloat(ImGuiHelper.DoLabelPrefix("Inner Radius"), ref node.InnerRadius);
            ImGui.DragFloat(ImGuiHelper.DoLabelPrefix("Outer Radius"), ref node.OuterRadius);
            ImGui.DragFloat(ImGuiHelper.DoLabelPrefix("Falloff"), ref node.Falloff);
        }
        
        
        if (node.LightType == SeInstanceLightNode.SeLightType.Spot)
        {
            ImGui.DragFloat(ImGuiHelper.DoLabelPrefix("Inner Cone Angle"), ref node.InnerConeAngle);
            ImGui.DragFloat(ImGuiHelper.DoLabelPrefix("Outer Cone Angle"), ref node.OuterConeAngle);
        }
    }
}