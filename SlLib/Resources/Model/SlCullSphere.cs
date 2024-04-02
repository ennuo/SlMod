using System.Numerics;
using SlLib.Serialization;

namespace SlLib.Resources.Model;

/// <summary>
///     Bounding sphere used for visibility testing.
/// </summary>
public class SlCullSphere : ILoadable
{
    /// <summary>
    ///     The center of the cull sphere.
    /// </summary>
    public Vector3 Center;

    /// <summary>
    ///     Unknown extents vector
    ///     <remarks>
    ///         I don't really have any idea what this is used for, and it doesn't seem like the game uses it either?
    ///     </remarks>
    /// </summary>
    public Vector4 ExtentsA = new(0.0f, 0.0f, 0.0f, 1.0f);

    /// <summary>
    ///     Unknown extents vector
    ///     <remarks>
    ///         I don't really have any idea what this is used for, and it doesn't seem like the game uses it either?
    ///     </remarks>
    /// </summary>
    public Vector4 ExtentsB;

    /// <summary>
    ///     The radius of the cull sphere.
    /// </summary>
    public float Radius = 1.0f;

    /// <inheritdoc />
    public void Load(ResourceLoadContext context, int offset)
    {
        Center = context.ReadFloat3(offset);
        Radius = context.ReadFloat(offset + 0xc);
        ExtentsA = context.ReadFloat4(offset + 0x10);
        ExtentsB = context.ReadFloat4(offset + 0x20);
    }
}