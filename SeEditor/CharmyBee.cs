using System.Numerics;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Runtime.Serialization;
using ImGuiNET;
using OpenTK.Graphics.OpenGL;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;
using SeEditor.Editor;
using SeEditor.Editor.Panel;
using SeEditor.Editor.Tools;
using SeEditor.Editor.Tools.NavTool;
using SeEditor.Graphics.ImGui;
using SeEditor.Graphics.OpenGL;
using SeEditor.Installers;
using SeEditor.Managers;
using SeEditor.Renderer;
using SeEditor.Renderer.Buffers;
using SlLib.Excel;
using SlLib.Lookup;
using SlLib.Resources;
using SlLib.Resources.Database;
using SlLib.Resources.Model;
using SlLib.Resources.Model.Commands;
using SlLib.Resources.Scene;
using SlLib.Resources.Scene.Definitions;
using SlLib.Resources.Scene.Instances;
using SlLib.SumoTool;
using SlLib.SumoTool.Siff;
using SlLib.SumoTool.Siff.NavData;
using static SeEditor.Dialogs.TinyFileDialog;
using PrimitiveType = OpenTK.Graphics.OpenGL.PrimitiveType;
using Quaternion = System.Numerics.Quaternion;
using Vector2 = System.Numerics.Vector2;
using Vector3 = System.Numerics.Vector3;
using Vector4 = System.Numerics.Vector4;

namespace SeEditor;

public class CharmyBee : GameWindow
{
    private ImGuiController _controller;
    
    private SlResourceDatabase? _database;
    private Navigation? _navigation;
    
    private ExcelData _racerData;
    private ExcelData _trackData;
    
    private bool _quickstart = true;
    
    private SeGraphNode? _clipboard;
    
    private NavRenderMode _navRenderMode = NavRenderMode.Route;
    
    private float _localSceneFrameWidth;
    private float _localSceneFrameHeight;
    
    private bool _requestedWorkspaceClose = false;
    private SeGraphNode? _requestedNodeDeletion;
    
    private List<NavRoute> _routes = [];
    private NavWaypoint? _selectedWaypoint;
    private NavRoute? _selectedRoute;
    private int _selectedRacingLine = -1;
    private int _selectedRacingLineSegment = -1;
    
    
    public class TreeNode(string name)
    {
        public string Name = name;
        public SeDefinitionNode? Association;
        public bool IsFolder;
        public List<TreeNode> Children = [];
    }
    
    public TreeNode Root = new("assets");
    
    private TreeNode AddFolderNode(string path)
    {
        if (string.IsNullOrEmpty(path)) return Root;
        path = path.Replace("\\", "/").Replace("assets/default/", string.Empty);
        
        TreeNode root = Root;
        string[] components = path.Split('/');
        foreach (string component in components)
        {
            TreeNode? child = root.Children.Find(n => n.Name == component);
            if (child == null)
            {
                child = new TreeNode(component)
                {
                    IsFolder = true
                };
                root.Children.Add(child);
                root = child;
            }
            else root = child;
        }
        
        return root;
    }

    private TreeNode AddItemNode(string path)
    {
        TreeNode folder = AddFolderNode(Path.GetDirectoryName(path) ?? string.Empty);
        TreeNode node = new TreeNode(Path.GetFileName(path));
        folder.Children.Add(node);
        return node;
    }
    
    public CharmyBee(string title, int width, int height) :
        base(GameWindowSettings.Default, new NativeWindowSettings { ClientSize = (width, height), Title = title })
    {
        VSync = VSyncMode.On;
    }

    protected override void OnLoad()
    {
        Title = "Sumo Engine Editor - Unnamed Workspace <OpenGL>";
        
        _controller = new ImGuiController(ClientSize.X, ClientSize.Y);
        
        ImGuiStylePtr style = ImGui.GetStyle();
        
        
        style.WindowMenuButtonPosition = ImGuiDir.None;
        style.TabRounding = 1.0f;
        style.FrameRounding = 1.0f;
        style.ScrollbarRounding = 1.0f;
        style.WindowRounding = 0.0f;
        style.DockingSeparatorSize = 1.0f;
        
        ImGui.GetIO().ConfigDragClickToInputText = true;
        ImGui.GetIO().ConfigWindowsMoveFromTitleBarOnly = true;
        
        SlFile.AddGameDataFolder("F:/sart/game/pc");

        _racerData = SlFile.GetExcelData("gamedata/racers") ??
                     throw new FileNotFoundException("Could not load racer data!");
        _trackData = SlFile.GetExcelData("gamedata/tracks") ??
                     throw new FileNotFoundException("Could not load track data!");
        
        SlRenderer.InitializeForOpenGL(ClientSize.X, ClientSize.Y);
        if (_quickstart)
        {
            SceneManager.LoadScene("levels/seasidehill2/seasidehill2");
            SetupNavigationRendering();
            OnWorkspaceLoad();
        }
    }

    private void SetupNavigationRendering()
    {
        _navigation = SceneManager.Current.Navigation;
        if (_navigation == null) return;
        
        foreach (NavWaypoint waypoint in _navigation.Waypoints)
        {
            var routeId = int.Parse(waypoint.Name.Split("_")[1]);

            NavRoute? route = _routes.Find(route => route.Id == routeId);
            if (route == null)
            {
                route = new NavRoute(routeId);
                _routes.Add(route);
            }

            route.Waypoints.Add(waypoint);
        }

        _routes.Sort((a, z) => a.Id - z.Id);
        foreach (var route in _routes)
        {
            route.Waypoints.Sort((a, z) =>
            {
                int indexA = int.Parse(a.Name.Split("_")[2]);
                int indexB = int.Parse(z.Name.Split("_")[2]);

                return indexA - indexB;
            });
        }

        if (_routes.Count > 0)
        {
            _selectedRoute = _routes[0];
            _selectedWaypoint = _selectedRoute.Waypoints.FirstOrDefault();
        }

        if (_navigation.RacingLines.Count > 0)
        {
            _selectedRacingLine = 0;
            if (_navigation.RacingLines[0].Segments.Count > 0)
                _selectedRacingLineSegment = 0;
        }
    }
    
    private void OnWorkspaceLoad()
    {
        _database = SceneManager.Current.Database;
        if (_database == null || SceneManager.DisableRendering) return;
        
        string name = SceneManager.Current.SourceFileName;
        name = string.IsNullOrEmpty(name) ? "Unnamed Workspace" : Path.GetFileNameWithoutExtension(name);
        Title = $"Sumo Engine Editor - {name} <OpenGL>";
        
        var textures = _database.GetResourcesOfType<SlTexture>();
        foreach (SlTexture texture in textures)
        {
            SlTextureInstaller.Install(texture); 
            AddItemNode(texture.Header.Name);
        }

        foreach (SlSkeleton skeleton in _database.GetResourcesOfType<SlSkeleton>())
            AddItemNode(skeleton.Header.Name);
        foreach (SlMaterial2 material in _database.GetResourcesOfType<SlMaterial2>())
            AddItemNode(material.Header.Name);
        foreach (SlAnim anim in _database.GetResourcesOfType<SlAnim>())
            AddItemNode(anim.Header.Name);
        
        var models = _database.GetResourcesOfType<SlModel>();
        foreach (SlModel model in models)
        {
            SlModelInstaller.Install(model);
            AddItemNode(model.Header.Name);
        }
    }

