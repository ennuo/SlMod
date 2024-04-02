using SlLib.Archives;
using SlLib.Resources.Excel;
using SlLib.Utilities;

namespace SlToolkit;

public class ResourceManager
{
    public static ResourceManager Instance { get; }
    
    public string Root = string.Empty;
    public readonly List<IFileSystem> FileSystems = [];
    public ExcelData RacerData = new();

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
        else CryptUtil.DecodeBuffer(data);
        
        RacerData = ExcelData.Load(data);
    }
    
    public byte[]? GetFile(string path)
    {
        byte[]? data = null;
        
        // Check each loaded pack file for the data
        foreach (IFileSystem system in FileSystems)
        {
            data = system.GetFile(path);
            if (data != null)
                return data;
        }
        
        // If we can't find it in any of the filesystems
        // fallback to reading from the root directory
        path = Path.Combine(Root, path);
        if (File.Exists(path))
            data = File.ReadAllBytes(path);
        
        return data;
    }
}