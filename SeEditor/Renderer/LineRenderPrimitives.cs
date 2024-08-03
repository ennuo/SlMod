using System.Numerics;
using System.Runtime.InteropServices;
using OpenTK.Graphics.OpenGL;
using SeEditor.Graphics.ImGui;
using SeEditor.Graphics.OpenGL;
using SlLib.Resources.Model;

namespace SeEditor.Renderer;

public static class LineRenderPrimitives
{
    private static int _cubeArrayObject;
    private static int _cubeIndexObject;
    private static int _cubeBufferObject;

    private static Shader _simpleShader;
    private static Shader _lineShader;
    
    [StructLayout(LayoutKind.Explicit, Size = 0x18)]
    private struct LineVertex(Vector3 from, Vector3 color)
    {
        [FieldOffset(0x0)]
        public Vector3 From = from;
        [FieldOffset(0xc)]
        public Vector3 Color = color;
    }
    
    private static readonly LineVertex[] SharedLinePool = new LineVertex[65535];
    private static int _numLineVertices;
    
    private static int _lineArrayObject;
    private static int _lineBufferObject;
    
    public static void BeginPrimitiveScene()
    {
        _numLineVertices = 0;
        _simpleShader.Bind();
        
        //GL.LineWidth(5.0f);
        //GL.PolygonMode(MaterialFace.FrontAndBack, PolygonMode.Line);   
    }

    public static void EndPrimitiveScene()
    {
        if (_numLineVertices != 0)
        {
            _lineShader.Bind();
            
            GL.BindVertexArray(_lineArrayObject);
            GL.BindBuffer(BufferTarget.ArrayBuffer, _lineBufferObject);
            GL.BufferSubData(BufferTarget.ArrayBuffer, IntPtr.Zero, _numLineVertices * 0x18, SharedLinePool);
            
            GL.LineWidth(3.0f);
            GL.DrawArrays(PrimitiveType.Lines, 0, _numLineVertices);
            
            GL.BindVertexArray(0);
            GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
        }
        
        //GL.PolygonMode(MaterialFace.FrontAndBack, PolygonMode.Fill);
        GL.UseProgram(0);
    }

    public static void DrawLine(Vector3 from, Vector3 to, Vector3 color)
    {
        SharedLinePool[_numLineVertices++] = new LineVertex(from, color);
        SharedLinePool[_numLineVertices++] = new LineVertex(to, color);
    }

    public static void DrawBoundingBox(Vector3 position, Vector3 scale)
    {
        Matrix4x4 world = Matrix4x4.CreateScale(scale) * Matrix4x4.CreateTranslation(position);
        DrawBoundingBox(world);
    }
    
    public static void DrawBoundingBox(Matrix4x4 world)
    {
        CharmyBee.cbWorldMatrix.SetData(world);
        GL.BindVertexArray(_cubeArrayObject);
        
        GL.DrawElements(PrimitiveType.LineLoop, 4, DrawElementsType.UnsignedShort, 0);
        GL.DrawElements(PrimitiveType.LineLoop, 4, DrawElementsType.UnsignedShort, (4 * sizeof(short)));
        GL.DrawElements(PrimitiveType.Lines, 8, DrawElementsType.UnsignedShort, (8 * sizeof(short)));
        
        GL.BindVertexArray(0);
    }
    
    public static void OnStartRenderer()
    {
        // Create programs
        {
            _simpleShader = new Shader("Data/Shaders/simple.vert", "Data/Shaders/simple.frag");
            _lineShader = new Shader("Data/Shaders/line.vert", "Data/Shaders/line.frag");
        }
        
        // Shared line pool
        _lineArrayObject = GL.GenVertexArray();
        GL.BindVertexArray(_lineArrayObject);

        _lineBufferObject = GL.GenBuffer();
        GL.BindBuffer(BufferTarget.ArrayBuffer, _lineBufferObject);
        GL.BufferData(BufferTarget.ArrayBuffer, SharedLinePool.Length * 0x18, IntPtr.Zero, BufferUsageHint.DynamicDraw);
            
        GL.EnableVertexAttribArray(SlVertexUsage.Position);
        GL.VertexAttribPointer(SlVertexUsage.Position, 3, VertexAttribPointerType.Float, false, 0x18, IntPtr.Zero);
        GL.EnableVertexAttribArray(SlVertexUsage.Color);
        GL.VertexAttribPointer(SlVertexUsage.Color, 3, VertexAttribPointerType.Float, true, 0x18, 0xc);
        
        
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