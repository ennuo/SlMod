using System.Drawing;
using System.Numerics;
using System.Runtime.Intrinsics;
using DirectXTexNet;
using ImGuiNET;
using OpenTK.Graphics.OpenGL;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using SeEditor.Graphics.ImGui;
using SeEditor.Managers;
using SeEditor.Renderer;
using SixLabors.ImageSharp.Advanced;
using SlLib.Resources.Database;
using SlLib.SumoTool;
using SlLib.SumoTool.Siff;
using SlLib.SumoTool.Siff.Entry;
using SlLib.SumoTool.Siff.Keyframe;
using SlLib.SumoTool.Siff.Objects;
using SlLib.SumoTool.Siff.Sprites;
using SlLib.Utilities;

using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing;
using SixLabors.ImageSharp.PixelFormats;
using SlLib.SumoTool.Siff.Fonts;
using Vector2 = System.Numerics.Vector2;
using Vector3 = System.Numerics.Vector3;
using Vector4 = System.Numerics.Vector4;
using Buffer = System.Buffer;
using Color = System.Drawing.Color;

namespace SeEditor;

public class Frontend : GameWindow
{
    private ImGuiController _controller;
    
    private KeyframeLibrary _keyframes;
    private ObjectDefLibrary _objects;
    private TexturePack _textures;
    private FontPack _fonts;
    private TextPack _text;
    private SceneLibrary _scenes;
    
    /// <summary>
    ///     The currently active scene.
    /// </summary>
    private Scene _scene;
    private int _sceneIndex = 0;

    private Vector2 _toolScreenOffset;
    private Vector2 _toolScreenDimensions;
    
    public Frontend(int width, int height) : 
        base(GameWindowSettings.Default, new NativeWindowSettings { ClientSize = (width, height), Title = "Sumo Tool - Frontend <OpenGL>" })
    {
        VSync = VSyncMode.On;
    }

    /// <summary>
    ///     Loads a frontend package and activates it.
    /// </summary>
    /// <param name="path">Path to the UI package</param>
    /// <exception cref="FileNotFoundException">Thrown if the package specified is not found</exception>
    /// <exception cref="Exception">Thrown if any other loading error occurs.</exception>
    private void LoadFrontend(string path)
    {
        Console.WriteLine($"Beginning load of UI package at {path}...");
        
        byte[]? packageData = SlFile.GetFile(path);
        if (packageData == null)
            throw new FileNotFoundException($"Could not find sumo tool package at {path}");
        SumoToolPackage package = SumoToolPackage.Load(SlPlatform.Win32.GetDefaultContext(), packageData);
        if (!package.HasLocaleData())
            throw new Exception("Sumo tool package doesn't contain any locale data!");

        SiffFile siff = package.GetLocaleSiff();
        
        Console.WriteLine("Setting up scene manager resources...");
        
        _keyframes = siff.LoadResource<KeyframeLibrary>(SiffResourceType.KeyFrameLibrary);
        _objects = siff.LoadResource<ObjectDefLibrary>(SiffResourceType.ObjectDefLibrary);
        _textures = siff.LoadResource<TexturePack>(SiffResourceType.TexturePack);
        _text = siff.LoadResource<TextPack>(SiffResourceType.TextPack);
        _scenes = siff.LoadResource<SceneLibrary>(SiffResourceType.SceneLibrary);
        _fonts = siff.LoadResource<FontPack>(SiffResourceType.FontPack);
    }

    private void SetActiveScene(Scene scene)
    {
        _scene = scene;
        
        
    }
    
    private float GetObjectZ(int hash)
    {
        (Vector2 _, Vector2 _, Vector2 _, float _, float z) = GetObjectKeyData(hash);
        return z;
    }

    private (Vector2 position, Vector2 size, Vector2 scale, float rotation, float z) GetObjectKeyData(int hash)
    {
        KeyframeEntry? entry = _keyframes.GetKeyframe(hash);
        if (entry == null)
            throw new NullReferenceException("Keyframe hash should not be NULL!");

        KeyframeData keyframe = entry.GetKeyFrame("ON") ?? 
                                entry.GetKeyFrame("POS_1") ?? 
                                entry.GetKeyFrame("LOOP_START") ?? 
                                entry.GetKeyFrame("KEYFRAME_1") ??
                                entry.Data.First();

        // KeyframeData keyframe = entry.Data.First();
        
        return (new Vector2(keyframe.X, keyframe.Y), new Vector2(entry.Width, entry.Height), keyframe.Scale, keyframe.Rotation, keyframe.Z);
    }
    
