using System.Numerics;
using SlLib.Resources.Database;
using SlLib.Serialization;

namespace SlLib.Resources.Model;

/// <summary>
///     Bounding sphere used for visibility testing.
/// </summary>
public class SlCullSphere : IResourceSerializable
{
    /// <summary>
    ///     The center of the cull sphere.
    /// </summary>
    public Vector3 SphereCenter = Vector3.Zero;
    
    /// <summary>
    ///     The radius of the cull sphere.
    /// </summary>
    public float Radius = 1.0f;

    /// <summary>
    ///     Unknown extents vector
    ///     <remarks>
    ///         I don't really have any idea what this is used for, and it doesn't seem like the game uses it either?
    ///     </remarks>
    /// </summary>
    public Vector4 BoxCenter = new(0.0f, 0.0f, 0.0f, 1.0f);

    /// <summary>
    ///     Unknown extents vector
    ///     <remarks>
    ///         I don't really have any idea what this is used for, and it doesn't seem like the game uses it either?
    ///     </remarks>
    /// </summary>
    public Vector4 Extents = new(0.5f, 0.5f, 0.5f, 0.0f);
    
    public void Load(ResourceLoadContext context)
    {
        SphereCenter = context.ReadFloat3();
        Radius = context.ReadFloat();
        BoxCenter = context.ReadFloat4();
        Extents = context.ReadFloat4();
    }

    /// <inheritdoc />
    public void Save(ResourceSaveContext context, ISaveBuffer buffer)
    {
        context.WriteFloat3(buffer, SphereCenter, 0x0);
        context.WriteFloat(buffer, Radius, 0xC);
        context.WriteFloat4(buffer, BoxCenter, 0x10);
        context.WriteFloat4(buffer, Extents, 0x20);
    }

    public int GetSizeForSerialization(SlPlatform platform, int version)
    {
        return 0x30;
    }
}