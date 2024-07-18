using System.Globalization;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using ImGuiNET;
using OpenTK.Graphics.OpenGL;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;
using SeEditor.Editor.Menu;
using SeEditor.Graphics.ImGui;
using SeEditor.Graphics.OpenGL;
using SeEditor.Managers;
using SeEditor.Renderer;
using SeEditor.Utilities;
using SharpGLTF.Schema2;
using SlLib.Enums;
using SlLib.Excel;
using SlLib.IO;
using SlLib.Lookup;
using SlLib.Resources;
using SlLib.Resources.Database;
using SlLib.Resources.Model;
using SlLib.Resources.Model.Commands;
using SlLib.Resources.Scene;
using SlLib.Resources.Scene.Definitions;
using SlLib.Resources.Scene.Instances;
using SlLib.Serialization;
using SlLib.SumoTool;
using SlLib.SumoTool.Siff;
using SlLib.SumoTool.Siff.NavData;
using SlLib.Utilities;
using Buffer = System.Buffer;
using PrimitiveType = OpenTK.Graphics.OpenGL.PrimitiveType;
using Quaternion = System.Numerics.Quaternion;
using TextureWrapMode = OpenTK.Graphics.OpenGL.TextureWrapMode;
using Vector2 = System.Numerics.Vector2;
using Vector3 = System.Numerics.Vector3;
using Vector4 = System.Numerics.Vector4;

namespace SeEditor;

public class MainWindow : GameWindow
{
    private ImGuiController _controller;


    private SeGraphNode? _selected;
    private SlResourceDatabase? _workspaceDatabaseFile;
    private ExcelData _racerData;
    private ExcelData _trackData;
    private EditorFramebuffer _framebuffer;
    
    private int _program;

    private int _programWorldLocation;
    private int _programCameraViewLocation;
    private int _programCameraProjectionLocation;
    private int _programViewPos;
    
    private int _programSkeletonLocation;
    private int _programIsSkinnedLocation;
    private int _programJointsLocation;
    private int _programEntityLocation;

    private int _programHasColorStreamLocation;
    private int _programColorLocation;
    private int _programColorMulLocation;
    private int _programColorAddLocation;
    private int _programLightAmbientLocation;
    private int _programSunColorLocation;
    private int _programSunLocation;

    private int _programHasDiffuseTextureLocation;
    private int _programHasEmissiveTextureLocation;
    private int _programDiffuseSamplerLocation;
    private int _programEmissiveSamplerLocation;
    
    private bool _quickstart = true;
    
    private Navigation? _navData;
    private SlModel _breadcrumbModel;
    
    public MainWindow(string title, int width, int height) :
        base(GameWindowSettings.Default, new NativeWindowSettings { ClientSize = (width, height), Title = title })
    {
        VSync = VSyncMode.On;
    }
    
