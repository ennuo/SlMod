using System.Numerics;
using System.Reflection;
using ImGuiNET;
using SeEditor.Attributes;
using SeEditor.Utilities;
using SlLib.Enums;
using SlLib.IO;
using SlLib.Resources;
using SlLib.Resources.Scene;
using SlLib.Resources.Scene.Definitions;
using SlLib.Resources.Scene.Instances;

namespace SeEditor.Menu;

public class NodeAttributesMenu
{
    private static readonly Dictionary<Type, CustomInspectorDrawMethodInfo> CustomDrawMethods = [];
    private static readonly Dictionary<Type, List<Type>> TypeTreeCache = [];
    
    private class CustomInspectorDrawMethodInfo
    {
        public string Header = string.Empty;
        public Action<SeNodeBase> Draw;
    }
    
    static NodeAttributesMenu()
    {
        Type type = typeof(NodeAttributesMenu);
        foreach (MethodInfo method in type.GetMethods())
        {
            if (!method.IsStatic) continue;
            InspectorAttribute? attribute = method.GetCustomAttributes<InspectorAttribute>().FirstOrDefault();
            if (attribute == null) continue;

            ParameterInfo? parameter = method.GetParameters().FirstOrDefault();
            if (parameter == null) continue;
            
            // Dirty hack to avoid invalid signature error
            ConstructorInfo ctor = typeof(Action<SeNodeBase>).GetConstructors()[0];
            IntPtr handle = method.MethodHandle.GetFunctionPointer();
            var del = (Action<SeNodeBase>)ctor.Invoke([null!, handle]);
            
            CustomDrawMethods[parameter.ParameterType] = new CustomInspectorDrawMethodInfo
            {
                Header = attribute.Header,
                Draw = del
            };
        }
    }

    [Inspector("Basic Particle Affector")]
    public static void Draw(SeDefinitionParticleAffectorBasicNode node)
    {
        DrawIndexedEnum<ParticleForceMode>("Force Mode", ref node.ForceMode);
    }

    [Inspector("Particle System")]
    public static void Draw(SeInstanceParticleSystemNode node)
    {
        DrawColor4("Color Add", ref node.ColourAdd);
        DrawColor4("Color Multiply", ref node.ColourMul);
        
        DrawCheckboxFlags("Render Group 0", ref node.SystemFlags, 1 << 0);
        DrawCheckboxFlags("Render Group 1", ref node.SystemFlags, 1 << 1);
        DrawCheckboxFlags("Render Group 2", ref node.SystemFlags, 1 << 2);
        DrawCheckboxFlags("Render Group 3", ref node.SystemFlags, 1 << 3);
        
        int drawOrder = (node.SystemFlags >> 4) & 0xf;
        if (DrawInputInt("Draw Order Override", ref drawOrder))
        {
            drawOrder &= 0xf;
            node.SystemFlags &= ~0xf0;
            node.SystemFlags |= drawOrder << 4;
        }
    }

    [Inspector("Particle System")]
    public static void Draw(SeDefinitionParticleSystemNode node)
    {
        DrawCheckboxFlags("World Space", ref node.SystemFlagsBitField, 1 << 0);
        DrawDragFloat("Max Clip Size", ref node.MaxClipSize);
        DrawCheckboxFlags("Max Clip Size Disabled", ref node.SystemFlagsBitField, 1 << 1);

        DrawCheckboxFlags("Force Opaque Pass", ref node.SystemFlagsBitField, 1 << 6);
        
        int drawOrder = (node.SystemFlagsBitField >> 2) & 0xf;
        if (DrawInputInt("Draw Order", ref drawOrder))
        {
            drawOrder &= 0xf;
            node.SystemFlagsBitField &= ~0x3c;
            node.SystemFlagsBitField |= drawOrder << 2;
        }
    }

