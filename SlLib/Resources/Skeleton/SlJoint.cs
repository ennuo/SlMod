using System.Numerics;

namespace SlLib.Resources.Skeleton;

public class SlJoint
{
    /// <summary>
    ///     The bind pose of this joint.
    /// </summary>
    public Matrix4x4 BindPose = Matrix4x4.Identity;

    /// <summary>
    ///     The inverse bind pose of this joint.
    /// </summary>
    public Matrix4x4 InverseBindPose = Matrix4x4.Identity;

    /// <summary>
    ///     The name of this joint.
    /// </summary>
    public string Name = string.Empty;

    /// <summary>
    ///     The index of the parent of this joint.
    /// </summary>
    public int Parent = -1;

    /// <summary>
    ///     The local rotation of this joint.
    /// </summary>
    public Quaternion Rotation = Quaternion.Identity;

    /// <summary>
    ///     The local scale of this joint.
    /// </summary>
    public Vector3 Scale = Vector3.One;

    /// <summary>
    ///     The local translation of this joint.
    /// </summary>
    public Vector3 Translation;
}