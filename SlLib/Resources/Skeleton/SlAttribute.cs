namespace SlLib.Resources.Skeleton;

/// <summary>
///     A configurable attribute for an entity in a skeleton.
/// </summary>
public class SlAttribute
{
    /// <summary>
    ///     The default value of this attribute.
    /// </summary>
    public float Default;

    /// <summary>
    ///     The name of the entity that this attribute belongs to.
    /// </summary>
    public string Entity = string.Empty;

    /// <summary>
    ///     The name of this attribute.
    /// </summary>
    public string Name = string.Empty;

    /// <summary>
    ///     The index of the parent of this attribute.
    /// </summary>
    public int Parent = -1;

    /// <summary>
    ///     The current user value of this attribute.
    /// </summary>
    public float Value;
}