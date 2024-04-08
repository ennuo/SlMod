namespace SlLib.Resources.Skeleton;

/// <summary>
///     Helper class for serializing joint hierarchy.
/// </summary>
public class JointHierarchyCacheItem(int index, int parent)
{
    /// <summary>
    ///     The index of this joint.
    /// </summary>
    public readonly int Index = index;

    /// <summary>
    ///     The index of the parent of this joint.
    /// </summary>
    public readonly int Parent = parent;

    /// <summary>
    ///     The number of parents this joint has.
    /// </summary>
    public int NumParents;

    /// <summary>
    ///     Fetches the number of parents this item has.
    /// </summary>
    /// <param name="items">Cache item list</param>
    public void FetchParentCount(List<JointHierarchyCacheItem> items)
    {
        NumParents = 0;
        JointHierarchyCacheItem item = this;
        while (item.Parent != -1)
        {
            item = items[item.Parent];
            NumParents++;
        }
    }
}