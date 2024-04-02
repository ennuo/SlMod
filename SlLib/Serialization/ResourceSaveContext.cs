using SlLib.Resources.Database;

namespace SlLib.Serialization;

public class ResourceSaveContext
{
    /// <summary>
    ///     The previous slab in the list for CPU data.
    /// </summary>
    private Slab? _cpu = null;

    /// <summary>
    ///     The current size of the CPU buffer.
    /// </summary>
    private int _cpuSize = 0;

    /// <summary>
    ///     The previous slab in the list for GPU data.-
    /// </summary>
    private Slab? _gpu = null;

    /// <summary>
    ///     The current size of the GPU buffer.
    /// </summary>
    private int _gpuSize = 0;

    /// <summary>
    ///     Cached serialized slabs by address.
    /// </summary>
    private Dictionary<int, Slab> _references = [];

    /// <summary>
    ///     Pointer relocations table.
    /// </summary>
    private List<SlResourceRelocation> _relocations = [];
}