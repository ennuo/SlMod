using SlLib.Excel;
using SlLib.Extensions;
using SlLib.Filesystem;
using SlLib.Resources.Database;

namespace SeEditor.Managers;

public class SlFile
{
    private static readonly List<IFileSystem> FileSystems = [];

    public static void AddGameDataFolder(string root)
    {
        if (!Directory.Exists(root))
            throw new DirectoryNotFoundException($"Game data folder doesn't exist at {root}");
        
        // Add all archives to the resource manager
        foreach (string archive in Directory.GetFiles(root, "*.toc"))
            FileSystems.Add(new SlPackFile(archive));
        
        // Fallback to game directory
        FileSystems.Add(new MappedFileSystem(@"C:\Program Files (x86)\Steam\steamapps\common\Sonic & All-Stars Racing Transformed\Data\"));
        FileSystems.Add(new MappedFileSystem(root));
        FileSystems.Add(new MappedFileSystem("F:/games/Team Sonic Racing/data"));
        FileSystems.Add(new MappedFileSystem("F:/sart/"));
    }

    public static void AddFileSystem(IFileSystem fs)
    {
        FileSystems.Add(fs);
    }
    
    public static bool DoesFileExist(string path)
    {
        return FileSystems.Any(system => system.DoesFileExist(path));
    }
    
    /// <summary>
    ///     Attempts to load an excel data file
    /// </summary>
    /// <param name="path">Path to excel data file</param>
    /// <returns>Excel data, if it exists</returns>
    public static ExcelData? GetExcelData(string path)
    {
        foreach (IFileSystem fs in FileSystems)
        {
            if (fs.DoesExcelDataExist(path))
                return fs.GetExcelData(path);   
        }

        return null;
    }

    public static SlResourceDatabase? GetSceneDatabase(string path, string extension = "pc")
    {
        foreach (IFileSystem fs in FileSystems)
        {
            if (fs.DoesSceneExist(path, extension))
                return fs.GetSceneDatabase(path, extension);
        }
        
        return null;
    }

    public static byte[]? GetFile(string path)
    {
        foreach (IFileSystem fs in FileSystems)
        {
            if (fs.DoesFileExist(path))
                return fs.GetFile(path);
        }

        return null;
    }
}