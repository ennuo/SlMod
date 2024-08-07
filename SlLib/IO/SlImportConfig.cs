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
    ///     Virtual scene name override.
    /// </summary>
    public string VirtualSceneName = Path.GetFileNameWithoutExtension(glbSourcePath);

    /// <summary>
    ///     Virtual file path to use for files
    /// </summary>
    public string VirtualFilePath = Path.GetDirectoryName(glbSourcePath)?.Replace("\\", "/").Replace("F:/", string.Empty) ?? string.Empty;

    /// <summary>
    ///     Handles how the scene is imported.
    /// </summary>
    public SlImportType ImportType = SlImportType.Standard;
    
    /// <summary>
    ///     Whether to build a character database
    /// </summary>
    public bool IsCharacterImport => ImportType != SlImportType.Standard;
    
    /// <summary>
    ///     Optional bone name re-mapping function.
    /// </summary>
    public Func<List<(string Bone, int Parent)>, int, string>? BoneRemapCallback;
}