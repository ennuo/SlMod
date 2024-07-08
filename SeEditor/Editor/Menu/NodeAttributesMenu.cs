using System.Numerics;
using ImGuiNET;
using SeEditor.Utilities;
using SlLib.Enums;
using SlLib.IO;
using SlLib.Resources;
using SlLib.Resources.Scene.Definitions;
using SlLib.Resources.Scene.Instances;

namespace SeEditor.Menu;

public class NodeAttributesMenu
{
    private static void StartNewLine()
    {
        ImGui.TableNextRow();
        ImGui.TableNextColumn();
    }

    private static void DoLabel(string text)
    {
        ImGui.AlignTextToFramePadding();
        ImGui.Text(text);
        ImGui.TableNextColumn();
    }
    
    private static void DrawHeader(string text)
    {
        StartNewLine();
        ImGuiHelper.DoBoldText(text);
    }

    private static void DrawCheckbox(string text, ref bool value)
    {
        StartNewLine();
        ImGui.PushID(text);
        DoLabel(text);
        
        ImGui.SetNextItemWidth(-1.0f);
        ImGui.Checkbox("##value", ref value);
        
        ImGui.PopID();
    }
    
    private static void DrawCheckboxFlags(string text, ref int value, int flag)
    {
        StartNewLine();
        ImGui.PushID(text);
        DoLabel(text);
        
        ImGui.SetNextItemWidth(-1.0f);
        ImGui.CheckboxFlags("##value", ref value, flag);
        
        ImGui.PopID();
    }

    private static void DrawInputInt(string text, ref int value)
    {
        StartNewLine();
        ImGui.PushID(text);
        DoLabel(text);
        
        ImGui.SetNextItemWidth(-1.0f);
        ImGui.InputInt("##value", ref value);
        
        ImGui.PopID();
    }

    private static void DrawDragFloat(string text, ref float value)
    {
        StartNewLine();
        ImGui.PushID(text);
        DoLabel(text);
        
        ImGui.SetNextItemWidth(-1.0f);
        ImGui.DragFloat("##value", ref value);
        
        ImGui.PopID();
    }
    
    private static bool DrawColor3(string text, ref Vector3 value)
    {
        StartNewLine();
        ImGui.PushID(text);
        DoLabel(text);
        
        ImGui.SetNextItemWidth(-1.0f);
        bool input = ImGui.ColorEdit3("##value", ref value);
        
        ImGui.PopID();
        return input;
    }
    
    private static bool DrawDragFloat3(string text, ref Vector3 value)
    {
        StartNewLine();
        ImGui.PushID(text);
        DoLabel(text);
        
        ImGui.SetNextItemWidth(-1.0f);
        bool input = ImGui.DragFloat3("##value", ref value);
        
        ImGui.PopID();
        return input;
    }

    private static void DrawIndexedEnum<T>(string text, ref int value) where T : Enum
    {
        StartNewLine();
        ImGui.PushID(text);
        DoLabel(text);

        ImGui.SetNextItemWidth(-1.0f);
        ImGuiHelper.DoIndexedEnum<T>("##value", ref value);
        ImGui.PopID();
    }

    private static void DrawHashedEnum<T>(string text, ref T value) where T : Enum
    {
        StartNewLine();
        ImGui.PushID(text);
        DoLabel(text);
        ImGui.SetNextItemWidth(-1.0f);
        ImGuiHelper.DoHashedEnum("##value", ref value);
        ImGui.PopID();
    }
    
    private static bool StartPropertyTable(string name)
    {
        if (!ImGui.CollapsingHeader(name, ImGuiTreeNodeFlags.DefaultOpen)) return false;
        
        
        
        ImGui.PushStyleVar(ImGuiStyleVar.FramePadding, new Vector2(2, 2));
        return ImGui.BeginTable("##propertytable", 2, ImGuiTableFlags.PadOuterX);
    }

    public static void Draw(WeaponPodInstanceNode node)
    {
        if (StartPropertyTable("Weapon Pod"))
        {
            DrawColor3("Pod Color", ref node.PodColor);
            DrawHashedEnum("Message", ref node.Message);
            DrawInputInt("Allocation Count", ref node.AllocationCount);
            
            ImGui.EndTable();
        }
        
        ImGui.PopStyleVar();
    }

    public static void Draw(SeInstanceTransformNode node)
    {
        if (StartPropertyTable("Transform"))
        {
            Vector3 rotation = MathUtils.ToEulerAngles(node.Rotation);
            
            
            DrawHeader("Local Transform");
            ImGui.Indent();
            if (DrawDragFloat3("Rotation", ref rotation))
                node.Rotation = MathUtils.FromEulerAngles(rotation);
            DrawDragFloat3("Translation", ref node.Translation);
            DrawDragFloat3("Scale", ref node.Scale);
            ImGui.Unindent();
            
            DrawCheckbox("Inherit Transforms", ref node.InheritTransforms);
            
            StartNewLine();
            if (ImGui.Button("GOTO!"))
            {
                Matrix4x4.Decompose(node.WorldMatrix, out Vector3 scale, out Quaternion r,
                    out Vector3 translation);


                Vector3 position = translation;
                
                MainWindow.EditorCamera_Position = position;
                MainWindow.EditorCamera_Rotation = Vector3.Zero;
            }
            
            
            ImGui.EndTable();
        }
        
        ImGui.PopStyleVar();
    }

