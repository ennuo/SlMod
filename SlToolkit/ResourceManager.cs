using SlLib.Excel;
using SlLib.Extensions;
using SlLib.Filesystem;
using SlLib.SumoTool;
using SlLib.SumoTool.Siff;

namespace SlToolkit;

public class ResourceManager
{
    public static ResourceManager Instance { get; }

    private readonly List<IFileSystem> _fileSystems = [];
    public TexturePack? HudElements;
    public string Language = "en";
    public ExcelData RacerData = new();

    static ResourceManager()
    {
        Instance = new ResourceManager();
    }

    public void SetGameDataFolder(string root)
    {
        if (!Directory.Exists(root))
            throw new DirectoryNotFoundException($"Game data folder doesn't exist at {root}");

        _fileSystems.Clear();

        // Prefer reading extracted files over packed files.
        _fileSystems.Add(new MappedFileSystem(root));

        // Add all archives to the resource manager
        foreach (string archive in Directory.GetFiles(root, "*.toc"))
            _fileSystems.Add(new SlPackFile(archive));

        // Now that the filesystem is setup, load common files needed for program to work

        RacerData = GetExcelData("gamedata/racers") ?? RacerData;
        // Load HUD elements for UI icons
        SumoToolPackage? package = GetSumoToolPackage("ui/frontend/hud_elements/hud_elements");
        if (package == null || !package.HasLocaleData()) return;
        SiffFile siff = package.GetLocaleSiff();
        if (siff.HasResource(SiffResourceType.TexturePack))
            HudElements = siff.LoadResource<TexturePack>(SiffResourceType.TexturePack);
    }

    /// <summary>
    ///     Attempts to get an excel data file from any filesystem.
    /// </summary>
    /// <param name="path">Path to excel data file</param>
    /// <returns>Excel data, if it exists</returns>
    public ExcelData? GetExcelData(string path)
    {
        foreach (IFileSystem fs in _fileSystems)
            if (fs.DoesExcelDataExist(path))
                return fs.GetExcelData(path);

        return null;
    }

    /// <summary>
    ///     Attempts to get a sumo tool package from any filesystem.
    /// </summary>
    /// <param name="path">Path to sumo tool file</param>
    /// <returns>Sumo tool package, if it exists</returns>
    public SumoToolPackage? GetSumoToolPackage(string path)
    {
        foreach (IFileSystem fs in _fileSystems)
            if (fs.DoesSumoToolFileExist(path, Language))
                return fs.GetSumoToolPackage(path, Language);

        return null;
    }

    public byte[]? GetFile(string path)
    {
        byte[]? data = null;

        // Check each loaded pack file for the data
        foreach (IFileSystem system in _fileSystems)
        {
            if (!system.DoesFileExist(path)) continue;
            return system.GetFile(path);
        }

        return data;
    }
}