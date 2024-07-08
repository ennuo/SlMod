using System.Numerics;
using OpenTK.Graphics.OpenGL;
using SeEditor.Graphics.OpenGL;
using SlLib.Resources.Model;

namespace SeEditor.Renderer;

public static class LineRenderPrimitives
{
    private static int _cubeArrayObject;
    private static int _cubeIndexObject;
    private static int _cubeBufferObject;
    
    private static int _simpleProgram;
    private static int _worldMatrixLocation;
    private static int _projectionMatrixLocation;
    private static int _viewMatrixLocation;

    public static void BeginPrimitiveScene(Matrix4x4 view, Matrix4x4 projection)
    {
        GL.UseProgram(_simpleProgram);
        
        GlUtil.UniformMatrix4(_viewMatrixLocation, ref view);
        GlUtil.UniformMatrix4(_projectionMatrixLocation, ref projection);
        
        //GL.LineWidth(5.0f);
        //GL.PolygonMode(MaterialFace.FrontAndBack, PolygonMode.Line);   
    }

    public static void EndPrimitiveScene()
    {
        //GL.PolygonMode(MaterialFace.FrontAndBack, PolygonMode.Fill);
        GL.UseProgram(0);
    }

    public static void DrawBoundingBox(Vector3 position, Vector3 scale)
    {
        Matrix4x4 world = Matrix4x4.CreateTranslation(position) * Matrix4x4.CreateScale(scale);
        DrawBoundingBox(world);
    }
    
    public static void DrawBoundingBox(Matrix4x4 world)
    {
        GlUtil.UniformMatrix4(_worldMatrixLocation, ref world);
        
        GL.BindVertexArray(_cubeArrayObject);
        
        GL.DrawElements(PrimitiveType.LineLoop, 4, DrawElementsType.UnsignedShort, 0);
        GL.DrawElements(PrimitiveType.LineLoop, 4, DrawElementsType.UnsignedShort, (4 * sizeof(short)));
        GL.DrawElements(PrimitiveType.Lines, 8, DrawElementsType.UnsignedShort, (8 * sizeof(short)));
        
        GL.BindVertexArray(0);
    }
    
    public static void OnStartRenderer()
    {
        // Create program
        {
            _simpleProgram = ImGuiController.CreateProgram("Simple Vertex Program",
                File.ReadAllText(@"D:\projects\slmod\SeEditor\Data\Shaders\simple.vert"),
                File.ReadAllText(@"D:\projects\slmod\SeEditor\Data\Shaders\simple.frag"));
            
            _worldMatrixLocation = GL.GetUniformLocation(_simpleProgram, "gWorld");
            _viewMatrixLocation = GL.GetUniformLocation(_simpleProgram, "gView");
            _projectionMatrixLocation = GL.GetUniformLocation(_simpleProgram, "gProjection");
        }
        
        // Initialize cube vertex object
        {
            float[] vertices = 
            [
                -0.5f, -0.5f, -0.5f, 1.0f,
                0.5f, -0.5f, -0.5f, 1.0f,
                0.5f,  0.5f, -0.5f, 1.0f,
                -0.5f,  0.5f, -0.5f, 1.0f,
                -0.5f, -0.5f,  0.5f, 1.0f,
                0.5f, -0.5f,  0.5f, 1.0f,
                0.5f,  0.5f,  0.5f, 1.0f,
                -0.5f,  0.5f,  0.5f, 1.0f,
            ];

            short[] elements =
            [
                0, 1, 2, 3,
                4, 5, 6, 7,
                0, 4, 1, 5, 2, 6, 3, 7
            ];

            _cubeArrayObject = GL.GenVertexArray();
            GL.BindVertexArray(_cubeArrayObject);

            _cubeIndexObject = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, _cubeIndexObject);
            GL.BufferData(BufferTarget.ElementArrayBuffer, elements.Length * sizeof(short), elements, BufferUsageHint.StaticDraw);

            _cubeBufferObject = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ArrayBuffer, _cubeBufferObject);
            GL.BufferData(BufferTarget.ArrayBuffer, vertices.Length * sizeof(float), vertices, BufferUsageHint.StaticDraw);
            
            GL.EnableVertexAttribArray(SlVertexUsage.Position);
            GL.VertexAttribPointer(SlVertexUsage.Position, 4, VertexAttribPointerType.Float, false, 0, IntPtr.Zero);
            
            GL.BindVertexArray(0);
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, 0);
            GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
        }
        
    }
}