    protected override void OnLoad()
    {
        Title += $": OpenGL Version: {GL.GetString(StringName.Version)}";
        _controller = new ImGuiController(ClientSize.X, ClientSize.Y);
        ImGui.GetStyle().WindowMenuButtonPosition = ImGuiDir.None;
        ImGui.GetIO().ConfigDragClickToInputText = true;
        ImGui.GetIO().ConfigWindowsMoveFromTitleBarOnly = true;

        SlFile.AddGameDataFolder("F:/sart/game/pc");

        _racerData = SlFile.GetExcelData("gamedata/racers") ??
                     throw new FileNotFoundException("Could not load racer data!");
        _trackData = SlFile.GetExcelData("gamedata/tracks") ??
                     throw new FileNotFoundException("Could not load track data!");
        
        _framebuffer = new EditorFramebuffer(ClientSize.X, ClientSize.Y);
        LineRenderPrimitives.OnStartRenderer();
        SetupDefaultShaders();
        
        if (_quickstart)
        {
            // _workspaceDatabaseFile = SlFile.GetSceneDatabase("levels/SeasideHill/SeasideHill", "wu") ??
            //                          throw new FileNotFoundException("Could not load quickstart database!");
            _workspaceDatabaseFile = SlFile.GetSceneDatabase("levels/seasidehill2/seasidehill2") ??
                                     throw new FileNotFoundException("Could not load quickstart database!");

            var instances = _workspaceDatabaseFile.GetNodesOfType<WeaponPodInstanceNode>();
            foreach (var instance in instances)
                instance.Message = WeaponPodMessage.Revenge;


            // byte[] navFile = SlFile.GetFile("levels/examples/examples.navpc") ??
            //                  throw new FileNotFoundException("Could not load quickstart navigation!");
            

            byte[] navFile = SlFile.GetFile("levels/seasidehill2/seasidehill2.navpc") ??
                             throw new FileNotFoundException("Could not load quickstart navigation!");
            SiffFile ksiffNavFile = SiffFile.Load(SlPlatform.Win32.GetDefaultContext(), navFile);
            if (!ksiffNavFile.HasResource(SiffResourceType.Navigation))
                throw new SerializationException("KSiff file doesn't contain navigation data!");
            _navData = ksiffNavFile.LoadResource<Navigation>(SiffResourceType.Navigation);
            
            var importer =
                new SlModelImporter(new SlImportConfig(_workspaceDatabaseFile!, "F:/sart/breadcrumb.glb"));

            _breadcrumbModel= importer.Import();
            _workspaceDatabaseFile!.AddResource(_breadcrumbModel);
            _breadcrumbModel = _workspaceDatabaseFile.FindResourceByHash<SlModel>(_breadcrumbModel.Header.Id)!;
            
            var definition = SeDefinitionNode.CreateObject<SeDefinitionEntityNode>();
            definition.UidName = _breadcrumbModel.Header.Name;
            _workspaceDatabaseFile!.RootDefinitions.Add(definition);
            definition.Model = new SlResPtr<SlModel>(_breadcrumbModel);
            
            
            var folder = SeInstanceNode.CreateObject<SeInstanceFolderNode>();
            folder.Definition = SeDefinitionFolderNode.Default;
            folder.UidName = "Racing Lines";
            folder.Parent = SeInstanceSceneNode.Default;
            
            _workspaceDatabaseFile!.AddNode(folder);
            _workspaceDatabaseFile!.AddNode(definition);
            
            
            
            Console.WriteLine($"Track has {_navData.RacingLines.Count} racing lines");
            Console.WriteLine($"Track has {_navData.Waypoints.Count} waypoints");
            
            for (int racingLineIndex = 0; racingLineIndex < _navData.RacingLines.Count; ++racingLineIndex)
            {
                var subfolder = SeInstanceNode.CreateObject<SeInstanceFolderNode>();
                subfolder.Definition = SeDefinitionFolderNode.Default;
                subfolder.UidName = $"Racing Line {racingLineIndex}";
                subfolder.Parent = folder;
                //if (racingLineIndex != 0)
                subfolder.BaseFlags &= ~1;
                //_renderLineFolders.Add(subfolder);
                
                _workspaceDatabaseFile!.AddNode(subfolder);
                
                
                NavRacingLine line = _navData.RacingLines[racingLineIndex];
                for (int lineSegmentIndex = 0; lineSegmentIndex < line.Segments.Count; ++lineSegmentIndex)
                {
                    NavRacingLineSeg segment = line.Segments[lineSegmentIndex];
                    var instance = SeInstanceNode.CreateObject<SeInstanceEntityNode>();
                    instance.Flags = 0;
                        
                    definition.Instances.Add(instance);
                    instance.Definition = definition;
                    instance.RenderLayer = 127;
                    instance.UidName = $"racingline_{racingLineIndex}_seg_{lineSegmentIndex}";


                    NavWaypoint? waypoint = segment.Link?.From;
                    if (waypoint != null)
                    {
                        var rotation =
                            Quaternion.CreateFromRotationMatrix(Matrix4x4.CreateLookAt(waypoint.Pos,
                                waypoint.Pos + waypoint.Dir, waypoint.Up));
                        rotation = Quaternion.Conjugate(rotation);

                        instance.Rotation = rotation;
                    }
                
                    instance.Translation = segment.RacingLine;
                
                    Matrix4x4 local =
                        Matrix4x4.CreateScale(instance.Scale) *
                        Matrix4x4.CreateFromQuaternion(instance.Rotation) *
                        Matrix4x4.CreateTranslation(instance.Translation);

                    instance.WorldMatrix = local;
                
                    definition.Model = new SlResPtr<SlModel>(_breadcrumbModel);
                        
                    _workspaceDatabaseFile!.RootDefinitions.Add(definition);

                    instance.Parent = subfolder;
                    _workspaceDatabaseFile.AddNode(instance);
                }
            }
            
            folder = SeInstanceNode.CreateObject<SeInstanceFolderNode>();
            folder.Definition = SeDefinitionFolderNode.Default;
            folder.UidName = "Spatial Groups";
            folder.Parent = SeInstanceSceneNode.Default;
            
            _workspaceDatabaseFile!.AddNode(folder);
            
            for (int spatialGroupIndex = 0; spatialGroupIndex < _navData.SpatialGroups.Count; ++spatialGroupIndex)
            {
                var subfolder = SeInstanceNode.CreateObject<SeInstanceFolderNode>();
                subfolder.Definition = SeDefinitionFolderNode.Default;
                subfolder.UidName = $"Spatial Group {spatialGroupIndex}";
                subfolder.Parent = folder;
                //if (racingLineIndex != 0)
                subfolder.BaseFlags &= ~1;
                _renderLineFolders.Add(subfolder);
                
                _workspaceDatabaseFile!.AddNode(subfolder);
                
                
                NavSpatialGroup group = _navData.SpatialGroups[spatialGroupIndex];
                for (int waypointLinkIndex = 0; waypointLinkIndex < group.Links.Count; ++waypointLinkIndex)
                {
                    NavWaypointLink link = group.Links[waypointLinkIndex];
                    NavWaypoint? waypoint = link.From;
                    if (waypoint == null) continue;
                    
                    var instance = SeInstanceNode.CreateObject<SeInstanceEntityNode>();
                    instance.Flags = 0;
                        
                    definition.Instances.Add(instance);
                    instance.Definition = definition;
                    instance.RenderLayer = 127;
                    instance.UidName = $"spatialgroup_{spatialGroupIndex}_seg_{waypointLinkIndex}";
                    
                    var rotation =
                        Quaternion.CreateFromRotationMatrix(Matrix4x4.CreateLookAt(waypoint.Pos,
                            waypoint.Pos + waypoint.Dir, waypoint.Up));
                    rotation = Quaternion.Conjugate(rotation);

                    instance.Rotation = rotation;
                
                    instance.Translation = waypoint.Pos;
                
                    Matrix4x4 local =
                        Matrix4x4.CreateScale(instance.Scale) *
                        Matrix4x4.CreateFromQuaternion(instance.Rotation) *
                        Matrix4x4.CreateTranslation(instance.Translation);

                    instance.WorldMatrix = local;
                
                    definition.Model = new SlResPtr<SlModel>(_breadcrumbModel);
                        
                    _workspaceDatabaseFile!.RootDefinitions.Add(definition);

                    instance.Parent = subfolder;
                    _workspaceDatabaseFile.AddNode(instance);
                }
            }
            
            
            
            // _workspaceDatabaseFile = SlFile.GetSceneDatabase("levels/sambadeagua/sambadeagua") ??
            //                          throw new FileNotFoundException("Could not load quickstart database!");
            
            // _workspaceDatabaseFile = SlResourceDatabase.Load(
            //     @"C:\\Program Files (x86)\\Steam\\steamapps\\common\\Sonic & All-Stars Racing Transformed\\Data\\levels\\seasidehill2\\seasidehill2.cpu.spc",
            //     @"C:\\Program Files (x86)\\Steam\\steamapps\\common\\Sonic & All-Stars Racing Transformed\\Data\\levels\\seasidehill2\\seasidehill2.gpu.spc", inMemory: true);
            
            
            //_workspaceDatabaseFile.DumpNodesToFolder("C:/Users/Aidan/Desktop/SeasideHill/");
            
            OnWorkspaceLoad();
        }
    }