    [Inspector("Instance")]
    public static void Draw(SeInstanceNode node)
    {
        DrawDragFloat("Local Time", ref node.LocalTime);
        DrawDragFloat("Local Time Scale", ref node.LocalTimeScale);
        
        int timestep = (int)node.TimeStep;
        if (DrawIndexedEnum<InstanceTimeStep>("Time Frame", ref timestep))
            node.TimeStep = (InstanceTimeStep)timestep;
    }

    [Inspector("Definition")]
    public static void Draw(SeDefinitionNode node)
    {
        StartNewLine();
        ImGui.PushID("Instances");
        DoLabel("Instances");
        
        foreach (SeInstanceNode instance in node.Instances)
        {
            ImGui.TreeNodeEx(instance.ShortName, ImGuiTreeNodeFlags.Leaf | ImGuiTreeNodeFlags.NoTreePushOnOpen);
        }
        
        ImGui.PopID();
    }

    [Inspector("Weapon Pod")]
    public static void Draw(WeaponPodInstanceNode node)
    {
        DrawColor3("Pod Color", ref node.PodColor);
        DrawHashedEnum("Message", ref node.Message);
        DrawInputInt("Allocation Count", ref node.AllocationCount);
    }
    
    [Inspector("Transform")]
    public static void Draw(SeDefinitionTransformNode node)
    {
        Vector3 rotation = MathUtils.ToEulerAngles(node.Rotation);
        
        DrawHeader("Local Transform");
        ImGui.Indent();
        if (DrawDragFloat3("Rotation", ref rotation))
            node.Rotation = MathUtils.FromEulerAngles(rotation);
        DrawDragFloat3("Translation", ref node.Translation);
        DrawDragFloat3("Scale", ref node.Scale);
        ImGui.Unindent();
        
        DrawCheckboxFlags("Inherit Transforms", ref node.InheritTransforms, 1);
    }
    
    [Inspector("Transform")]
    public static void Draw(SeInstanceTransformNode node)
    {
        Vector3 rotation = MathUtils.ToEulerAngles(node.Rotation);
        
        
        DrawHeader("Local Transform");
        ImGui.Indent();
        if (DrawDragFloat3("Rotation", ref rotation))
            node.Rotation = MathUtils.FromEulerAngles(rotation);
        DrawDragFloat3("Translation", ref node.Translation);
        DrawDragFloat3("Scale", ref node.Scale);
        ImGui.Unindent();
        
        DrawCheckboxFlags("Inherit Transforms", ref node.InheritTransforms, 1);
        
        StartNewLine();
        if (ImGui.Button("GOTO!"))
        {
            Matrix4x4.Decompose(node.WorldMatrix, out Vector3 scale, out Quaternion r,
                out Vector3 translation);


            Vector3 position = translation;
            
            MainWindow.EditorCamera_Position = position;
            MainWindow.EditorCamera_Rotation = Vector3.Zero;
        }
    }
    
    [Inspector("Entity")]
    public static void Draw(SeInstanceEntityNode node)
    {
        DrawInputInt("Render Layer", ref node.RenderLayer);
        
        DrawHeader("Flags");
        ImGui.Indent();
        
        DrawCheckboxFlags("Force Forward Render", ref node.Flags, 1 << 0);
        DrawCheckboxFlags("Shadow Cast", ref node.Flags, 1 << 1);
        
        ImGui.Unindent();
    }

    [Inspector("Catchup Respot")]
    public static void Draw(CatchupRespotInstanceNode node)
    {
        DrawIndexedEnum<Laps>("Lap", ref node.Lap);
        DrawIndexedEnum<DriveMode>("Drive Mode", ref node.DriveMode);
    }

    [Inspector("Trigger Phantom")]
    public static void Draw(TriggerPhantomDefinitionNode node)
    {
        DrawIndexedEnum<TriggerPhantomShape>("Shape", ref node.Shape);
        switch ((TriggerPhantomShape) node.Shape)
        {
            case TriggerPhantomShape.Sphere:
            {
                DrawDragFloat("Radius", ref node.WidthRadius);
                break;
            }

            case TriggerPhantomShape.CylinderX:
            case TriggerPhantomShape.CylinderY:
            case TriggerPhantomShape.CylinderZ:
            {
                DrawDragFloat("Radius", ref node.WidthRadius);
                DrawDragFloat("Height", ref node.Height);
                break;
            }

            default:
            {
                DrawDragFloat("Width", ref node.WidthRadius);
                DrawDragFloat("Height", ref node.Height);
                DrawDragFloat("Depth", ref node.Depth);
                break;
            }
        }
    }
    
