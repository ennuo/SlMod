using System.Numerics;
using SeEditor.Graphics.OpenGL;
using SeEditor.Renderer.Buffers;
using SlLib.Resources.Model;

namespace SeEditor.Renderer;

public static class SlRenderer
{
    /// <summary>
    ///     The primary framebuffer used for the editor scene.
    /// </summary>
    public static EditorFramebuffer Framebuffer { get; private set; }
    
    /// <summary>
    ///     The width of the framebuffer.
    /// </summary>
    public static int Width { get; private set; }
    
    /// <summary>
    ///     The height of the framebuffer.
    /// </summary>
    public static int Height { get; private set; }
    
    /// <summary>
    ///     The default shader used for all 3D geometry.
    /// </summary>
    public static Shader DefaultShader { get; private set; }

    /// <summary>
    ///     Shared render context for rendering models.
    /// </summary>
    public static SlModelRenderContext Context { get; } = new();
    
    /// <summary>
    ///     All uniform buffers used by the renderer.
    /// </summary>
    private static readonly UniformBuffer[] Buffers = new UniformBuffer[SlRenderBuffers.Count];

    /// <summary>
    ///     Whether the renderer has been initialized or not.
    /// </summary>
    private static bool _initialized;

    /// <summary>
    ///     Sets up the renderer after the GL context has been initialized.
    /// </summary>
    public static void InitializeForOpenGL(int width, int height)
    {
        if (_initialized)
            throw new Exception("SlRenderer has already been initialized!");
        
        Width = width;
        Height = height;
        Framebuffer = new EditorFramebuffer(width, height);
        PrimitiveRenderer.OnStartRenderer();

        Buffers[SlRenderBuffers.CommonModifiers] = new UniformBuffer(0x40, SlRenderBuffers.CommonModifiers);
        Buffers[SlRenderBuffers.ViewProjection] = new UniformBuffer(0x100, SlRenderBuffers.ViewProjection);
        Buffers[SlRenderBuffers.WorldMatrix] = new UniformBuffer(0x40, SlRenderBuffers.WorldMatrix);
        SetConstantBuffer(SlRenderBuffers.CommonModifiers, new ConstantBufferCommonModifiers
        {
            AlphaRef = 0.01f,
            ColorAdd = Vector4.Zero,
            ColorMul = Vector4.One,
            FogMul = 0.0f
        });
        
        DefaultShader = new Shader("Data/Shaders/default.vert", "Data/Shaders/default.frag");
        
        _initialized = true;
    }
    
    public static void SetConstantBuffer(int index, ArraySegment<byte> data)
    {
        Buffers[index].SetData(data);
    }
    
    public static void SetConstantBuffer<T>(int index, T data) where T : struct
    {
        Buffers[index].SetData(data);
    }

    public static void OnResize(int width, int height)
    {
        if (Width != width || Height != height)
            Framebuffer.Resize(width, height);
        
        Width = width;
        Height = height;
    }
}