    private void RenderObject(int hash, ImDrawListPtr drawList, Matrix4x4 pMatrix)
    {
        var def = _objects.GetObjectDef<IObjectDef>(hash);
        (Vector2 pos, Vector2 size, Vector2 scale, float rotation, float z) = GetObjectKeyData(hash);
        if (def == null) return;
        
        pos = (pos + def.Anchor) * _toolScreenDimensions;
        size *= _toolScreenDimensions;
        
        Matrix4x4 local =
            Matrix4x4.CreateScale(new Vector3(scale, 1.0f)) *
            Matrix4x4.CreateRotationZ(rotation) *
            Matrix4x4.CreateTranslation(new Vector3(pos, z));
        Matrix4x4 global = local * pMatrix;
        
        Vector2 p0 = _toolScreenOffset + Vector3.Transform(Vector3.Zero, global).AsVector128().AsVector2();
        Vector2 p1 = _toolScreenOffset + Vector3.Transform(new Vector3(size, 0.0f), global).AsVector128().AsVector2();
        
        switch (def)
        {
            case GroupObject group:
            {
                foreach (int child in group.ObjectHashes)
                    RenderObject(child, drawList, global);
                
                break;
            }
            case TextureObject texture:
            {
                Sprite? sprite = _textures.GetSprite(texture.TextureHash);
                if (sprite == null) break;

                var dim = new Vector2(sprite.Sheet.Width, sprite.Sheet.Height);
                var uv0 = new Vector2(sprite.X, sprite.Y);
                var uv1 = uv0 + new Vector2(sprite.Width, sprite.Height);
                
                uv0 /= dim;
                uv1 /= dim;
                
                drawList.AddImage(sprite.Sheet.TextureId, p0, p1, uv0, uv1);
                
                break;
            }
            case TextObject text:
            {
                Font? font = _fonts.Fonts.Find(f => f.Hash == text.FontHash);
                if (font == null) break;
                
                string value = _text[text.StringHash];
                float x = 0.0f, y;
                y = (float)(font.Ascender - font.Descender);
                foreach (char c in value)
                {
                    CharacterInfo? info = font.Characters.Find(ch => ch.CharCode == (short)c);
                    if (info == null) throw new NullReferenceException("No character!");
                    
                    x += info.PreShift;
                    if (info.HasGraphic != 0)
                    {
                        Sprite? sprite = _textures.GetSprite(info.TextureHash);
                        if (sprite == null) throw new NullReferenceException("No texture!");
                        
                        var dim = new Vector2(sprite.Sheet.Width, sprite.Sheet.Height);
                        var uv0 = new Vector2(sprite.X + info.X, sprite.Y + info.H);
                        var uv1 = uv0 + new Vector2(info.W, info.H);
                        uv0 /= dim;
                        uv1 /= dim;
                        
                        float w = (ushort)info.W;
                        float h = (ushort)info.H;
                    
                        p0 = _toolScreenOffset + Vector3.Transform(new Vector3(x, y + info.YAdjust, 0.0f), global).AsVector128().AsVector2();
                        p1 = _toolScreenOffset + Vector3.Transform(new Vector3(x + w, y + h + info.YAdjust, 0.0f), global).AsVector128().AsVector2();
                        
                        drawList.AddImage(sprite.Sheet.TextureId, p0, p1, uv0, uv1);
                    }

                    x += info.PostShift;
                }
                
                break;
            }
            default:
            {
                break;
            }
        }
    }
    