    public static void Draw(SeInstanceEntityNode node)
    {
        if (StartPropertyTable("Entity"))
        {
            
            DrawInputInt("Render Layer", ref node.RenderLayer);
            
            DrawHeader("Flags");
            ImGui.Indent();
            
            DrawCheckboxFlags("Force Forward Render", ref node.Flags, 1 << 0);
            DrawCheckboxFlags("Shadow Cast", ref node.Flags, 1 << 1);
            
            ImGui.Unindent();
            ImGui.EndTable();
        }
        
        ImGui.PopStyleVar();
    }

    public static void Draw(CatchupRespotInstanceNode node)
    {
        if (StartPropertyTable("Catchup Respot"))
        {
            DrawIndexedEnum<Laps>("Lap", ref node.Lap);
            DrawIndexedEnum<DriveMode>("Drive Mode", ref node.DriveMode);
            
            ImGui.EndTable();
        }
        
        ImGui.PopStyleVar();
    }
    
    public static void Draw(TriggerPhantomInstanceNode node)
    {
        if (StartPropertyTable("Trigger Phantom"))
        {
            DrawHeader("Action Events");
            ImGui.Indent();
            for (int i = 0; i < 8; ++i)
            {
                bool hasNodeLink = node.LinkedNode[i] != null;
                bool hasEvent = node.MessageText[i] != TriggerPhantomInstanceNode.MessageType.Empty;
                
                string text = $"[{i}]";
                StartNewLine();
                ImGui.PushID(text);
                
                DoLabel(text);
                
                ImGui.PushItemWidth(-1.0f);

                string nodeName = hasNodeLink ? node.LinkedNode[i]!.ShortName : "-Empty-";
                if (ImGui.BeginCombo("##message_link_combo", nodeName))
                    ImGui.EndCombo();

                if (ImGui.BeginDragDropTarget())
                {
                 
                    ImGuiPayloadPtr editorNodeDrop = ImGui.AcceptDragDropPayload("EDITOR_NODE");
                    if (editorNodeDrop.IsDelivery())
                    {
                        Console.WriteLine("DROPPED!");
                    }
                    
                    ImGui.EndDragDropTarget();
                }
                
                ImGuiHelper.DoHashedEnum("##message_enum_combo", ref node.MessageText[i]);
                
                ImGui.PopItemWidth();
                ImGui.PopID();
                
                // dont show all nodes if they're empty
                if (!hasEvent) break;
            }
            ImGui.Unindent();
            
            DrawHeader("Lap Masks");
            ImGui.Indent();
            DrawCheckbox("Lap 1", ref node.Lap1);
            DrawCheckbox("Lap 2", ref node.Lap2);
            DrawCheckbox("Lap 3", ref node.Lap3);
            DrawCheckbox("Lap 4", ref node.Lap4);
            ImGui.Unindent();
            
            DrawHeader("Triggers");
            ImGui.Indent();
            DrawCheckboxFlags("Trigger Car", ref node.Flags, (int)TriggerPhantomInstanceNode.VehicleFlags.TriggerCar);
            DrawCheckboxFlags("Trigger Boat", ref node.Flags, (int)TriggerPhantomInstanceNode.VehicleFlags.TriggerBoat);
            DrawCheckboxFlags("Trigger Plane", ref node.Flags, (int)TriggerPhantomInstanceNode.VehicleFlags.TriggerPlane);
            DrawCheckboxFlags("Trigger Weapon", ref node.Flags, (int)TriggerPhantomInstanceNode.VehicleFlags.TriggerWeapon);
            DrawCheckboxFlags("Trigger On-load", ref node.Flags, (int)TriggerPhantomInstanceNode.VehicleFlags.TriggerOnLoad);
            DrawCheckboxFlags("Trigger Prediction", ref node.Flags, (int)TriggerPhantomInstanceNode.VehicleFlags.TriggerPrediction);
            DrawCheckboxFlags("Trigger Has Jumbomap", ref node.Flags, (int)TriggerPhantomInstanceNode.VehicleFlags.TriggerHasJumboMap);
            DrawCheckboxFlags("Trigger Once per Racer", ref node.Flags, (int)TriggerPhantomInstanceNode.VehicleFlags.TriggerOncePerRacer);
            ImGui.Unindent();
            
            DrawIndexedEnum<TriggerPhantomInstanceNode.LeaderType>("Leader", ref node.Leader);
            DrawInputInt("Num Activations", ref node.NumActivations);
            DrawDragFloat("Prediction Time", ref node.PredictionTime);
            
            ImGui.EndTable();
        }
        
        ImGui.PopStyleVar();
    }
    
    
}