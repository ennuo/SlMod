using SlLib.Resources;
using SlLib.Resources.Database;

namespace SlLib.IO;

public class SlImportConfig(SlResourceDatabase database, string glbSourcePath)
{
    /// <summary>
    ///     The database to save imported assets to.
    /// </summary>
    public SlResourceDatabase Database = database;

    /// <summary>
    ///     The path to the target glTF 2.0 Binary file.
    /// </summary>
    public string GlbSourcePath = glbSourcePath;

    /// <summary>
    ///     The skeleton to use when importing this model.
    ///     If no skeleton is provided, one will be generated.
    /// </summary>
    public SlSkeleton? Skeleton;
    
    /// <summary>
    ///     Optional bone name re-mapping function.
    /// </summary>
    public Func<List<(string Bone, int Parent)>, int, string>? BoneRemapCallback;
}