    private void DrawFrontend()
    {
        ImGui.SetNextWindowSize(new Vector2(_scene.Width, _scene.Height));
        ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, Vector2.Zero);
        if (ImGui.Begin("Layout Preview"))
        {
            ImGui.PopStyleVar();
            ImDrawListPtr drawList = ImGui.GetWindowDrawList();

            _toolScreenOffset = ImGui.GetCursorScreenPos();
            _toolScreenDimensions = ImGui.GetContentRegionAvail();
            foreach (var hash in _scene.Objects)
                RenderObject(hash, drawList, Matrix4x4.Identity);
        }
        ImGui.End();
    }

    void SortObjects(List<int> hashes)
    {
        hashes.Sort((a, z) =>
        {
            IObjectDef defA = _objects.Objects[a];
            IObjectDef defZ = _objects.Objects[z];

            int diff = defZ.Layer.CompareTo(defA.Layer);
            return diff == 0 ? z.CompareTo(a) : diff;
        });
        
        foreach (int hash in hashes)
        {
            IObjectDef def = _objects.Objects[hash];
            if (def is GroupObject group)
                SortObjects(group.ObjectHashes);
        }
    }

    private void DrawMainDockWindow()
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
        
        ImGui.DockSpace(ImGui.GetID("Dockspace"), Vector2.Zero, ImGuiDockNodeFlags.PassthruCentralNode);
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
                    ImGui.EndMenu();
                }
                
                ImGui.EndMainMenuBar();
            }
        }
        
        ImGui.End();
    }

    protected override void OnLoad()
    {
        base.OnLoad();
        _controller = new ImGuiController(ClientSize.X, ClientSize.Y);
        SlFile.AddGameDataFolder("F:/sart/game/pc");
        
        // LoadFrontend("ui/frontend/newfe/mainfe_en.stz");
        LoadFrontend("ui/frontend/commonprojects/commonprojects_en.stz");
        // LoadFrontend("ui/frontend/hud_elements/hud_elements_en.stz");
        // LoadFrontend("ui/frontend/pause_menu/pause_menu_en.stz");
        
        // _scene = _scenes.FindScene("CHARACTER_SELECT") ?? throw new Exception("Could not find character select scene!");
        _scene = _scenes.Scenes.First();
        _sceneIndex = 0;
        SortObjects(_scene.Objects);
        
        Console.WriteLine("Mounting all sprite sheets into GL context...");
        int index = 0;
        foreach (SpriteSheet sheet in _textures.Sheets)
        {
            sheet.TextureId = GL.GenTexture();
            GL.BindTexture(TextureTarget.Texture2D, sheet.TextureId);
            
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter,
                (int)TextureMagFilter.Linear);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter,
                (int)TextureMinFilter.Linear);
            
            byte[] textureData = new byte[sheet.Data.Count - 0x80];
            Buffer.BlockCopy(sheet.Data.Array!, sheet.Data.Offset + 0x80, textureData, 0, textureData.Length);

            if (sheet.Format == DXGI_FORMAT.BC2_UNORM)
            {
                GL.CompressedTexImage2D(TextureTarget.Texture2D, 0, InternalFormat.CompressedRgbaS3tcDxt3Ext, sheet.Width,
                    sheet.Height, 0, textureData.Length, textureData);   
            }
            else if (sheet.Format == DXGI_FORMAT.B8G8R8A8_UNORM)
            {
                GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, sheet.Width, sheet.Height, 0,
                    PixelFormat.Bgra, PixelType.UnsignedByte, textureData);
            }
            else throw new Exception("Unsupported format: " + sheet.Format);
        }
    }

    protected override void OnResize(ResizeEventArgs e)
    {
        base.OnResize(e);
        _controller.WindowResized(ClientSize.X, ClientSize.Y);
        GL.Viewport(0, 0, ClientSize.X, ClientSize.Y);
    }

    protected override void OnTextInput(TextInputEventArgs e)
    {
        base.OnTextInput(e);
        _controller.PressChar((char)e.Unicode);
        
        char c = (char)e.Unicode;
        if (c is >= 'A' and <= 'Z')
            c -= 'A';

        if (c == 'q')
        {
            _sceneIndex = (_sceneIndex + 1) % _scenes.Scenes.Count;
            _scene = _scenes.Scenes[_sceneIndex];
        }

        if (c == 'e')
        {
            _sceneIndex = _sceneIndex - 1;
            if (_sceneIndex < 0)
                _sceneIndex = _scenes.Scenes.Count - 1;
            _scene = _scenes.Scenes[_sceneIndex];
        }
        
    }

    protected override void OnMouseWheel(MouseWheelEventArgs e)
    {
        base.OnMouseWheel(e);
        _controller.MouseScroll(e.Offset);
    }

    protected override void OnRenderFrame(FrameEventArgs args)
    {
        base.OnRenderFrame(args);
        
        GL.ClearColor(new Color4(65, 106, 160, 255));
        GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit | ClearBufferMask.StencilBufferBit);
        
        _controller.Update(this, (float)args.Time);

        DrawMainDockWindow();
        DrawFrontend();
        
        _controller.Render();
        ImGuiController.CheckGLError("End of frame");
        SwapBuffers();
    }
    
    private static Color ColorFromHSV(double hue, double saturation, double value)
    {
        int hi = Convert.ToInt32(Math.Floor(hue / 60)) % 6;
        double f = hue / 60 - Math.Floor(hue / 60);

        value *= 255;
        int v = Convert.ToInt32(value);
        int p = Convert.ToInt32(value * (1 - saturation));
        int q = Convert.ToInt32(value * (1 - f * saturation));
        int t = Convert.ToInt32(value * (1 - (1 - f) * saturation));
        
        return hi switch
        {
            0 => Color.FromArgb(255, v, t, p),
            1 => Color.FromArgb(255, q, v, p),
            2 => Color.FromArgb(255, p, v, t),
            3 => Color.FromArgb(255, p, q, v),
            4 => Color.FromArgb(255, t, p, v),
            _ => Color.FromArgb(255, v, p, q)
        };
    }
}