    private void TriggerCloseWorkspace()
    {
        Title = "Sumo Engine Editor - <OpenGL>";
        if (_database == null)
        {
            _requestedWorkspaceClose = false;
            return;
        }

        var textures = _database.GetResourcesOfType<SlTexture>();
        foreach (SlTexture texture in textures)
        {
            if (texture.ID == 0) continue;
            GL.DeleteTexture(texture.ID);
            texture.ID = 0;
        }

        var models = _database.GetResourcesOfType<SlModel>();
        foreach (SlModel model in models)
        {
            if (model.Resource.Segments.Count == 0) continue;

            foreach (SlStream stream in model.Resource.PlatformResource.VertexStreams)
            {
                if (stream.VBO == 0) continue;
                GL.DeleteBuffer(stream.VBO);
                stream.VBO = 0;
            }

            var indexStream = model.Resource.PlatformResource.IndexStream;
            if (indexStream.VBO != 0)
            {
                GL.DeleteBuffer(indexStream.VBO);
                indexStream.VBO = 0;
            }

            foreach (SlModelSegment segment in model.Resource.Segments)
            {
                if (segment.VAO == 0) continue;

                GL.DeleteVertexArray(segment.VAO);
                segment.VAO = 0;
            }
        }

        _requestedWorkspaceClose = false;
        _database = null;
        _navigation = null;
        
        _routes.Clear();
        _selectedWaypoint = null;
        _selectedRoute = null;
        _selectedRacingLine = -1;
        _selectedRacingLineSegment = -1;
        
        GC.Collect();
    }
    
    protected override void OnResize(ResizeEventArgs e)
    {
        base.OnResize(e);

        // Tell ImGui of the new size
        _controller.WindowResized(ClientSize.X, ClientSize.Y);
    }
    
