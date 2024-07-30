using System.Numerics;
using OpenTK.Windowing.GraphicsLibraryFramework;
using SeEditor.Renderer.Buffers;
using SeEditor.Utilities;

namespace SeEditor.Editor;

public class SceneCamera
{
    public float Fov { get; set; } = 60.0f * MathUtils.Deg2Rad;
    public float AspectRatio { get; set; } = 16.0f / 9.0f;
    public float Near { get; set; } = 0.1f;
    public float Far { get; set; } = 20000.0f;

    /// <summary>
    ///     The camera's view matrix.
    /// </summary>
    public Matrix4x4 View => MatrixData.View;
    
    /// <summary>
    ///     The camera's projection matrix.
    /// </summary>
    public Matrix4x4 Projection => MatrixData.Projection;
    
    /// <summary>
    ///     The current position of the camera.
    /// </summary>
    public Vector3 Position;
    
    /// <summary>
    ///     The current rotation of the camera.
    /// </summary>
    public Vector3 Rotation;
    
    /// <summary>
    ///     Inverse of the camera's rotation, used for translating locally.
    /// </summary>
    private Matrix4x4 _inverseRotation;
    
    /// <summary>
    ///     Camera matrix data, stored in a explicit struct for uniform buffers.
    /// </summary>
    public ConstantBufferViewProjection MatrixData;
    
    /// <summary>
    ///     Recomputes view and projection matrices.
    /// </summary>
    public void RecomputeMatrixData()
    {
        MatrixData.Projection = Matrix4x4.CreatePerspectiveFieldOfView(Fov, AspectRatio, Near, Far);
        _inverseRotation = Matrix4x4.CreateRotationX(-Rotation.X) *
                           Matrix4x4.CreateRotationY(-Rotation.Y) *
                           Matrix4x4.CreateRotationZ(-Rotation.Z);

        var translation = Matrix4x4.CreateTranslation(Position);
        var rotation = 
            Matrix4x4.CreateRotationZ(Rotation.Z) *
            Matrix4x4.CreateRotationY(Rotation.Y) *
            Matrix4x4.CreateRotationX(Rotation.X);

        MatrixData.View = translation * rotation;
        
        Matrix4x4.Invert(View, out MatrixData.ViewInverse);
        MatrixData.ViewProjection = View * Projection;
    }

    /// <summary>
    ///     Translates the camera relative to the current view.
    /// </summary>
    /// <param name="delta">Camera delta translation</param>
    public void TranslateLocal(Vector3 delta)
    {
        Position += Vector3.Transform(delta, _inverseRotation);
    }
    
    /// <summary>
    ///     Adjusts camera based on user input.
    /// </summary>
    /// <param name="keyboard">Current keyboard input state</param>
    /// <param name="mouse">Current mouse input state</param>
    public void OnInput(KeyboardState keyboard, MouseState mouse)
    {
        bool shift = keyboard.IsKeyDown(Keys.LeftShift);
        bool middle = mouse.IsButtonDown(MouseButton.Button3);
        
        if (shift && middle)
        {
            Position += Vector3.Transform(new Vector3(mouse.Delta.X, -mouse.Delta.Y, 0.0f), _inverseRotation);
        }
        else if (middle)
        {
            Rotation.X -= mouse.Delta.Y * 0.01f;
            Rotation.Y -= mouse.Delta.X * 0.01f;
        }
        else if (!shift)
        {
            float delta = mouse.ScrollDelta.Y * 20.0f;
            Position += Vector3.Transform(new Vector3(0.0f, 0.0f, delta), _inverseRotation);
        }
    }
}