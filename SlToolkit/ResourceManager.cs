using SlLib.Excel;
using SlLib.Filesystem;
using SlLib.SumoTool;
using SlLib.SumoTool.Siff;
using SlLib.Utilities;

namespace SlToolkit;

public class ResourceManager
{
    public readonly List<IFileSystem> FileSystems = [];
    public TexturePack? HudElements;
    public string Language = "en";
    public ExcelData RacerData = new();

    public string Root = string.Empty;

    static ResourceManager()
    {
        Instance = new ResourceManager
        {
            Root = "F:/cache/sonic/pc"
        };

        Instance.FileSystems.Add(new SlPackFile("F:/cache/sonic/Frontend"));
        Instance.FileSystems.Add(new SlPackFile("F:/cache/sonic/GameAssets"));
        Instance.FileSystems.Add(new SlPackFile("F:/cache/sonic/GameData"));

        Instance.Initialize();
    }

    public static ResourceManager Instance { get; }

    public void Initialize()
    {
        byte[]? data = GetFile("gamedata/racers.zat");
        if (data == null)
        {
            // Check if the unencrypted version exists as a fallback
            data = GetFile("gamedata/racers.dat");
            if (data == null)
                return;
        }
        // Zat files are encrypted
        else
        {
            CryptUtil.DecodeBuffer(data);
        }

        RacerData = ExcelData.Load(data);

        byte[]? packageData = GetFile("ui/frontend/hud_elements/hud_elements_en.stz");
        if (packageData == null) return;
        SumoToolPackage package = SumoToolPackage.Load(packageData);
        if (!package.HasLocaleData()) return;
        SiffFile siff = package.GetLocaleSiff();
        if (siff.HasResource(SiffResourceType.TexturePack))
            HudElements = siff.LoadResource<TexturePack>(SiffResourceType.TexturePack);
    }

    public byte[]? GetFile(string path)
    {
        byte[]? data = null;

        // Check each loaded pack file for the data
        foreach (IFileSystem system in FileSystems)
        {
            if (!system.DoesFileExist(path)) continue;
            return system.GetFile(path);
        }

        // If we can't find it in any of the filesystems
        // fallback to reading from the root directory
        path = Path.Combine(Root, path);
        if (File.Exists(path))
            data = File.ReadAllBytes(path);

        return data;
    }
}