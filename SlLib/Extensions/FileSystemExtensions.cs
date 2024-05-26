using SlLib.Excel;
using SlLib.Filesystem;
using SlLib.Resources.Database;
using SlLib.SumoTool;
using SlLib.Utilities;

namespace SlLib.Extensions;

/// <summary>
///     Extensions for loading common game resources from filesystems.
/// </summary>
public static class FileSystemExtensions
{
    /// <summary>
    ///     Checks if any sumo tool files exist at a path.
    /// </summary>
    /// <param name="fs">Filesystem</param>
    /// <param name="path">Path of sumo tool file</param>
    /// <param name="language">Language extension of package</param>
    /// <returns>Whether or not any sumo tool files exist</returns>
    public static bool DoesSumoToolFileExist(this IFileSystem fs, string path, string language = "en")
    {
        return
            // Check if a common or locale compressed package exists
            fs.DoesFileExist($"{path}.stz") || fs.DoesFileExist($"{path}_{language}.stz") ||
            // Check if common tool files exist
            (fs.DoesFileExist($"{path}.dat") && fs.DoesFileExist($"{path}.rel")) ||
            // Check if common locale files exist
            (fs.DoesFileExist($"{path}_{language}.dat") && fs.DoesFileExist($"{path}_{language}.rel"));
    }

    /// <summary>
    ///     Checks if a scene file exists at a path.
    /// </summary>
    /// <param name="fs">Filesystem</param>
    /// <param name="path">Path to scene resource database</param>
    /// <param name="platform">Platform of database</param>
    /// <returns></returns>
    public static bool DoesSceneExist(this IFileSystem fs, string path, string platform = "pc")
    {
        return fs.DoesFileExist($"{path}.cpu.s{platform}") && fs.DoesFileExist($"{path}.gpu.s{platform}");
    }

    /// <summary>
    ///     Loads a sumo tool package.
    /// </summary>
    /// <param name="fs">Filesystem</param>
    /// <param name="path">Path to sumo tool files</param>
    /// <param name="language">Language extension for package</param>
    /// <returns>Parsed sumo tool package</returns>
    public static SumoToolPackage GetSumoToolPackage(this IFileSystem fs, string path, string language = "en")
    {
        string localePrefix = $"{path}_{language}";

        // Try loading locale compressed package
        if (fs.DoesFileExist($"{localePrefix}.stz"))
        {
            using Stream compressedPackageStream = fs.GetFileStream($"{localePrefix}.stz", out _);
            return SumoToolPackage.Load(compressedPackageStream);
        }

        // Try loading common compressed package
        if (fs.DoesFileExist($"{path}.stz"))
        {
            using Stream compressedPackageStream = fs.GetFileStream($"{path}.stz", out _);
            return SumoToolPackage.Load(compressedPackageStream);
        }

        // Otherwise, create a new package file from the individual tool files
        var package = new SumoToolPackage();

        // Check for common data files
        if (fs.DoesFileExist($"{path}.dat"))
        {
            byte[] dat = fs.GetFile($"{path}.dat");
            byte[] rel = fs.GetFile($"{path}.rel");
            byte[]? gpu = null;
            if (fs.DoesFileExist($"{path}.gpu"))
                gpu = fs.GetFile($"{path}.gpu");

            package.SetCommonChunks(dat, rel, gpu);
        }

        // Check for locale data files
        if (fs.DoesFileExist($"{localePrefix}.dat"))
        {
            byte[] dat = fs.GetFile($"{localePrefix}.dat");
            byte[] rel = fs.GetFile($"{localePrefix}.rel");
            byte[]? gpu = null;
            if (fs.DoesFileExist($"{localePrefix}.gpu"))
                gpu = fs.GetFile($"{localePrefix}.gpu");

            package.SetLocaleChunks(dat, rel, gpu);
        }

        return package;
    }

    /// <summary>
    ///     Loads a resource database from a scene file.
    /// </summary>
    /// <param name="fs">Filesystem</param>
    /// <param name="path">Path to scene resource database</param>
    /// <param name="extension">Platform extension of database</param>
    /// <returns>Parsed scene resource database</returns>
    public static SlResourceDatabase GetSceneDatabase(this IFileSystem fs, string path, string extension = "pc")
    {
        extension = $"s{extension}";
        string cpuFilePath = $"{path}.cpu.{extension}";
        string gpuFilePath = $"{path}.gpu.{extension}";
        
        SlPlatform platform = SlPlatform.GuessPlatformFromExtension(cpuFilePath);
        
        using Stream cpuStream = fs.GetFileStream(cpuFilePath, out int cpuStreamSize);
        using Stream gpuStream = fs.GetFileStream(gpuFilePath, out _);

        return SlResourceDatabase.Load(cpuStream, cpuStreamSize, gpuStream, platform);
    }

    /// <summary>
    ///     Checks if an excel data file exists at a path.
    /// </summary>
    /// <param name="fs">Filesystem</param>
    /// <param name="path">Path to excel data</param>
    /// <returns>Whether or not file exists</returns>
    public static bool DoesExcelDataExist(this IFileSystem fs, string path)
    {
        return fs.DoesFileExist($"{path}.dat") || fs.DoesFileExist($"{path}.zat");
    }

    /// <summary>
    ///     Loads excel data at a path
    /// </summary>
    /// <param name="fs">Filesystem</param>
    /// <param name="path">Path to excel data</param>
    /// <returns>Parsed excel data</returns>
    public static ExcelData GetExcelData(this IFileSystem fs, string path)
    {
        byte[] dat;

        // Check if the encrypted version exists
        if (fs.DoesFileExist($"{path}.zat"))
        {
            dat = fs.GetFile($"{path}.zat");
            CryptUtil.DecodeBuffer(dat);
        }
        // Otherwise, load the normal dat file
        else
        {
            dat = fs.GetFile($"{path}.dat");
        }

        return ExcelData.Load(dat);
    }
}