    private void RenderMainDockWindow()
    {
        ImGui.SetNextWindowPos(Vector2.Zero);
        ImGui.SetNextWindowSize(new Vector2(ClientSize.X, ClientSize.Y));
        ImGuiWindowFlags flags =
            ImGuiWindowFlags.NoBringToFrontOnFocus |
            ImGuiWindowFlags.NoNavFocus |
            ImGuiWindowFlags.NoDocking |
            ImGuiWindowFlags.NoTitleBar |
            ImGuiWindowFlags.NoResize |
            ImGuiWindowFlags.NoMove |
            ImGuiWindowFlags.NoCollapse |
            ImGuiWindowFlags.MenuBar |
            ImGuiWindowFlags.NoBackground;

        ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, Vector2.Zero);
        bool open = false;
        bool show = ImGui.Begin("Main", ref open, flags);
        ImGui.PopStyleVar();

        
        if (show)
        {
            if (ImGui.BeginMainMenuBar())
            {
                if (ImGui.BeginMenu("File"))
                {
                    if (ImGui.MenuItem("open menu"))
                    {
                        tinyfd_messageBox("Editor", "Eat shit", "ok", "info", 1);
                    }
                    
                    if (ImGui.MenuItem("Close Workspace"))
                        _requestedWorkspaceClose = true;
                    
                    if (ImGui.MenuItem("DEBUG SAVE TO DESKTOP"))
                    {
                        _database?.Save(
                            @"C:/Users/Aidan/Desktop/sample.cpu.spc",
                            @"C:/Users/Aidan/Desktop/sample.gpu.spc",
                            inMemory: true);
                    }

                    if (ImGui.MenuItem("DEBUG SAVE TO SEASIDEHILL"))
                    {
                        _database?.Save(
                            @"C:\\Program Files (x86)\\Steam\\steamapps\\common\\Sonic & All-Stars Racing Transformed\\Data\\levels\\seasidehill2\\seasidehill2.cpu.spc",
                            @"C:\\Program Files (x86)\\Steam\\steamapps\\common\\Sonic & All-Stars Racing Transformed\\Data\\levels\\seasidehill2\\seasidehill2.gpu.spc",
                            inMemory: true);
                    }

                    if (ImGui.MenuItem("DEBUG SAVE ALL"))
                    {
                        _database?.FlushSceneGraph();
                    }

                    ImGui.EndMenu();
                }
                
                ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(3.0f, 4.0f));
                if (ImGui.BeginMenu("Nodes"))
                {
                    DrawNodeCreationMenu();
                    ImGui.EndMenu();
                }
                ImGui.PopStyleVar(1);

                ImGui.SetCursorPos(ImGui.GetCursorPos() + new Vector2(10.0f, 0.0f));
                if (ImGui.BeginTabBar("##modes"))
                {
                    if (ImGui.BeginTabItem("Layout"))
                    {
                        ImGui.EndTabItem();
                    }

                    if (ImGui.BeginTabItem("Navigation"))
                    {
                        ImGui.EndTabItem();
                    }
                    
                    ImGui.EndTabBar();
                }


                float width = ImGui.GetWindowWidth();
                float framerate = ImGui.GetIO().Framerate;
                ImGui.SetCursorPosX(width - 100);
                ImGui.Text($"({framerate:0.#} FPS)");

                ImGui.EndMainMenuBar();
            }
        }

        ImGui.End();
    }

    private void DrawNodeCreationMenu()
    {
        if (ImGui.MenuItem("Create Empty Folder", "Ctrl+Shift+N"))
        {
            
        }
    }

    // this function is pretty expensive, ~10% of processing time
    // TODO: Add scenegraph dirty flags to avoid re-calculating every frame.
    private void RecomputeAllWorldMatrices()
    {
        if (_database == null) return;

        Recompute(_database.Scene);
        
        return;

        void Recompute(SeGraphNode node)
        {
            if (node is SeInstanceTransformNode entity)
            {
                Matrix4x4 local =
                    Matrix4x4.CreateScale(entity.Scale) *
                    Matrix4x4.CreateFromQuaternion(entity.Rotation) *
                    Matrix4x4.CreateTranslation(entity.Translation);

                Matrix4x4 world = local;

                var animator = entity.FindAncestorThatDerivesFrom<SeInstanceAnimatorNode>();
                // if (animator != null && (entity.TransformFlags & 1) != 0)
                // {
                //     short index = (short)((entity.TransformFlags << 0x15) >>> 0x16);
                //     Matrix4x4 bind = Matrix4x4.Identity;
                //     if (animator.Definition is SeDefinitionAnimatorNode def)
                //     {
                //         SlSkeleton? skeleton = def.Skeleton;
                //         if (skeleton != null)
                //             bind = skeleton.Joints[index].BindPose;
                //     }
                //     
                //     world = (local * bind) * animator.WorldMatrix;
                // }
                // else
                {
                    var parent = entity.FindAncestorThatDerivesFrom<SeInstanceTransformNode>();
                    // if (parent != null && (entity.InheritTransforms & 3) != 3)
                    if (parent != null && (entity.InheritTransforms & 1) != 0)
                        world = local * parent.WorldMatrix;
                }

                entity.WorldMatrix = world;
            }

            SeGraphNode? child = node.FirstChild;
            while (child != null)
            {
                Recompute(child);
                child = child.NextSibling;
            }
        }
    }

    private InspectorPanel _inspectorPanel = new InspectorPanel();
    private void RenderAttributeMenu()
    {
        _inspectorPanel.OnSelectionChanged();
        _inspectorPanel.OnImGuiRender();
    }

    private void SetRenderContextMaterial(SlModelRenderContext context, SlMaterial2 material)
    {
        if (context.Material == material) return;
        context.Material = material;
        Shader shader = SlRenderer.DefaultShader;
        
        SlConstantBuffer? cb = material.IndexToConstantBuffer[SlRenderBuffers.CommonModifiers];
        if (cb != null)
        {
            SlRenderer.SetConstantBuffer(SlRenderBuffers.CommonModifiers, cb.Data);
        }
        else
        {
            SlRenderer.SetConstantBuffer(SlRenderBuffers.CommonModifiers, new ConstantBufferCommonModifiers
            {
                AlphaRef = 0.01f,
                ColorAdd = Vector4.Zero,
                ColorMul = Vector4.One,
                FogMul = 0.0f
            });    
        }
        
        if (context.Wireframe)
        {
            Vector4 one = Vector4.One;
            Vector4 zero = Vector4.Zero;
            Vector4 col = new Vector4(255.0f / 255.0f, 172.0f / 255.0f, 28.0f / 255.0f, 1.0f);
            
            shader.SetInt("gHasColorStream", 0);
            shader.SetInt("gHasDiffuseTexture", 0);
            shader.SetVector3("gColour", ref col);

            GL.LineWidth(5.0f);
            GL.PolygonMode(MaterialFace.FrontAndBack, PolygonMode.Line);
        }
        else
        {
            GL.PolygonMode(MaterialFace.FrontAndBack, PolygonMode.Fill);
            SlSampler? diffuseSampler = material.Samplers.Find(sampler =>
                sampler.Header.Name is "gDiffuseTexture" or "gAlbedoTexture");

            SlSampler? emissiveSampler = material.Samplers.Find(sampler => sampler.Header.Name is "gEmissiveTexture");

            SlTexture? diffuse = diffuseSampler?.Texture.Instance;
            SlTexture? emissive = emissiveSampler?.Texture.Instance;
            
            Vector4 color = Vector4.One;
            
            // add lookups for constants to materials, 2.54% time spent in this function.
            if (material.HasConstant("gDiffuseColour"))
                color = material.GetConstant("gDiffuseColour");
            
            shader.SetVector3("gColour", ref color);

            if (emissive == null)
            {
                shader.SetInt("gHasEmissiveTexture", 0);
            }
            else
            {
                shader.SetInt("gHasEmissiveTexture", 1);
                shader.SetInt("gEmissiveTexture", 1);
                
                GL.ActiveTexture(TextureUnit.Texture1);
                GL.BindTexture(TextureTarget.Texture2D, emissive.ID);
            }

            if (diffuse == null)
            {
                shader.SetInt("gHasDiffuseTexture", 0);
            }
            else
            {
                shader.SetInt("gHasDiffuseTexture", 1);
                GL.ActiveTexture(TextureUnit.Texture0);
                GL.BindTexture(TextureTarget.Texture2D, diffuse.ID);
            }
        }
    }

    private void RenderSegmentInstanced(SlModel model, SlModelSegment segment, SlModelRenderContext context)
    {
        SlMaterial2? material = model.Materials[segment.MaterialIndex];
        if (material != null) SetRenderContextMaterial(context, material);
        foreach (SlModelInstanceData instance in context.Instances)
        {
            SlRenderer.SetConstantBuffer(SlRenderBuffers.WorldMatrix, instance.WorldMatrix);
            SlRenderer.DefaultShader.SetInt("gHasColorStream",
                segment.Format.HasAttribute(SlVertexUsage.Color) ? 1 : 0);


            GL.BindVertexArray(segment.VAO);
            GL.DrawElements(PrimitiveType.Triangles, segment.Sector.NumElements, DrawElementsType.UnsignedShort,
                segment.FirstIndex * sizeof(ushort));
        }
    }

    private void RenderEntity(SeInstanceEntityNode instance, SeDefinitionEntityNode entity, bool wireframe)
    {
        // re-add this at some point
        if (instance is SeInstanceEntityShadowNode) return;

        SlModel? model = entity.Model;
        if (model == null) return;
        if (model.Resource.Segments.Count == 0) return;
        
        if (!SceneCamera.Active.IsOnFrustum(instance)) return;
        

        SlSkeleton? skeleton = model.Resource.Skeleton;

        SlModelRenderContext context = SlRenderer.Context;
        context.Prepare(wireframe);
        
        var data = new SlModelInstanceData
        {
            InstanceWorldMatrix = instance.WorldMatrix,
            InstanceBindMatrix = instance.WorldMatrix
        };

        // SetMul = result, right, left

        if (model.Resource.EntityIndex != -1 && skeleton != null)
            data.InstanceBindMatrix =
                skeleton.Joints[model.Resource.EntityIndex].InverseBindPose * data.InstanceBindMatrix;


        context.EntityWorldMatrix = instance.WorldMatrix;

        // TestVisibility
        // SlCullSphere* Sphere
        // Matrix4x4* Model (entity world pos, not bind @ 0x0)
        // Matrix4x4* ??? (some camera matrix?)
        // SlModelContext*
        // bool ??? (false)

        context.SceneGraphInstances.Add(data);
        context.Instances = context.SceneGraphInstances;

        SlRenderer.DefaultShader.SetInt("gEntityID", instance.Uid);

        foreach (IRenderCommand command in model.Resource.RenderCommands)
        {
            // Special case for the actual rendering
            if (command is RenderSegmentCommand renderSegmentCommand)
            {
                if (context.NextSegmentIsVisible)
                {
                    SlModelSegment segment = model.Resource.Segments[renderSegmentCommand.SegmentIndex];
                    RenderSegmentInstanced(model, segment, context);
                }

                context.NextSegmentIsVisible = true;
            }
            else command.Work(model, context);
        }
    }
    
    private TreeNode? _selectedFolder;
    private void RenderAssetView()
    {
        if (_database == null) return;
        _selectedFolder ??= Root;
        
        ImGui.BeginChild("Folder View", new Vector2(150.0f, 0.0f), ImGuiChildFlags.ResizeX | ImGuiChildFlags.Border);
        DrawDirectoryTree(Root);
        ImGui.EndChild();
        
        ImGui.SameLine();

        ImGui.BeginChild("Item View");
        if (_selectedFolder != null)
        {
            foreach (var item in _selectedFolder.Children)
            {
                ImGui.TreeNodeEx(item.Name, ImGuiTreeNodeFlags.Leaf | ImGuiTreeNodeFlags.NoTreePushOnOpen);
                if (ImGui.IsItemHovered() && ImGui.IsMouseDoubleClicked(ImGuiMouseButton.Left))
                {
                    if (item.IsFolder)
                        _selectedFolder = item;
                }
            }
        }
        ImGui.EndChild();
        
        
        return;

        void DrawDirectoryTree(TreeNode root)
        {
            bool isLeaf = !root.Children.Any(c => c.IsFolder);
            var flags = ImGuiTreeNodeFlags.OpenOnArrow | ImGuiTreeNodeFlags.OpenOnDoubleClick;
            if (isLeaf) flags |= ImGuiTreeNodeFlags.Leaf;
            if (_selectedFolder == root) flags |= ImGuiTreeNodeFlags.Selected;
            
            bool open = ImGui.TreeNodeEx(root.Name, flags);
            if (ImGui.IsItemClicked())
                _selectedFolder = root;
            
            if (open)
            {
                foreach (TreeNode child in root.Children)
                {
                    if (!child.IsFolder) continue;
                    DrawDirectoryTree(child);   
                }
                
                ImGui.TreePop();
            }
        }
    }

    private void RenderHierarchy(bool definitions)
    {
        if (_database == null) return;
        
        if (definitions)
        {
            foreach (SeDefinitionNode definition in _database.RootDefinitions)
            {
                // hide pointless nodes
                // if (definition is SeDefinitionTextureNode or SeDefinitionEntityNode or SeProject or SeProjectEnd or SeWorkspace or SeWorkspaceEnd
                //     or SeDefinitionAnimationStreamNode or SeDefinitionCollisionNode or SeDefinitionAnimatorNode or SeDefinitionFolderNode or SeDefinitionLocatorNode) continue;
                
                
                DrawTree(definition);
            }
        }
        else
        {
            SeGraphNode? child = _database.Scene.FirstChild;
            while (child != null)
            {
                DrawTree(child);
                child = child.NextSibling;
            }
        }

        void DrawTree(SeGraphNode root)
        {
            if (definitions && root is SeInstanceNode) return;
            if (!definitions && root is SeDefinitionNode) return;
            //if (root is SeInstanceAnimatorNode) return;

            bool isLeaf = root.FirstChild == null;

            var flags = ImGuiTreeNodeFlags.OpenOnArrow | ImGuiTreeNodeFlags.OpenOnDoubleClick;
            if (isLeaf) flags |= ImGuiTreeNodeFlags.Leaf;
            if (Selection.ActiveNode == root) flags |= ImGuiTreeNodeFlags.Selected;

            // string name = Path.GetFileNameWithoutExtension(root.GetShortName());
            // if (name.StartsWith(root.Prefix, StringComparison.InvariantCultureIgnoreCase))
            // {
            //     name = root.Prefix.ToUpper() + name[root.Prefix.Length..];
            // }

            bool open = ImGui.TreeNodeEx(root.Uid, flags, root.CleanName);

            if (ImGui.BeginDragDropSource())
            {
                // Just to get it to not crash
                GCHandle handle = GCHandle.Alloc(root.Uid);
                ImGui.SetDragDropPayload("EDITOR_NODE", (IntPtr)handle, sizeof(int), ImGuiCond.Once);
                handle.Free();

                ImGui.AlignTextToFramePadding();
                ImGuiHelper.DoBoldText(root.GetType().Name);
                ImGui.AlignTextToFramePadding();
                ImGui.Text(root.ShortName);


                ImGui.EndDragDropSource();
            }
            else if ((ImGui.IsItemHovered() && ImGui.IsMouseReleased(ImGuiMouseButton.Left)) &&
                     !ImGui.IsItemToggledOpen())
                Selection.ActiveNode = root;


            if (ImGui.BeginPopupContextItem())
            {
                Selection.ActiveNode = root;

                if (ImGui.BeginMenu("Create"))
                {
                    if (ImGui.MenuItem("saronali_test_1"))
                    {
                        var database = SlFile.GetSceneDatabase("pickupstar")!;
                        database.CopyTo(_database!);
                    }

                    if (ImGui.MenuItem("cubetest1"))
                    {
                        var database = SlFile.GetSceneDatabase("export/cubetest1")!;
                        database.CopyTo(_database!);
                        
                        var inst = _database!.FindNodeByPartialName<SeInstanceEntityNode>("root_cube")!;
                        inst.Scale = new Vector3(4.0f);
                        inst.Translation = new Vector3(180.0f, -55.0f, 0.0f);
                        
                        OnWorkspaceLoad();
                    }

                    if (ImGui.MenuItem("Model"))
                    {
                        // var importer =
                        //     new SlSceneImporter(new SlImportConfig(_workspaceDatabaseFile!, "F:/sart/deer.glb"));
                        //
                        // SlModel model = importer.Import();
                        // _workspaceDatabaseFile!.AddResource(model);

                        SlResourceDatabase.Load("C:/Users/Aidan/Desktop/gwii_moomoomeadows.cpu.spc",
                                "C:/Users/Aidan/Desktop/gwii_moomoomeadows.gpu.spc", inMemory: true)
                            .CopyTo(_database);
                        
                        SlModel? model = _database.FindResourceByPartialName<SlModel>("se_entity_gwii_moomoomeadows")!;

                        var definition = SeDefinitionNode.CreateObject<SeDefinitionEntityNode>();
                        var instance = SeInstanceNode.CreateObject<SeInstanceAreaNode>();

                        definition.UidName = model.Header.Name;
                        instance.Definition = definition;
                        instance.UidName = "se_area_seasidehill";

                        definition.Model = new SlResPtr<SlModel>(model);

                        _database!.RootDefinitions.Add(definition);

                        instance.Parent = Selection.ActiveNode;

                        OnWorkspaceLoad();
                    }


                    if (ImGui.MenuItem("Folder"))
                    {
                        var folder = SeInstanceNode.CreateObject<SeInstanceFolderNode>();

                        folder.Debug_ResourceType = SlResourceType.SeInstanceFolderNode;
                        folder.Definition = SeDefinitionFolderNode.Default;
                        folder.Parent = Selection.ActiveNode;
                    }

                    ImGui.EndMenu();
                }


                ImGui.Separator();

                if (ImGui.MenuItem("Copy"))
                {
                    _clipboard = Selection.ActiveNode;
                }

                if (ImGui.MenuItem("Paste", _clipboard != null))
                {
                    _database!.PasteClipboard([_clipboard!], Selection.ActiveNode.Parent);
                }

                if (ImGui.MenuItem("Paste as Child", _clipboard != null))
                {
                    _database!.PasteClipboard([_clipboard!], Selection.ActiveNode);
                }
                
                ImGui.Separator();

                if (ImGui.MenuItem("Delete"))
                {
                    _requestedNodeDeletion = Selection.ActiveNode;
                    Selection.ActiveNode = null;
                }

                ImGui.EndPopup();
            }

            if (open)
            {
                SeGraphNode? child = root.FirstChild;
                while (child != null)
                {
                    DrawTree(child);
                    child = child.NextSibling;
                }

                ImGui.TreePop();
            }
        }
    }
    
    private void RenderRacingLineEditor()
    {
        if (_navigation == null) return;

        ImGui.Begin("Navigation");

        // name format is 
        // $"waypoint_{route}_{waypoint}"
        

        if (ImGui.BeginTabBar("##racingLineEditorTabBar"))
        {
            if (ImGui.BeginTabItem("Waypoints"))
            {
                _navRenderMode = NavRenderMode.Route;
                
                ImGui.PushItemWidth(-1.0f);
                
                if (ImGui.BeginCombo("##Route", _selectedRoute?.Name ?? "None"))
                {
                    for (int i = 0; i < _routes.Count; ++i)
                    {
                        bool selected = _routes[i] == _selectedRoute;
                        if (ImGui.Selectable(_routes[i].Name, selected))
                            _selectedRoute = _routes[i];
                        if (selected)
                            ImGui.SetItemDefaultFocus();
                    }
                    
                    ImGui.EndCombo();
                }
                ImGui.PopItemWidth();
                
                
                ImGui.BeginChild("Left Pane", new Vector2(150.0f, 0.0f), ImGuiChildFlags.Border | ImGuiChildFlags.ResizeX);
                
                if (_selectedRoute != null)
                {
                    foreach (NavWaypoint waypoint in _selectedRoute.Waypoints)
                    {
                        if (ImGui.Selectable(waypoint.Name, _selectedWaypoint == waypoint))
                            _selectedWaypoint = waypoint;
                    }   
                }
                
                ImGui.EndChild();
                
                ImGui.SameLine();

                ImGui.BeginChild("Item View", new Vector2(0.0f, -ImGui.GetFrameHeightWithSpacing()));

                if (_selectedWaypoint != null)
                {
                    ImGui.Text(_selectedWaypoint.Name);
                    ImGui.Separator();

                    ImGui.DragFloat3("Position", ref _selectedWaypoint.Pos);
                    ImGui.DragFloat3("Direction", ref _selectedWaypoint.Dir, 0.01f, -1.0f, 1.0f);
                    ImGui.DragFloat3("Up", ref _selectedWaypoint.Up, 0.01f, -1.0f, 1.0f);
                    string name = _selectedWaypoint.UnknownWaypoint?.Name ?? "None";
                    ImGui.InputText("groupend", ref name, 256);
                }

                ImGui.EndChild();
                
                ImGui.EndTabItem();
            }

            if (ImGui.BeginTabItem("Segments"))
            {
                _navRenderMode = NavRenderMode.RacingLine;
                
                ImGui.PushItemWidth(-1.0f);
                
                if (ImGui.BeginCombo("##RacingLines", $"Racing Line {_selectedRacingLine}"))
                {
                    for (int i = 0; i < _navigation.RacingLines.Count; ++i)
                    {
                        bool selected = i == _selectedRacingLine;
                        if (ImGui.Selectable($"Racing Line {i}", selected))
                            _selectedRacingLine = i;
                        if (selected)
                            ImGui.SetItemDefaultFocus();
                    }
                    
                    ImGui.EndCombo();
                }
                ImGui.PopItemWidth();
                
                
                ImGui.BeginChild("Left Pane", new Vector2(150.0f, 0.0f), ImGuiChildFlags.Border | ImGuiChildFlags.ResizeX);
                
                if (_selectedRacingLine != -1)
                {
                    NavRacingLine line = _navigation.RacingLines[_selectedRacingLine];
                    for (int i = 0; i < line.Segments.Count; ++i)
                    {
                        if (ImGui.Selectable($"Segment {i}", _selectedRacingLineSegment == i))
                            _selectedRacingLineSegment = i;
                    }
                }
                
                ImGui.EndChild();
                
                ImGui.SameLine();

                ImGui.BeginChild("Item View", new Vector2(0.0f, -ImGui.GetFrameHeightWithSpacing()));

                if (_selectedRacingLineSegment != -1)
                {
                    ImGui.Text($"Segment {_selectedRacingLineSegment}");
                    ImGui.Separator();

                    NavWaypointLink? link = _navigation.RacingLines[_selectedRacingLine]
                        .Segments[_selectedRacingLineSegment].Link;
                    
                    if (link != null)
                    {
                        if (ImGui.BeginCombo("From", link.From!.Name)) ImGui.EndCombo();
                        if (ImGui.BeginCombo("To", link.To!.Name)) ImGui.EndCombo();
                        ImGui.DragFloat("Width", ref link.Width);
                    }

                }

                ImGui.EndChild();
                
                ImGui.EndTabItem();
            }

            if (ImGui.BeginTabItem("Track Layout"))
            {
                
                ImGui.EndTabItem();
            }
            
            
            ImGui.EndTabBar();
        }
        
        
        ImGui.End();
    }

    private void RenderWorkspace()
    {
        ImGui.Begin("Hierarchy");
        RenderHierarchy(false);
        ImGui.End();

        ImGui.Begin("Definitions");
        RenderHierarchy(true);
        ImGui.End();

        ImGui.Begin("Inspector");
        RenderAttributeMenu();
        ImGui.End();

        ImGui.Begin("Assets");
        RenderAssetView();
        ImGui.End();
        
        // if (Selection.ActiveNode is SeInstanceNode instance && instance.Definition != null)
        // {
        //     ImGui.Begin("Definition");
        //     RenderAttributeMenu(instance.Definition);
        //     ImGui.End();
        // }

        ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, Vector2.Zero);
        ImGui.Begin("Scene");

        _localSceneFrameWidth = ImGui.GetContentRegionAvail().X;
        _localSceneFrameHeight = ImGui.GetContentRegionAvail().Y;

        Vector2 pos = ImGui.GetCursorScreenPos();

        var minWindowPoint = new Vector2(pos.X, pos.Y);
        var maxWindowPoint = new Vector2(pos.X + _localSceneFrameWidth, pos.Y + _localSceneFrameHeight);

        ImGui.GetWindowDrawList().AddImage(
            SlRenderer.Framebuffer.GetRenderTexture(),
            minWindowPoint,
            maxWindowPoint,
            new Vector2(0.0f, 1.0f),
            new Vector2(1.0f, 0.0f)
        );


        if (ImGui.IsMouseHoveringRect(minWindowPoint, maxWindowPoint))
        {
            // So you don't have to left click the window first to move the camera.
            if (ImGui.IsMouseClicked(ImGuiMouseButton.Middle))
                ImGui.SetWindowFocus();

            // Avoid any calculations if we're not in a state where we can manipulate the camera.
            if (ImGui.IsWindowFocused())
            {
                SceneCamera.Active.OnInput(KeyboardState, MouseState);
                if (ImGui.IsMousePosValid() && ImGui.IsMouseReleased(ImGuiMouseButton.Left))
                {
                    Vector2 mousePos = ImGui.GetIO().MousePos - pos;
                    int pick = SlRenderer.Framebuffer.GetEntityPick((int)mousePos.X, (int)_localSceneFrameHeight - (int)mousePos.Y);
                    Selection.ActiveNode = _database?.LoadGenericNode(pick);
                }
            }
        }

        ImGui.End();
        ImGui.PopStyleVar();

        RenderRacingLineEditor();
    }

    private static float[] TransparentColorData = [0.0f, 0.0f, 0.0f, 0.0f];


    private void RenderPrimitives()
    {
        GL.Disable(EnableCap.DepthTest);
        GL.Clear(ClearBufferMask.DepthBufferBit);

        // render locators
        {
            PrimitiveRenderer.BeginPrimitiveScene();

            // foreach (TriggerPhantomDefinitionNode def in _workspaceDatabaseFile.GetNodesOfType<TriggerPhantomDefinitionNode>())
            // foreach (TriggerPhantomInstanceNode phantom in def.Instances)
            // {
            //     Vector3 scale = new Vector3(def.WidthRadius, def.Height, def.Depth);
            //     Vector3 translation = phantom.WorldMatrix.Translation;
            //
            //     Matrix4x4 rot = Matrix4x4.CreateFromQuaternion(Quaternion.CreateFromRotationMatrix(phantom.WorldMatrix));
            //     
            //
            //
            //     Matrix4x4 matrix = Matrix4x4.CreateScale(scale) * rot * Matrix4x4.CreateTranslation(translation);
            //     LineRenderPrimitives.DrawBoundingBox(matrix); 
            // }

            // if (_selected is SeInstanceCollisionNode { Definition: SeDefinitionCollisionNode colDef } colInst)
            // {
            //     SlResourceCollision? collision = colDef.Collision;
            //     if (collision != null)
            //     {
            //         foreach (var section in collision.Mesh.Sections)
            //         foreach (var branch in section.Branches)
            //         {
            //             if (branch.Leaf != -1) continue;
            //
            //             Vector3 scale = branch.Extents.AsVector128().AsVector3() * 2.0f;
            //
            //             Vector3 translation =
            //                 Vector4.Transform(branch.Center, colInst.WorldMatrix)
            //                     .AsVector128()
            //                     .AsVector3();
            //             
            //             LineRenderPrimitives.DrawBoundingBox(Matrix4x4.CreateScale(scale) * Matrix4x4.CreateTranslation(translation));
            //         }
            //     }
            // }
            
            if (Selection.ActiveNode is SeInstanceEntityNode { Definition: SeDefinitionEntityNode entityDef } entityInst)
            {
                SlModel? model = entityDef.Model;
                if (model != null)
                {
                    Vector3 scale = model.CullSphere.Extents.AsVector128().AsVector3() * 2.0f;

                    Vector3 translation =
                        Vector4.Transform(model.CullSphere.BoxCenter, entityInst.WorldMatrix)
                            .AsVector128()
                            .AsVector3();
                    
                    Matrix4x4 rot = Matrix4x4.CreateFromQuaternion(Quaternion.CreateFromRotationMatrix(entityInst.WorldMatrix));
            
                    Matrix4x4 matrix = Matrix4x4.CreateScale(scale) * Matrix4x4.CreateTranslation(translation);
                    PrimitiveRenderer.DrawBoundingBox(matrix);    
                }
            }

            if (Selection.ActiveNode is TriggerPhantomInstanceNode { Definition: TriggerPhantomDefinitionNode def } phantom)
            {
                Matrix4x4.Decompose(phantom.WorldMatrix, out Vector3 worldScale, out Quaternion worldRotation,
                    out Vector3 worldTranslation);

                Vector3 scale = new Vector3(def.WidthRadius, def.Height, def.Depth) * worldScale;
                Matrix4x4 matrix = Matrix4x4.CreateScale(scale) * Matrix4x4.CreateFromQuaternion(worldRotation) *
                                   Matrix4x4.CreateTranslation(worldTranslation);
                PrimitiveRenderer.DrawBoundingBox(matrix);
            }

            if (Selection.ActiveNode is SeInstanceTransformNode transform)
            {
                PrimitiveRenderer.DrawBoundingBox(transform.WorldMatrix);
            }

            if (_navigation != null)
            {

                if (_selectedRoute != null && _navRenderMode == NavRenderMode.Route)
                {
                    Vector3[] colors = new Vector3[]
                    {
                        new Vector3(1.0f, 0.0f, 0.0f),
                        new Vector3(0.0f, 1.0f, 0.0f),
                        new Vector3(0.0f, 0.0f, 1.0f),
                        new Vector3(1.0f, 1.0f, 1.0f)
                    };
                    
                    foreach (NavWaypoint waypoint in _selectedRoute.Waypoints)
                    {
                        PrimitiveRenderer.DrawLine(waypoint.Pos, waypoint.Pos + waypoint.Up * 4.0f, new Vector3(209.0f / 255.0f, 209.0f / 255.0f, 14.0f / 255.0f));
                        PrimitiveRenderer.DrawLine(waypoint.Pos, waypoint.Pos + waypoint.Dir * 4.0f, new Vector3(0.0f, 1.0f, 0.0f));

                        
                        
                        if (_selectedWaypoint == waypoint)
                        {
                            PrimitiveRenderer.DrawLine(waypoint.Pos, waypoint.Pos + waypoint.FromLinks[0].FromToNormal * 8.0f, new Vector3(1.0f, 0.0f, 0.0f));
                            
                            if (waypoint.UnknownWaypoint != null)
                                PrimitiveRenderer.DrawLine(waypoint.Pos, waypoint.UnknownWaypoint.Pos, new Vector3(0.1f, 0.2f, 0.3f));   
                        
                            for (int j = 0; j < waypoint.FromLinks[0].CrossSection.Count - 1; ++j)
                            {
                                PrimitiveRenderer.DrawLine(waypoint.FromLinks[0].CrossSection[j], waypoint.FromLinks[0].CrossSection[j + 1], colors[j]);
                            }   
                        }
                    }
                }

                
                
                if (_selectedRacingLine < _navigation.RacingLines.Count && _selectedRacingLine >= 0 && _navRenderMode == NavRenderMode.RacingLine)
                {
                    NavRacingLine line = _navigation.RacingLines[_selectedRacingLine];


                    Vector3 markerColor = new Vector3(209.0f / 255.0f, 209.0f / 255.0f, 14.0f / 255.0f);
                    Vector3 crossSectionColor = new Vector3(14.0f / 255.0f, 14.0f / 255.0f, 228.0f / 255.0f);
                    Vector3 linkColor = new Vector3(215.0f / 255.0f, 14.0f / 255.0f, 255.0f / 255.0f);
                    
                    
                    for (int i = 1; i < line.Segments.Count + 1; ++i)
                    {
                        NavRacingLineSeg prev = line.Segments[i - 1];
                        NavRacingLineSeg next = line.Segments[i % line.Segments.Count];
                        
                        NavWaypointLink prevLink = prev.Link!;
                        NavWaypointLink nextLink = next.Link!;
                        
                        PrimitiveRenderer.DrawLine(prev.RacingLine, next.RacingLine, Vector3.One);
                        
                        PrimitiveRenderer.DrawLine(prevLink.Left, nextLink.Left, linkColor);
                        PrimitiveRenderer.DrawLine(prevLink.Right, nextLink.Right, linkColor);
                        
                        PrimitiveRenderer.DrawLine(prevLink.Left, prevLink.Right, crossSectionColor);
                        
                        PrimitiveRenderer.DrawLine(prevLink.From!.Pos, prevLink.To!.Pos, linkColor);
                        
                        PrimitiveRenderer.DrawLine(prevLink.From!.Pos, prevLink.From!.Pos + prevLink.From!.Up * 4.0f, markerColor);
                    }
                    
                    
                    
                    // foreach (NavRacingLineSeg segment in line.Segments)
                    // {
                    //     NavWaypoint waypoint = segment.Link!.From!;
                    //     var left = segment.Link!.Left;
                    //     var right = segment.Link!.Right;
                    //     
                    //     LineRenderPrimitives.DrawLine(left, right);
                    //     
                    //     //LineRenderPrimitives.DrawLine(waypoint.Pos, waypoint.Pos + waypoint.Up * 4.0f);
                    //     //LineRenderPrimitives.DrawLine(segment.Link!.From!.Pos, segment.Link!.To!.Pos);
                    // }
                }
                
                


                // foreach (NavWaypoint waypoint in _navData.Waypoints)
                // {
                //     //var rotation = Matrix4x4.CreateLookTo(Vector3.Zero, waypoint.Dir, waypoint.Up);
                //     //var scale = Matrix4x4.CreateScale(Vector3.One);
                //     //var translation = Matrix4x4.CreateTranslation(waypoint.Pos);
                //     LineRenderPrimitives.DrawBoundingBox(waypoint.Pos, Vector3.One);
                // }
            }

            PrimitiveRenderer.EndPrimitiveScene();
        }

        if (Selection.ActiveNode is SeInstanceSplineNode spline)
        {
            _splineTool.Target = spline;
            _splineTool.OnRender();
        }

        GL.Disable(EnableCap.Blend);
    }

    private SplineTool _splineTool = new SplineTool();
    
    
    private void MeshTest()
    {
        if (SceneManager.DisableRendering) return;
        if (SceneManager.RenderNavigationOnly)
        {
            SlRenderer.Framebuffer.Bind();
            GL.ClearColor(0.0f, 0.0f, 0.0f, 1.0f);
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit | ClearBufferMask.StencilBufferBit);
            GL.ClearBuffer(ClearBuffer.Color, 1, TransparentColorData);
            
            SceneCamera.Active.RecomputeMatrixData();
            SlRenderer.SetConstantBuffer(SlRenderBuffers.ViewProjection, SceneCamera.Active.MatrixData);
            RenderPrimitives();
            SlRenderer.Framebuffer.Unbind();
            return;
        }
        
        RecomputeAllWorldMatrices();

        SlRenderer.Framebuffer.Bind();

        SeFogInstanceNode? fog = _database.GetNodesOfType<SeFogInstanceNode>().FirstOrDefault();
        if (fog != null)
        {
            GL.ClearColor(new Color4(
                fog.FogColour.X * fog.FogColourIntensity,
                fog.FogColour.Y * fog.FogColourIntensity,
                fog.FogColour.Z * fog.FogColourIntensity,
                fog.FogColour.W * fog.FogColourIntensity
            ));
        }
        else
        {
            GL.ClearColor(new Color4(65, 106, 160, 255));
        }

        GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit | ClearBufferMask.StencilBufferBit);
        GL.ClearBuffer(ClearBuffer.Color, 1, TransparentColorData);


        // GL.Enable(EnableCap.Blend);
        GL.Disable(EnableCap.CullFace);
        GL.Enable(EnableCap.DepthTest);
        // GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);


        Shader shader = SlRenderer.DefaultShader;
        shader.Bind();
        
        SceneCamera.Active.RecomputeMatrixData();
        SlRenderer.SetConstantBuffer(SlRenderBuffers.ViewProjection, SceneCamera.Active.MatrixData);
        
        Vector3 ambcol = Vector3.One;
        Vector3 suncol = Vector3.One;

        var lights = _database.GetNodesOfType<SeInstanceLightNode>();
        SeInstanceLightNode? ambientLight =
            lights.Find(light => light.LightType == SeInstanceLightNode.SeLightType.Ambient);
        SeInstanceLightNode? sunLight =
            lights.Find(light => light.LightType == SeInstanceLightNode.SeLightType.Directional);

        if (ambientLight != null)
        {
            ambcol = ambientLight.Color * ambientLight.IntensityMultiplier;
        }

        if (sunLight != null)
        {
            suncol = sunLight.Color; // * sunLight.IntensityMultiplier;
            var dir = Vector3.Transform(Vector3.UnitZ, sunLight.Rotation);
            shader.SetVector3("gSun", ref dir);
        }

        shader.SetVector3("gLightAmbient", ref ambcol);
        shader.SetVector3("gSunColor", ref suncol);

        var nodes = _database.FindNodesThatDeriveFrom<SeInstanceEntityNode>();
        nodes.Sort((a, z) => a.RenderLayer - z.RenderLayer);

        foreach (var node in nodes)
        {
            if (!node.IsVisible()) continue;
            if (node.Definition is not SeDefinitionEntityNode entity) continue;

            bool isSelected = false;
            SeGraphNode? parent = node;
            while (parent != null)
            {
                isSelected |= parent == Selection.ActiveNode;
                parent = parent.Parent;
            }

            RenderEntity(node, entity, false);
            if (isSelected)
                RenderEntity(node, entity, true);
        }
        
        GL.UseProgram(0);
        RenderPrimitives();
        SlRenderer.Framebuffer.Unbind();
    }

    private Column? _selectedTrackData;
    private Column? _selectedRacerData;

    private bool RenderExcelTabContents(Worksheet? worksheet, ref Column? selectedExcelColumn)
    {
        ImGui.BeginChild("Left Pane", new Vector2(150.0f, 0.0f), ImGuiChildFlags.Border | ImGuiChildFlags.ResizeX);
        if (worksheet != null)
        {
            foreach (Column column in worksheet.Columns)
            {
                if (ImGui.Selectable(column.GetString("DisplayName")))
                    selectedExcelColumn = column;
            }
        }

        ImGui.EndChild();
        ImGui.SameLine();

        ImGui.BeginGroup();

        ImGui.BeginChild("Item View", new Vector2(0.0f, -ImGui.GetFrameHeightWithSpacing()));

        if (selectedExcelColumn != null)
        {
            ImGui.Text(selectedExcelColumn.GetString("DisplayName"));
            ImGui.Separator();

            foreach (Cell cell in selectedExcelColumn.Cells)
            {
                string? value = cell.Value.ToString();
                if (value == null) continue;

                string name = ExcelPropertyNameLookup.GetPropertyName(cell.Name) ?? cell.Name.ToString();
                ImGui.InputText(name, ref value, 32);
            }
        }

        ImGui.EndChild();

        bool shouldOpen = ImGui.Button("Open");

        ImGui.SameLine();
        ImGui.Button("Delete");

        ImGui.EndGroup();

        return shouldOpen;
    }

    private void RenderContextMenu(SeNodeBase? node)
    {
    }

    private void RenderProjectSelector()
    {
        bool p_open = false;

        ImGui.SetNextWindowPos(ImGui.GetMainViewport().GetCenter(), ImGuiCond.Appearing, new Vector2(0.5f, 0.5f));
        ImGui.SetNextWindowSize(ImGui.GetMainViewport().Size);

        if (ImGui.Begin("Projects", ref p_open,
                ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoMove | ImGuiWindowFlags.Modal |
                ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoDocking))
        {
            if (ImGui.BeginTabBar("##Tabs", ImGuiTabBarFlags.None))
            {
                if (ImGui.BeginTabItem("Tracks"))
                {
                    if (RenderExcelTabContents(_trackData.GetWorksheet("Tracks"), ref _selectedTrackData))
                    {
                        string? directory = _selectedTrackData?.GetString("TrackDirectory");
                        if (!string.IsNullOrEmpty(directory) && directory != "(unused)")
                        {
                            Console.WriteLine("OPENING TRACK: " + directory);
                            
                            
                            SceneManager.LoadScene($"levels/{directory}/{directory}");
                            OnWorkspaceLoad();
                            SetupNavigationRendering();
                        }
                    }

                    ImGui.EndTabItem();
                }

                if (ImGui.BeginTabItem("Racers"))
                {
                    RenderExcelTabContents(_racerData.GetWorksheet("Racers"), ref _selectedRacerData);
                    ImGui.EndTabItem();
                }

                ImGui.EndTabBar();
            }
        }

        ImGui.End();
    }

    protected override void OnRenderFrame(FrameEventArgs e)
    {
        base.OnRenderFrame(e);

        GL.ClearColor(new Color4(65, 106, 160, 255));
        GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit | ClearBufferMask.StencilBufferBit);

        _controller.Update(this, (float)e.Time);

        if (_database != null)
        {
            RenderMainDockWindow();
            RenderWorkspace();

            if (_requestedWorkspaceClose)
                TriggerCloseWorkspace();

            if (KeyboardState.IsKeyDown(Keys.Delete) && Selection.ActiveNode != null)
                _requestedNodeDeletion = Selection.ActiveNode;

            if (_requestedNodeDeletion != null)
            {
                if (_requestedNodeDeletion == Selection.ActiveNode)
                    Selection.ActiveNode = null;
                
                var children = _requestedNodeDeletion.FindDescendantsThatDeriveFrom<SeGraphNode>();
                children.Add(_requestedNodeDeletion);
                foreach (SeGraphNode node in children)
                    _database.RemoveNode(node.Uid);
                _requestedNodeDeletion = null;
            }
        }
        else
        {
            RenderProjectSelector();
        }

        GL.Viewport(0, 0, ClientSize.X, ClientSize.Y);
        _controller.Render();
        ImGuiController.CheckGLError("End of frame");

        if (_database != null)
        {
            if (_localSceneFrameWidth > 0 && _localSceneFrameHeight > 0)
                SlRenderer.Framebuffer.Resize((int)_localSceneFrameWidth, (int)_localSceneFrameHeight);
            MeshTest();
        }

        SwapBuffers();
    }
    
    public enum TransformMode
    {
        None,
        Translate,
        Rotate,
        Scale
    }

    public enum AxisLock
    {
        None,
        X,
        Y,
        Z
    }
    
    private TransformMode _transformMode = TransformMode.None;
    private AxisLock _axisLock = AxisLock.None;
    private int renderLineIndex = 0;
    
    protected override void OnTextInput(TextInputEventArgs e)
    {
        base.OnTextInput(e);

        char c = (char)e.Unicode;
        if (c is >= 'A' and <= 'Z')
            c -= 'A';

        if (_navigation != null)
        {
            int oldRenderLineIndex = renderLineIndex;

            if (c == 'n')
            {
                SceneManager.RenderNavigationOnly = !SceneManager.RenderNavigationOnly;
            }
            
            if (c == 'e')
            {
                renderLineIndex = (renderLineIndex + 1) % _navigation.RacingLines.Count;

                //Console.WriteLine($"switching to racing line {renderLineIndex} : permissions {_navData.RacingLines[renderLineIndex].Permissions}");
            }

            if (c == 'q')
            {
                renderLineIndex--;
                if (renderLineIndex == -1)
                    renderLineIndex = _navigation.RacingLines.Count - 1;

                //Console.WriteLine($"switching to racing line {renderLineIndex} : permissions {_navData.RacingLines[renderLineIndex].Permissions}");
            }

            if (oldRenderLineIndex != renderLineIndex)
            {
                // _renderLineFolders[oldRenderLineIndex].BaseFlags &= ~1;
                // _renderLineFolders[renderLineIndex].BaseFlags |= 1;
                //
                // _selected = _renderLineFolders[renderLineIndex];
            }
        }


        // if (_selected != null)
        // {
        //     switch (c)
        //     {
        //         case 'g': 
        //             _interactMode = InteractMode.Translate;
        //             break;
        //         case 'r':
        //             _interactMode = InteractMode.Rotate;
        //             break;
        //         case 's':
        //             _interactMode = InteractMode.Scale;
        //             break;
        //         case 'x':
        //             _axisLock = AxisLock.X;
        //             break;
        //         case 'y':
        //             _axisLock = AxisLock.Y;
        //             break;
        //         case 'z':
        //             _axisLock = AxisLock.Z;
        //             break;
        //     }
        // }
        //
        // Console.WriteLine($"[Interact]={_interactMode}, [AxisLock]={_axisLock}");
        //
        //
        //

        Vector3 delta = Vector3.Zero;
        const float speed = 1.0f;
        switch (e.Unicode)
        {
            case 87:
                delta = new Vector3(0.0f, 0.0f, 1.0f) * speed;
                break;
            case 83:
                delta = new Vector3(0.0f, 0.0f, -1.0f) * speed;
                break;
            case 119:
                delta = new Vector3(0.0f, -1.0f, 0.0f) * speed;
                break;
            case 97:
                delta = new Vector3(1.0f, 0.0f, 0.0f) * speed;
                break;
            case 115:
                delta = new Vector3(0.0f, 1.0f, 0.0f) * speed;
                break;
            case 100:
                delta = new Vector3(-1.0f, 0.0f, 0.0f) * speed;
                break;
        }

        if (delta != Vector3.Zero)
        {
            SceneCamera.Active.TranslateLocal(delta);
        }

        _controller.PressChar((char)e.Unicode);
    }

    protected override void OnMouseDown(MouseButtonEventArgs e)
    {
        base.OnMouseDown(e);

        _transformMode = TransformMode.None;
        _axisLock = AxisLock.None;

        Console.WriteLine($"[Interact]={_transformMode}, [AxisLock]={_axisLock}");
    }

    protected override void OnMouseWheel(MouseWheelEventArgs e)
    {
        base.OnMouseWheel(e);

        _controller.MouseScroll(e.Offset);
    }
}