    private void OnWorkspaceLoad()
    {
        if (_workspaceDatabaseFile == null) return;

        // var materials = _workspaceDatabaseFile.GetResourcesOfType<SlMaterial2>();
        // foreach (SlMaterial2 material in materials)
        // {
        //     material.PrintBufferLayouts();
        //     foreach (SlSampler sampler in material.Samplers)
        //     {
        //         Console.WriteLine($"{sampler.Header.Name} : {sampler.Index}");
        //     }
        //     Console.WriteLine();
        // }
        
        var textures = _workspaceDatabaseFile.GetResourcesOfType<SlTexture>();
        foreach (SlTexture texture in textures)
        {
            if (texture.ID != 0 || !texture.HasData()) continue;
            texture.ID = GL.GenTexture();
            GL.BindTexture(TextureTarget.Texture2D, texture.ID);

            bool hasMipMaps = texture.Mips > 1;
            if (hasMipMaps)
            {
                GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureBaseLevel, 0); 
                GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMaxLevel, texture.Mips - 1);    
            }
            
            
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.LinearMipmapLinear);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.Repeat);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.Repeat);

            int numFaces = texture.Cubemap ? 6 : 1;
            var info = SlTexturePlatformInfo.Info[(int)texture.Format];
            if (!info.IsValid())
            {
                Console.WriteLine($"UNSUPPORTED TEXTURE FORMAT: {texture.Header.Name} : {texture.Format}");
                continue;
            }
            
            bool isCompressedTexture = info.IsCompressedType();
            
            int dataOffset = 0x80;
            int width = texture.Width, height = texture.Height;
            
            for (int face = 0; face < numFaces; ++face)
            for (int i = 0; i < texture.Mips; ++i)
            {

                var target = TextureTarget.Texture2D;
                if (texture.Cubemap) target = TextureTarget.TextureCubeMapPositiveX + face;

                int size;
                if (isCompressedTexture)
                    size = ((width + 3) / 4) * ((height + 3) / 4) * (info.Stride * 2);
                else
                    size = width * height * ((info.Stride + 7) / 8);
                
                byte[] textureData = new byte[size];
                Buffer.BlockCopy(texture.Data.Array!, texture.Data.Offset + dataOffset, textureData, 0, size);
                
                if (isCompressedTexture)
                {
                    GL.CompressedTexImage2D(target, i, (InternalFormat)info.InternalFormat, width, height, 0, size, textureData);    
                }
                else
                {
                    GL.TexImage2D(target, i, info.InternalFormat, width, height, 0, info.Format, info.Type, textureData);
                }

                dataOffset += size;
                
                width >>>= 1;
                height >>>= 1;

                if (width == 0 && height == 0) break;
                if (width == 0) width = 1;
                if (height == 0) height = 1;
            }
            
            if (!hasMipMaps) GL.GenerateMipmap(GenerateMipmapTarget.Texture2D);
        }

        GL.BindTexture(TextureTarget.Texture2D, 0);

        var models = _workspaceDatabaseFile.GetResourcesOfType<SlModel>();
        foreach (SlModel model in models)
        {
            model.Convert(SlPlatform.Win32);
            if (model.Resource.Segments.Count == 0) continue;
            
            foreach (SlStream stream in model.Resource.PlatformResource.VertexStreams)
            {
                if (stream.VBO != 0) continue;
                stream.VBO = GL.GenBuffer();
                GL.BindBuffer(BufferTarget.ArrayBuffer, stream.VBO);

                byte[] t = new byte[stream.Data.Count];
                stream.Data.CopyTo(t);
                GL.BufferData(BufferTarget.ArrayBuffer, t.Length, t, BufferUsageHint.StaticDraw);
            }

            var indexStream = model.Resource.PlatformResource.IndexStream;
            if (indexStream.VBO == 0)
            {
                indexStream.VBO = GL.GenBuffer();
                GL.BindBuffer(BufferTarget.ElementArrayBuffer, indexStream.VBO);

                byte[] t = new byte[indexStream.Data.Count];
                indexStream.Data.CopyTo(t);
                GL.BufferData(BufferTarget.ElementArrayBuffer, t.Length, t, BufferUsageHint.StaticDraw);
            }

            foreach (SlModelSegment segment in model.Resource.Segments)
            {
                if (segment.VAO != 0) continue;
                segment.VAO = GL.GenVertexArray();

                GL.BindVertexArray(segment.VAO);
                GL.BindBuffer(BufferTarget.ElementArrayBuffer, segment.IndexStream.VBO);
                foreach (SlVertexAttribute attribute in segment.Format.GetFlattenedAttributes())
                {
                    var stream = segment.VertexStreams[attribute.Stream]!;

                    GL.BindBuffer(BufferTarget.ArrayBuffer, stream.VBO);

                    var type = VertexAttribPointerType.Float;

                    bool normalized = false;
                    switch (attribute.Type)
                    {
                        case SlVertexElementType.Float:
                            type = VertexAttribPointerType.Float;
                            break;
                        case SlVertexElementType.Half:
                            type = VertexAttribPointerType.HalfFloat;
                            break;
                        case SlVertexElementType.UByte:
                            type = VertexAttribPointerType.UnsignedByte;
                            break;
                        case SlVertexElementType.UByteN:
                            type = VertexAttribPointerType.UnsignedByte;
                            normalized = true;
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }

                    GL.EnableVertexAttribArray(attribute.Usage);
                    GL.VertexAttribPointer(attribute.Usage, attribute.Count, type, normalized, stream.Stride,
                        (segment.VertexStart * stream.Stride) + attribute.Offset);
                }

                // if (segment.WeightBuffer.Count != 0)
                // {
                //     GL.EnableVertexAttribArray(SlVertexUsage.BlendIndices);
                //     
                //     
                //     GL.EnableVertexAttribArray(SlVertexUsage.BlendWeight);
                //     
                //     
                //     
                //     
                // }


                GL.BindVertexArray(0);
            }
        }
    }
    
    private void TriggerCloseWorkspace()
    {
        EditorCamera_Position = Vector3.Zero;
        EditorCamera_Rotation = Vector3.Zero;
        
        if (_workspaceDatabaseFile == null)
        {
            _requestedWorkspaceClose = false;
            return;
        }

        var textures = _workspaceDatabaseFile.GetResourcesOfType<SlTexture>();
        foreach (SlTexture texture in textures)
        {
            if (texture.ID == 0) continue;
            GL.DeleteTexture(texture.ID);
            texture.ID = 0;
        }

        var models = _workspaceDatabaseFile.GetResourcesOfType<SlModel>();
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
        _workspaceDatabaseFile = null;

        var scene = SeInstanceSceneNode.Default;
        while (scene.FirstChild != null)
            scene.FirstChild.Parent = null;

        GC.Collect();
    }

    private void SetupDefaultShaders()
    {
        _program = ImGuiController.CreateProgram("Master",
            File.ReadAllText(@"D:\projects\slmod\SeEditor\Data\Shaders\default.vert"),
            File.ReadAllText(@"D:\projects\slmod\SeEditor\Data\Shaders\default.frag"));

        _programWorldLocation = GL.GetUniformLocation(_program, "gWorld");
        _programCameraViewLocation = GL.GetUniformLocation(_program, "gView");
        _programCameraProjectionLocation = GL.GetUniformLocation(_program, "gProjection");
        _programViewPos = GL.GetUniformLocation(_program, "gViewPos");
        
        _programHasDiffuseTextureLocation = GL.GetUniformLocation(_program, "gHasDiffuseTexture");
        _programDiffuseSamplerLocation = GL.GetUniformLocation(_program, "gDiffuseTexture");

        _programHasEmissiveTextureLocation = GL.GetUniformLocation(_program, "gHasEmissiveTexture");
        _programEmissiveSamplerLocation = GL.GetUniformLocation(_program, "gEmissiveTexture");

        _programSkeletonLocation = GL.GetUniformLocation(_program, "gSkeleton");
        _programIsSkinnedLocation = GL.GetUniformLocation(_program, "gIsSkinned");
        _programJointsLocation = GL.GetUniformLocation(_program, "gJoints");
        _programEntityLocation = GL.GetUniformLocation(_program, "gEntityID");

        _programHasColorStreamLocation = GL.GetUniformLocation(_program, "gHasColorStream");
        _programColorLocation = GL.GetUniformLocation(_program, "gColour");
        _programColorMulLocation = GL.GetUniformLocation(_program, "gColourMul");
        _programColorAddLocation = GL.GetUniformLocation(_program, "gColourAdd");
        _programLightAmbientLocation = GL.GetUniformLocation(_program, "gLightAmbient");
        _programSunLocation = GL.GetUniformLocation(_program, "gSun");
        _programSunColorLocation = GL.GetUniformLocation(_program, "gSunColor");
    }

    protected override void OnResize(ResizeEventArgs e)
    {
        base.OnResize(e);

        // Tell ImGui of the new size
        _controller.WindowResized(ClientSize.X, ClientSize.Y); 
    }

    private bool _requestedWorkspaceClose = false;
    private SeGraphNode? _requestedNodeDeletion;

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

        ImGui.DockSpace(ImGui.GetID("Dockspace"), Vector2.Zero, ImGuiDockNodeFlags.PassthruCentralNode);
        if (show)
        {
            if (ImGui.BeginMainMenuBar())
            {
                if (ImGui.BeginMenu("File"))
                {
                    if (ImGui.MenuItem("Close Workspace"))
                        _requestedWorkspaceClose = true;

                    if (ImGui.MenuItem("DEBUG SAVE TO SEASIDEHILL"))
                    {
                        _workspaceDatabaseFile?.Save(
                            @"C:\\Program Files (x86)\\Steam\\steamapps\\common\\Sonic & All-Stars Racing Transformed\\Data\\levels\\seasidehill2\\seasidehill2.cpu.spc",
                            @"C:\\Program Files (x86)\\Steam\\steamapps\\common\\Sonic & All-Stars Racing Transformed\\Data\\levels\\seasidehill2\\seasidehill2.gpu.spc", inMemory: true);
                    }

                    if (ImGui.MenuItem("DEBUG SAVE ALL"))
                    {
                        _workspaceDatabaseFile?.Debug_SetNodesFromScene(SeInstanceSceneNode.Default);
                    }
                    
                    ImGui.EndMenu();
                }
                
                
                
                

                // float width = ImGui.GetWindowWidth();
                // float framerate = ImGui.GetIO().Framerate;
                // ImGui.SetCursorPosX(width - 100);
                // ImGui.Text($"({framerate:0.#} FPS)");

                ImGui.EndMainMenuBar();
            }
        }

        ImGui.End();
    }

    private void RecomputeAllWorldMatrices()
    {
        if (_workspaceDatabaseFile == null) return;

        Recompute(SeInstanceSceneNode.Default);
        
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

    private void RenderAttributeMenu(SeGraphNode? node)
    {
        if (node == null) return;
        
        if (node is SeNodeBase n)
        {
            bool isActive = (n.BaseFlags & 0x1) != 0;
            bool isVisible = (n.BaseFlags & 0x2) != 0;
            
            ImGui.Checkbox("###BaseNodeEnabledToggle", ref isActive);
            ImGui.SameLine();
            

            string name = n.ShortName;
            ImGui.PushItemWidth(-1.0f);
            ImGui.InputText("##BaseNodeName", ref name, 255);
            ImGui.PopItemWidth();

            ImGui.InputText(ImGuiHelper.DoLabelPrefix("Tag"), ref n.Tag, 255);

            ImGuiHelper.DoLabelPrefix("Type");
            ImGui.Text(n.Debug_ResourceType.ToString());

            ImGui.Checkbox(ImGuiHelper.DoLabelPrefix("Visible"), ref isVisible);
            
            ImGui.Text(SlUtil.SumoHash(node.Tag).ToString());
            
            n.BaseFlags = (n.BaseFlags & ~1) | (isActive ? 1 : 0);
            n.BaseFlags = (n.BaseFlags & ~2) | (isVisible ? 2 : 0);

            if (ImGui.Button("=debug serialize node="))
            {
                var context = new ResourceSaveContext();
                ISaveBuffer buffer = context.Allocate(n.GetSizeForSerialization(context.Platform, context.Version));
                context.SaveReference(buffer, n, 0);
                (byte[] cpuData, byte[] gpuData) = context.Flush(1);
                
                File.WriteAllBytes("C:/Users/Aidan/Desktop/node.bin", cpuData);
                File.WriteAllBytes("C:/Users/Aidan/Desktop/node.original.bin", _workspaceDatabaseFile!.GetNodeResourceData(n.Uid)!);
            }

            if (ImGui.Button("=debug save to database="))
            {
                _workspaceDatabaseFile?.AddNode(n);
            }
        }
        
        NodeAttributesMenu.Draw(node);
        
        if (node is SeInstanceLightNode ln) SeAttributeMenu.Draw(ln);
        if (node is SeDefinitionCameraNode cn && ImGui.CollapsingHeader("Camera", ImGuiTreeNodeFlags.DefaultOpen))
        {
            ImGui.SliderFloat("Vertical FOV", ref cn.VerticalFov, 0.0f, 120.0f);
            ImGui.InputFloat("Aspect", ref cn.Aspect);
            ImGui.InputFloat("Near Plane", ref cn.NearPlane);
            ImGui.InputFloat("Far Plane", ref cn.NearPlane);
            ImGui.InputFloat2("Orthographic Scale", ref cn.OrthographicScale);

            bool persp = (cn.CameraFlags & 1) != 0;
            ImGui.Checkbox("Perspective", ref persp);
            cn.CameraFlags &= ~1;
            if (persp) cn.CameraFlags |= 1;
        }
    }

    private void SetRenderContextMaterial(SlModelRenderContext context, SlMaterial2 material)
    {
        if (context.Material == material) return;
        context.Material = material;
        
        if (context.Wireframe)
        {
            Vector4 one = Vector4.One;
            Vector4 zero = Vector4.Zero;
            Vector4 col = new Vector4(255.0f / 255.0f, 172.0f / 255.0f, 28.0f / 255.0f, 1.0f);
            
            GL.Uniform1(_programHasColorStreamLocation, 0);
            GL.Uniform1(_programHasDiffuseTextureLocation, 0);
        
            GlUtil.UniformVector3(_programColorAddLocation, ref zero);
            GlUtil.UniformVector3(_programColorMulLocation, ref one);
            GlUtil.UniformVector3(_programColorLocation, ref col);
        
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

            //material.PrintConstantValues();

            Vector4 color = Vector4.One;
            if (material.HasConstant("gDiffuseColour"))
                color = material.GetConstant("gDiffuseColour");

            Vector4 colorMul = Vector4.One;
            if (material.HasConstant("gColourMul"))
                colorMul = material.GetConstant("gColourMul");

            Vector4 colorAdd = Vector4.Zero;
            if (material.HasConstant("gColourAdd"))
                colorAdd = material.GetConstant("gColourAdd");
    

            GlUtil.UniformVector3(_programColorAddLocation, ref colorAdd);
            GlUtil.UniformVector3(_programColorMulLocation, ref colorMul);
            GlUtil.UniformVector3(_programColorLocation, ref color);

            if (emissive == null)
            {
                GL.Uniform1(_programHasEmissiveTextureLocation, 0);
            }
            else
            {
                GL.Uniform1(_programHasEmissiveTextureLocation, 1);
                GL.Uniform1(_programEmissiveSamplerLocation, 1);
                
                GL.ActiveTexture(TextureUnit.Texture1);
                GL.BindTexture(TextureTarget.Texture2D, emissive.ID);
            }

            if (diffuse == null)
            {
                GL.Uniform1(_programHasDiffuseTextureLocation, 0);
            }
            else
            {
                GL.Uniform1(_programHasDiffuseTextureLocation, 1);
                GL.Uniform1(_programDiffuseSamplerLocation, 0);

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
            GlUtil.UniformMatrix4(_programWorldLocation, ref instance.WorldMatrix);
            GL.Uniform1(_programHasColorStreamLocation,
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
                
        SlSkeleton? skeleton = model.Resource.Skeleton;
        

        var context = new SlModelRenderContext
        {
            Wireframe = wireframe,
        };
        
        var data = new SlModelInstanceData
        {
            InstanceWorldMatrix = instance.WorldMatrix,
            InstanceBindMatrix = instance.WorldMatrix
        };
        
        // SetMul = result, right, left
        
        if (model.Resource.EntityIndex != -1 && skeleton != null)
            data.InstanceBindMatrix = skeleton.Joints[model.Resource.EntityIndex].InverseBindPose * data.InstanceBindMatrix;


        context.EntityWorldMatrix = instance.WorldMatrix;
        
        // TestVisibility
            // SlCullSphere* Sphere
            // Matrix4x4* Model (entity world pos, not bind @ 0x0)
            // Matrix4x4* ??? (some camera matrix?)
            // SlModelContext*
            // bool ??? (false)

        context.SceneGraphInstances.Add(data);
        context.Instances = context.SceneGraphInstances;
        
        GL.Uniform1(_programEntityLocation, instance.Uid);
        
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
    
    private void RenderHierarchy(bool definitions)
    {

        if (definitions)
        {
            foreach (SeDefinitionNode definition in _workspaceDatabaseFile?.RootDefinitions)
            {
                DrawTree(definition);
            }
        }
        else
        {
            SeGraphNode? child = SeInstanceSceneNode.Default.FirstChild;
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
            if (_selected == root) flags |= ImGuiTreeNodeFlags.Selected;

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
            else if ((ImGui.IsItemHovered() && ImGui.IsMouseReleased(ImGuiMouseButton.Left)) && !ImGui.IsItemToggledOpen())
                _selected = root;

            

            if (ImGui.BeginPopupContextItem())
            {
                _selected = root;

                if (ImGui.BeginMenu("Create"))
                {
                    if (ImGui.MenuItem("Model"))
                    {
                        var importer =
                            new SlModelImporter(new SlImportConfig(_workspaceDatabaseFile!, "F:/sart/deer.glb"));

                        SlModel model = importer.Import();
                        _workspaceDatabaseFile!.AddResource(model);

                        model = _workspaceDatabaseFile.FindResourceByHash<SlModel>(model.Header.Id)!;
                        
                        var definition = SeDefinitionNode.CreateObject<SeDefinitionEntityNode>();
                        var instance = SeInstanceNode.CreateObject<SeInstanceEntityNode>();

                        definition.UidName = model.Header.Name;
                        
                        definition.Instances.Add(instance);
                        instance.Definition = definition;

                        definition.Model = new SlResPtr<SlModel>(model);
                        
                        _workspaceDatabaseFile!.RootDefinitions.Add(definition);

                        instance.Parent = _selected;
                        
                        OnWorkspaceLoad();
                    }
                    
                    
                    
                    if (ImGui.MenuItem("Folder"))
                    {
                        var folder = SeInstanceNode.CreateObject<SeInstanceFolderNode>();

                        folder.Debug_ResourceType = SlResourceType.SeInstanceFolderNode;
                        folder.Definition = SeDefinitionFolderNode.Default;
                        folder.Parent = _selected;
                    }
                    
                    ImGui.EndMenu();
                }
                    
                
                
                ImGui.Separator();

                if (ImGui.MenuItem("Delete"))
                {
                    _requestedNodeDeletion = _selected;
                    _selected = null;
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

    private void RenderWorkspace()
    {
        ImGui.Begin("Instances");
        RenderHierarchy(false);
        ImGui.End();

        ImGui.Begin("Definitions");
        RenderHierarchy(true);
        ImGui.End();

        ImGui.Begin("Inspector");
        RenderAttributeMenu(_selected);
        ImGui.End();

        if (_selected is SeInstanceNode instance && instance.Definition != null)
        {
            ImGui.Begin("Definition");
            RenderAttributeMenu(instance.Definition);
            ImGui.End();
        }

        ImGui.Begin("3D View");

        _vwidth = ImGui.GetContentRegionAvail().X;
        _vheight = ImGui.GetContentRegionAvail().Y;

        Vector2 pos = ImGui.GetCursorScreenPos();

        var minWindowPoint = new Vector2(pos.X, pos.Y);
        var maxWindowPoint = new Vector2(pos.X + _vwidth, pos.Y + _vheight);

        ImGui.GetWindowDrawList().AddImage(
            _framebuffer.GetRenderTexture(),
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
                UpdateCameraFromMouseInput();
                if (ImGui.IsMousePosValid() && ImGui.IsMouseReleased(ImGuiMouseButton.Left))
                {
                    Vector2 mousePos = ImGui.GetIO().MousePos - pos;
                    Console.WriteLine(mousePos);
                    int pick = _framebuffer.GetEntityPick((int)mousePos.X, (int)_vheight - (int)mousePos.Y);
                    Console.WriteLine(pick);
                    _selected = _workspaceDatabaseFile?.LoadGenericNode(pick);
                }
            }
        }

        ImGui.End();


        //ImGui.ShowDemoWindow();
    }
    
    private static float[] TransparentColorData = [0.0f, 0.0f, 0.0f, 0.0f];
    private void MeshTest()
    {
        RecomputeAllWorldMatrices();
        
        _framebuffer.Bind();

        SeFogInstanceNode? fog = _workspaceDatabaseFile.GetNodesOfType<SeFogInstanceNode>().FirstOrDefault();
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
        
        
        
        
        

        GL.Enable(EnableCap.Blend);
        GL.Disable(EnableCap.CullFace);
        GL.Enable(EnableCap.DepthTest);
        GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
        

        GL.UseProgram(_program);

        InvalidateCameraMatrices();
        GlUtil.UniformMatrix4(_programCameraViewLocation, ref EditorCamera_ViewMatrix);
        GlUtil.UniformMatrix4(_programCameraProjectionLocation, ref EditorCamera_ProjectionMatrix);
        GlUtil.UniformVector3(_programViewPos, ref EditorCamera_Position);
        
        Vector3 ambcol = Vector3.One;
        Vector3 suncol = Vector3.One;

        var lights = _workspaceDatabaseFile.GetNodesOfType<SeInstanceLightNode>();
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
            var dir = Vector3.Transform(Vector3.UnitY, sunLight.Rotation);
            GlUtil.UniformVector3(_programSunLocation, ref dir);
        }

        GlUtil.UniformVector3(_programLightAmbientLocation, ref ambcol);
        GlUtil.UniformVector3(_programSunColorLocation, ref suncol);
        
        var nodes = _workspaceDatabaseFile.FindNodesThatDeriveFrom<SeInstanceEntityNode>();
        nodes.Sort((a, z) => a.RenderLayer - z.RenderLayer);
        
        foreach (var node in nodes)
        {
            if (!node.IsVisible()) continue;
            if (node.Definition is not SeDefinitionEntityNode entity) continue;

            bool isSelected = false;
            SeGraphNode? parent = node;
            while (parent != null)
            {
                isSelected |= parent == _selected;
                parent = parent.Parent;
            }

            RenderEntity(node, entity, false);
            if (isSelected)
                RenderEntity(node, entity, true);
        }

        GL.UseProgram(0);
        
        GL.Disable(EnableCap.DepthTest);
        GL.Clear(ClearBufferMask.DepthBufferBit);
        
        // render locators
        {
            LineRenderPrimitives.BeginPrimitiveScene(EditorCamera_ViewMatrix, EditorCamera_ProjectionMatrix);

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
            
            if (_selected is TriggerPhantomInstanceNode { Definition: TriggerPhantomDefinitionNode def } phantom)
            {
                Matrix4x4.Decompose(phantom.WorldMatrix, out Vector3 worldScale, out Quaternion worldRotation,
                    out Vector3 worldTranslation);
                
                Vector3 scale = new Vector3(def.WidthRadius, def.Height, def.Depth) * worldScale;
                Matrix4x4 matrix = Matrix4x4.CreateScale(scale) * Matrix4x4.CreateFromQuaternion(worldRotation) * Matrix4x4.CreateTranslation(worldTranslation);
                LineRenderPrimitives.DrawBoundingBox(matrix); 
            }
            
            if (_selected is SeInstanceTransformNode transform)
            {
                LineRenderPrimitives.DrawBoundingBox(transform.WorldMatrix);    
            }

            if (_navData != null)
            {
                // if (renderLineIndex < _navData.RacingLines.Count)
                // {
                //     NavRacingLine line = _navData.RacingLines[renderLineIndex];
                //     foreach (NavRacingLineSeg segment in line.Segments)
                //     { 
                //         LineRenderPrimitives.DrawBoundingBox(segment.RacingLine, Vector3.One);
                //     }
                // }
                
                
                // foreach (NavWaypoint waypoint in _navData.Waypoints)
                // {
                //     //var rotation = Matrix4x4.CreateLookTo(Vector3.Zero, waypoint.Dir, waypoint.Up);
                //     //var scale = Matrix4x4.CreateScale(Vector3.One);
                //     //var translation = Matrix4x4.CreateTranslation(waypoint.Pos);
                //     LineRenderPrimitives.DrawBoundingBox(waypoint.Pos, Vector3.One);
                // }
            }
            
            LineRenderPrimitives.EndPrimitiveScene();
        }
        
        GL.Disable(EnableCap.Blend);
        
        _framebuffer.Unbind();
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
                            _workspaceDatabaseFile = SlFile.GetSceneDatabase($"levels/{directory}/{directory}");
                            OnWorkspaceLoad();
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

    private float _vwidth;
    private float _vheight;

    protected override void OnRenderFrame(FrameEventArgs e)
    {
        base.OnRenderFrame(e);

        GL.ClearColor(new Color4(65, 106, 160, 255));
        GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit | ClearBufferMask.StencilBufferBit);

        _controller.Update(this, (float)e.Time);

        if (_workspaceDatabaseFile != null)
        {
            RenderMainDockWindow();
            RenderWorkspace();

            if (_requestedWorkspaceClose)
                TriggerCloseWorkspace();

            if (KeyboardState.IsKeyDown(Keys.Delete) && _selected != null)
                _requestedNodeDeletion = _selected;
            
            if (_requestedNodeDeletion != null)
            {
                var children = _requestedNodeDeletion.FindDescendantsThatDeriveFrom<SeGraphNode>();
                children.Add(_requestedNodeDeletion);
                foreach (SeGraphNode node in children)
                {
                    _workspaceDatabaseFile.RemoveNode(node.Uid);
                    node.Parent = null;
                }
                
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

        if (_workspaceDatabaseFile != null)
        {
            if (_vwidth > 0 && _vheight > 0)
                _framebuffer.Resize((int)_vwidth, (int)_vheight);
            MeshTest();
        }

        SwapBuffers();
    }
    
    public static Matrix4x4 EditorCamera_ProjectionMatrix = Matrix4x4.Identity;
    public static Matrix4x4 EditorCamera_ViewMatrix = Matrix4x4.Identity;
    public static Vector3 EditorCamera_Position = Vector3.Zero;
    public static Vector3 EditorCamera_Rotation = Vector3.Zero;
    public static Matrix4x4 EditorCamera_InvRotation = Matrix4x4.Identity;

    private void InvalidateCameraMatrices()
    {
        EditorCamera_ProjectionMatrix = Matrix4x4.CreatePerspectiveFieldOfView(
            60.0f * MathUtils.Deg2Rad,
            _vwidth / _vheight,
            0.1f,
            20000.0f
        );
        
        EditorCamera_InvRotation =
            Matrix4x4.CreateRotationX(-EditorCamera_Rotation.X) *
            Matrix4x4.CreateRotationY(-EditorCamera_Rotation.Y) *
            Matrix4x4.CreateRotationZ(-EditorCamera_Rotation.Z);

        var translation = Matrix4x4.CreateTranslation(EditorCamera_Position);
        var rotation =
            Matrix4x4.CreateRotationZ(EditorCamera_Rotation.Z) *
            Matrix4x4.CreateRotationY(EditorCamera_Rotation.Y) *
            Matrix4x4.CreateRotationX(EditorCamera_Rotation.X);

        EditorCamera_ViewMatrix = translation * rotation;
    }

    private void UpdateCameraFromMouseInput()
    {
        bool shift = KeyboardState.IsKeyDown(Keys.LeftShift);
        bool middle = MouseState.IsButtonDown(MouseButton.Button3);

        if (shift && middle)
        {
            EditorCamera_Position += Vector3.Transform(new Vector3(MouseState.Delta.X, -MouseState.Delta.Y, 0.0f),
                EditorCamera_InvRotation);
        }
        else if (middle)
        {
            EditorCamera_Rotation.X -= MouseState.Delta.Y * 0.01f;
            EditorCamera_Rotation.Y -= MouseState.Delta.X * 0.01f;
        }
        else if (!shift)
        {
            float delta = MouseState.ScrollDelta.Y * 20.0f;
            EditorCamera_Position += Vector3.Transform(new Vector3(0.0f, 0.0f, delta), EditorCamera_InvRotation);
        }
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
    private List<SeInstanceFolderNode> _renderLineFolders = [];
    
    protected override void OnTextInput(TextInputEventArgs e)
    {
        base.OnTextInput(e);
        
        char c = (char)e.Unicode;
        if (c is >= 'A' and <= 'Z')
            c -= 'A';

        if (_navData != null)
        {
            int oldRenderLineIndex = renderLineIndex;
            
            if (c == 'e')
            {
                renderLineIndex = (renderLineIndex + 1) % _renderLineFolders.Count;
                
                //Console.WriteLine($"switching to racing line {renderLineIndex} : permissions {_navData.RacingLines[renderLineIndex].Permissions}");
            }

            if (c == 'q')
            {
                renderLineIndex--;
                if (renderLineIndex == -1)
                    renderLineIndex = _renderLineFolders.Count - 1;
                
                //Console.WriteLine($"switching to racing line {renderLineIndex} : permissions {_navData.RacingLines[renderLineIndex].Permissions}");
            }

            if (oldRenderLineIndex != renderLineIndex)
            {
                _renderLineFolders[oldRenderLineIndex].BaseFlags &= ~1;
                _renderLineFolders[renderLineIndex].BaseFlags |= 1;
                
                _selected = _renderLineFolders[renderLineIndex];
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
            EditorCamera_Position += Vector3.Transform(delta, EditorCamera_InvRotation);
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