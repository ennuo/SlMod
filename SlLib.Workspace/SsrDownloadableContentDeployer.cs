using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using SlLib.Extensions;
using SlLib.Utilities;

namespace SlLib.Workspace;

using SlLib.Filesystem;
using SlLib.Resources.Database;
using SlLib.SumoTool;
using SlLib.SumoTool.Siff;

public class SsrDownloadableContentDeployer
{
    private static SlPlatformContext PlatformWin32 = new()
    {
        Platform = SlPlatform.Win32, IsSSR = true, Version = -1
    };

    private static SlPlatformContext PlatformX360 = new()
    {
        Platform = SlPlatform.Xbox360, IsSSR = true, Version = -1
    };

    private static SlPlatformContext PlatformPS3 = new()
    {
        Platform = SlPlatform.Ps3, IsSSR = true, Version = -1
    };
    
    const string WinFileData = "F:/sart/ssr/pc/";
    const string XboxFileData = "F:/sart/ssr/dlc/xbox/";
    private const string Ps3FileData = "F:/sart/ssr/dlc/ps3/";
    private const string PublishDirectory = "F:/sart/ssr/dlc/build/";
    const string GameDirectory = "C:\\Program Files (x86)\\Steam\\steamapps\\common\\Sonic and SEGA All Stars Racing\\";
    
    public static void Run()
    {
        var tracks = new SsrPackFile($"{GameDirectory}/resource/tracks.xpac");
        var ai = new SsrPackFile($"{GameDirectory}/resource/ai.xpac");
        var racers = new SsrPackFile($"{GameDirectory}/resource/racers.xpac");
        
        Console.WriteLine($"[] - Performing KSiff conversions tests for locale data...");
        
        PublishPackage("resource/sumotoolresources/fe_character_select_metalsonic");
        PublishPackage("resource/sumotoolresources/fe_leaderboards_metal");
        PublishPackage("resource/sumotoolresources/fe_track_select_deathegg");
        PublishPackage("resource/sumotoolresources/characters/race_results_metalsonic");
        PublishPackage("resource/sumotoolresources/loading/doomeggzone_dlc");
        PublishPackage("resource/sumotoolresources/shopping/characterbio_metalsonic");
        PublishPackage("resource/sumotoolresources/shopping/trackbio_deathegg");
        PublishPackage("resource/sumotoolresources/trackintro/track_intro_deathegg");
        
        PublishPackage("resource/sumotoolresources/fe_shopping_deathegg");
        PublishPackage("resource/sumotoolresources/fe_shopping_deathegg");
        PublishPackage("resource/sumotoolresources/fe_shopping_metalsonic");
        PublishPackage("resource/sumotoolresources/fe_navigator_deathegg");
        
        PublishPackage("resource/sumotoolresources/ingame/commonhud");
        
        Console.WriteLine("[] - Converting X360 Mecha Sonic Siff files...");
        
        PublishSiff("resource/racers/mechasonic");
        PublishSiff("resource/select/mechasonicselect");
        PublishSiff("resource/select/mechasonicselect");
        PublishSiff("resource/tracks/doomeggzone_dlc");
        PublishSiff("resource/tracks/doomeggzone_dlc_pcrt_sh_data");
        
        tracks.SetFile("resource/tracks/seasidehill_easy.zif", File.ReadAllBytes($"{GameDirectory}/resource/tracks/doomeggzone_dlc.zif"));
        tracks.SetFile("resource/tracks/seasidehill_easy.zig", File.ReadAllBytes($"{GameDirectory}/resource/tracks/doomeggzone_dlc.zig"));
        
        tracks.SetFile("resource/tracks/seasidehill_easy_pcrt_sh_data.zif", File.ReadAllBytes($"{GameDirectory}/resource/tracks/doomeggzone_dlc_pcrt_sh_data.zif"));
        tracks.SetFile("resource/tracks/seasidehill_easy_pcrt_sh_data.zig", File.ReadAllBytes($"{GameDirectory}/resource/tracks/doomeggzone_dlc_pcrt_sh_data.zig"));
        
        ai.SetFile("resource/ai/ai_seasidehill_easy.txt", File.ReadAllBytes("F:/sart/ssr/dlc/xbox/resource/ai/ai_doomeggzone_dlc.txt"));
        
        
        
        
    }
    