    [Inspector("Trigger Phantom")]
    public static void Draw(TriggerPhantomInstanceNode node)
    {
        DrawHeader("Action Events");
        ImGui.Indent();
        for (int i = 0; i < 8; ++i)
        {
            bool hasNodeLink = node.LinkedNode[i] != null;
            bool hasEvent = node.MessageText[i] != TriggerPhantomHashInfo.Empty;
            
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
        DrawCheckboxFlags("Trigger Car", ref node.PhantomFlags, (int)VehicleFlags.TriggerCar);
        DrawCheckboxFlags("Trigger Boat", ref node.PhantomFlags, (int)VehicleFlags.TriggerBoat);
        DrawCheckboxFlags("Trigger Plane", ref node.PhantomFlags, (int)VehicleFlags.TriggerPlane);
        DrawCheckboxFlags("Trigger Weapon", ref node.PhantomFlags, (int)VehicleFlags.TriggerWeapon);
        DrawCheckboxFlags("Trigger On-load", ref node.PhantomFlags, (int)VehicleFlags.TriggerOnLoad);
        DrawCheckboxFlags("Trigger Prediction", ref node.PhantomFlags, (int)VehicleFlags.TriggerPrediction);
        DrawCheckboxFlags("Trigger Has Jumbomap", ref node.PhantomFlags, (int)VehicleFlags.TriggerHasJumboMap);
        DrawCheckboxFlags("Trigger Once per Racer", ref node.PhantomFlags, (int)VehicleFlags.TriggerOncePerRacer);
        ImGui.Unindent();
        
        DrawIndexedEnum<LeaderType>("Leader", ref node.Leader);
        DrawInputInt("Num Activations", ref node.NumActivations);
        DrawDragFloat("Prediction Time", ref node.PredictionTime);
    }
    
    public static void Draw(SeNodeBase? node)
    {
        if (node == null) return;

        Type root = node.GetType();
        
        var types = TypeTreeCache.GetValueOrDefault(node.GetType());
        if (types == null)
        {
            types = [root];
            
            Type graphNodeType = typeof(SeGraphNode);
            Type definitionNodeType = typeof(SeDefinitionNode);
            
            Type transformNodeType = typeof(SeInstanceTransformNode);
            if (node is SeDefinitionNode)
                transformNodeType = typeof(SeDefinitionTransformNode);
            
            
            // Fetch the type hierarchy
            Type? type = root.BaseType;
            while (type != null && type != graphNodeType)
            {
                types.Add(type);
                type = type.BaseType;
            }
            
            // Make sure transform node properties are always drawn first
            if (types.Remove(transformNodeType))
                types.Add(transformNodeType);
            
            types.Reverse();

            // Definition list is really big so move it to the bottom
            if (types.Remove(definitionNodeType))
                types.Add(definitionNodeType);


            TypeTreeCache[root] = types;
        }
        
        foreach (Type type in types)
        {
            if (CustomDrawMethods.TryGetValue(type, out CustomInspectorDrawMethodInfo? info))
            {
                if (StartPropertyTable(info.Header))
                {
                    info.Draw(node);
                    ImGui.EndTable();
                }
            
                ImGui.PopStyleVar();
                
                continue;
            }

            var fields = type.GetFields(BindingFlags.DeclaredOnly | BindingFlags.Public | BindingFlags.Instance);
            if (fields.Length == 0) continue;

            if (StartPropertyTable(type.Name))
            {
                foreach (FieldInfo field in fields)
                {
                    object? value = field.GetValue(node);

                    if (field.FieldType.IsSubclassOf(typeof(SeNodeBase)) || field.FieldType == typeof(SeNodeBase))
                    {
                        var n = (SeNodeBase?)value;
                        
                        StartNewLine();
                        ImGui.PushID(field.Name);
                        DoLabel(field.Name);
                        
                        ImGui.PushItemWidth(-1.0f);
                        if (ImGui.BeginCombo("##message_link_combo", n?.ShortName ?? "-Empty-"))
                            ImGui.EndCombo();
                        ImGui.PopItemWidth();
                        
                        ImGui.PopID();

                        continue;
                    }
                    
                    switch (value)
                    {
                        case bool b:
                        {
                            if (DrawCheckbox(field.Name, ref b))
                                field.SetValue(node, b);
                            break;
                        }
                        case int i:
                        {
                            if (DrawInputInt(field.Name, ref i))
                                field.SetValue(node, i);
                            break;
                        }
                        case float f:
                        {
                            if (DrawDragFloat(field.Name, ref f))
                                field.SetValue(node, f);
                            
                            break;
                        }
                        case string s:
                        {
                            if (DrawInputText(field.Name, ref s))
                                field.SetValue(node, s);

                            break;
                        }
                        case Vector3 v3:
                        {
                            if (DrawDragFloat3(field.Name, ref v3))
                                field.SetValue(node, v3);

                            break;
                        }
                        case Vector4 v4:
                        {
                            if (DrawDragFloat4(field.Name, ref v4))
                                field.SetValue(node, v4);

                            break;
                        }
                    }
                }
                
                ImGui.EndTable();
            }
            
            ImGui.PopStyleVar();
        }
    }
    
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

    private static bool DrawCheckbox(string text, ref bool value)
    {
        StartNewLine();
        ImGui.PushID(text);
        DoLabel(text);
        
        ImGui.SetNextItemWidth(-1.0f);
        bool ret = ImGui.Checkbox("##value", ref value);
        
        ImGui.PopID();

        return ret;
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
    
    private static bool DrawInputText(string text, ref string value)
    {
        StartNewLine();
        ImGui.PushID(text);
        DoLabel(text);
        
        ImGui.SetNextItemWidth(-1.0f);
        bool ret = ImGui.InputText("##value", ref value, 255);
        
        ImGui.PopID();
        return ret;
    }

    private static bool DrawInputInt(string text, ref int value)
    {
        StartNewLine();
        ImGui.PushID(text);
        DoLabel(text);
        
        ImGui.SetNextItemWidth(-1.0f);
        bool ret = ImGui.InputInt("##value", ref value);
        
        ImGui.PopID();
        return ret;
    }

    private static bool DrawDragFloat(string text, ref float value)
    {
        StartNewLine();
        ImGui.PushID(text);
        DoLabel(text);
        
        ImGui.SetNextItemWidth(-1.0f);
        bool ret = ImGui.DragFloat("##value", ref value);
        
        ImGui.PopID();
        
        return ret;
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
    
    private static bool DrawColor4(string text, ref Vector4 value)
    {
        StartNewLine();
        ImGui.PushID(text);
        DoLabel(text);
        
        ImGui.SetNextItemWidth(-1.0f);
        bool input = ImGui.ColorEdit4("##value", ref value);
        
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
    
    private static bool DrawDragFloat4(string text, ref Vector4 value)
    {
        StartNewLine();
        ImGui.PushID(text);
        DoLabel(text);
        
        ImGui.SetNextItemWidth(-1.0f);
        bool input = ImGui.DragFloat4("##value", ref value);
        
        ImGui.PopID();
        return input;
    }

    private static bool DrawIndexedEnum<T>(string text, ref int value) where T : Enum
    {
        StartNewLine();
        ImGui.PushID(text);
        DoLabel(text);

        ImGui.SetNextItemWidth(-1.0f);
        bool ret = ImGuiHelper.DoIndexedEnum<T>("##value", ref value);
        ImGui.PopID();

        return ret;
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
}