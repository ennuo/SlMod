using ImGuiNET;
using SeEditor.Menu;
using SlLib.Resources.Scene.Instances;

namespace SeEditor;

public static class SeAttributeMenu
{
    public static void Draw(TriggerPhantomInstanceNode node)
    {
        if (true)
        {
            NodeAttributesMenu.Draw(node);
            return;
        }
        
        
        if (!ImGui.CollapsingHeader("Phantom", ImGuiTreeNodeFlags.DefaultOpen)) return;


        var names = Enum.GetNames<TriggerPhantomInstanceNode.MessageType>();
        var values = (int[])Enum.GetValuesAsUnderlyingType<TriggerPhantomInstanceNode.MessageType>();
        
        for (int i = 0; i < 8; ++i)
        {
            string name = "-Empty-";
            if (node.LinkedNode[i] != null)
                name = node.LinkedNode[i].ShortName;
            
            if (ImGui.BeginCombo("##combo_node_" + i, name))
            {
                ImGui.EndCombo();
            }
            
            ImGui.SameLine();
                
            if (ImGui.BeginCombo("##combo_" + i, node.MessageText[i].ToString()))
            {
                for (int j = 0; j < names.Length; ++j)
                {
                    bool selected = (int)node.MessageText[i] == values[i];
                    if (ImGui.Selectable(names[i], selected))
                        node.MessageText[i] = (TriggerPhantomInstanceNode.MessageType)values[i];
                    if (selected)
                        ImGui.SetItemDefaultFocus();
                }
                
                ImGui.EndCombo();
            }
            
            
        }

        ImGui.Checkbox(ImGuiHelper.DoLabelPrefix("Lap 1"), ref node.Lap1);
        ImGui.Checkbox(ImGuiHelper.DoLabelPrefix("Lap 2"), ref node.Lap2);
        ImGui.Checkbox(ImGuiHelper.DoLabelPrefix("Lap 3"), ref node.Lap3);
        ImGui.Checkbox(ImGuiHelper.DoLabelPrefix("Lap 4"), ref node.Lap4);

        ImGui.Combo(ImGuiHelper.DoLabelPrefix("Leader"), ref node.Leader,
            Enum.GetNames<TriggerPhantomInstanceNode.LeaderType>(), 5);
        
        ImGui.CheckboxFlags(ImGuiHelper.DoLabelPrefix("Trigger Car"), ref node.Flags, (int)TriggerPhantomInstanceNode.VehicleFlags.TriggerCar);
        ImGui.CheckboxFlags(ImGuiHelper.DoLabelPrefix("Trigger Boat"), ref node.Flags, (int)TriggerPhantomInstanceNode.VehicleFlags.TriggerBoat);
        ImGui.CheckboxFlags(ImGuiHelper.DoLabelPrefix("Trigger Plane"), ref node.Flags, (int)TriggerPhantomInstanceNode.VehicleFlags.TriggerPlane);
        ImGui.CheckboxFlags(ImGuiHelper.DoLabelPrefix("Trigger Weapon"), ref node.Flags, (int)TriggerPhantomInstanceNode.VehicleFlags.TriggerWeapon);
        ImGui.CheckboxFlags(ImGuiHelper.DoLabelPrefix("Trigger On-load"), ref node.Flags, (int)TriggerPhantomInstanceNode.VehicleFlags.TriggerOnLoad);
        ImGui.CheckboxFlags(ImGuiHelper.DoLabelPrefix("Trigger Prediction"), ref node.Flags, (int)TriggerPhantomInstanceNode.VehicleFlags.TriggerPrediction);
        ImGui.CheckboxFlags(ImGuiHelper.DoLabelPrefix("Trigger Has Jumbomap"), ref node.Flags, (int)TriggerPhantomInstanceNode.VehicleFlags.TriggerHasJumboMap);
        ImGui.CheckboxFlags(ImGuiHelper.DoLabelPrefix("Trigger Once per Racer"), ref node.Flags, (int)TriggerPhantomInstanceNode.VehicleFlags.TriggerOncePerRacer);
        
        ImGui.InputInt(ImGuiHelper.DoLabelPrefix("Num Activations"), ref node.NumActivations);
        ImGui.DragFloat(ImGuiHelper.DoLabelPrefix("Prediction Time"), ref node.PredictionTime);
    }
    
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