    private static byte[] ConvertPackageToWin32(string path)
    {
        var plat = new SlPlatformContext { Platform = SlPlatform.Win32, IsSSR = true, Version = -1 };
        var converted = new SumoToolPackage(plat);
        var original = SumoToolPackage.Load(PlatformPS3, path);
        
        if (original.HasLocaleData())
        {
            var target = new SiffFile(PlatformWin32);
            var siff = original.GetLocaleSiff();
            ReimportChunks(siff, target);
            converted.SetLocaleData(target);
        }

        if (original.HasCommonData())
        {
            var target = new SiffFile(PlatformWin32);
            var siff = original.GetCommonSiff();
            ReimportChunks(siff, target);
            converted.SetCommonData(target);
        }

        return converted.Save(compress: true);

        void ReimportChunks(SiffFile siff, SiffFile target)
        {
            if (siff.HasResource(SiffResourceType.Info))
                target.SetResource(siff.LoadResource<InfoSiffData>(SiffResourceType.Info), SiffResourceType.Info);
            if (siff.HasResource(SiffResourceType.TexturePack))
                target.SetResource(siff.LoadResource<TexturePack>(SiffResourceType.TexturePack), SiffResourceType.TexturePack);
            
            if (siff.HasResource(SiffResourceType.KeyFrameLibrary))
                target.SetResource(siff.LoadResource<KeyframeLibrary>(SiffResourceType.KeyFrameLibrary), SiffResourceType.KeyFrameLibrary);
            
            if (siff.HasResource(SiffResourceType.ObjectDefLibrary))
                target.SetResource(siff.LoadResource<ObjectDefLibrary>(SiffResourceType.ObjectDefLibrary), SiffResourceType.ObjectDefLibrary);
            
            if (siff.HasResource(SiffResourceType.SceneLibrary))
                target.SetResource(siff.LoadResource<SceneLibrary>(SiffResourceType.SceneLibrary), SiffResourceType.SceneLibrary);
            
            if (siff.HasResource(SiffResourceType.FontPack))
                target.SetResource(siff.LoadResource<FontPack>(SiffResourceType.FontPack), SiffResourceType.FontPack);
 
            if (siff.HasResource(SiffResourceType.TextPack))
                target.SetResource(siff.LoadResource<TextPack>(SiffResourceType.TextPack), SiffResourceType.TextPack);
        }
    }

    private static void PublishFile(string path, byte[] data)
    {
        path = $"{PublishDirectory}\\{path}";
        string? directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            Directory.CreateDirectory(directory);
        File.WriteAllBytes(path, data);
    }
    
    private static void PublishPackage(string path)
    {
        foreach (string region in new[] {"en", "fr", "ge", "it", "jp", "sp", "us"})
        {
            string regional = path + "_" + region + ".stz";
            string local = Ps3FileData + regional;
            if (!File.Exists(local)) continue;
            
            byte[] data = ConvertPackageToWin32(local);
            if (regional.EndsWith("_en.stz"))
                PublishFile(regional.Replace("_en.stz", "_us.stz"), data);
            PublishFile(regional, data);
        }
    }
    
    private static void PublishSiff(string path)
    {
        string local = XboxFileData + path;
        (byte[] dat, byte[] gpu) = ConvertSiffToWin32(local);
        
        PublishFile($"{path}.zif", dat);
        PublishFile($"{path}.zig", gpu);
    }
    
    private static (byte[] dat, byte[] gpu) ConvertSiffToWin32(string path)
    {
        var plat = new SlPlatformContext { Platform = SlPlatform.Win32, IsSSR = true, Version = -1 };
        var target = new SiffFile(plat);
        var siff = SiffFile.Load(PlatformX360, File.ReadAllBytes($"{path}.zif"), null,
            File.ReadAllBytes($"{path}.zig"), compressed: true);
        
        if (siff.HasResource(SiffResourceType.ShData))
            target.SetResource(siff.LoadResource<ShSamplerData>(SiffResourceType.ShData), SiffResourceType.ShData);
        if (siff.HasResource(SiffResourceType.Navigation))
            target.SetResource(siff.LoadResource<Navigation>(SiffResourceType.Navigation), SiffResourceType.Navigation);
        if (siff.HasResource(SiffResourceType.Forest))
            target.SetResource(siff.LoadResource<ForestLibrary>(SiffResourceType.Forest), SiffResourceType.Forest, overrideGpuData: true);
        if (siff.HasResource(SiffResourceType.VisData))
            target.SetResource(siff.LoadResource<VisData>(SiffResourceType.VisData), SiffResourceType.VisData);
        if (siff.HasResource(SiffResourceType.Collision))
            target.SetResource(siff.LoadResource<CollisionMesh>(SiffResourceType.Collision), SiffResourceType.Collision);
        if (siff.HasResource(SiffResourceType.Logic))
            target.SetResource(siff.LoadResource<LogicData>(SiffResourceType.Logic), SiffResourceType.Logic);
        if (siff.HasResource(SiffResourceType.LensFlare2))
            target.SetResource(siff.LoadResource<LensFlare2>(SiffResourceType.LensFlare2), SiffResourceType.LensFlare2);
        if (siff.HasResource(SiffResourceType.Trail))
            target.SetResource(siff.LoadResource<TrailData>(SiffResourceType.Trail), SiffResourceType.Trail);
        
        
        target.BuildKSiff(out byte[] dat, out byte[] gpu, compressed: true);

        return (dat, gpu);
    }
}