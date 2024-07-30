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
    
    const string pc = "F:/sart/ssr/pc/";
    const string xbox = "F:/sart/ssr/DLC/XBOX/Extract/";
    const string game = "C:\\Program Files (x86)\\Steam\\steamapps\\common\\Sonic and SEGA All Stars Racing\\";


    public static void RPCTest()
    {
        var tracks = new SsrPackFile($"{game}/resource/tracks.xpac");
        var ai = new SsrPackFile($"{game}/resource/ai.xpac");
        var racers = new SsrPackFile($"{game}/resource/racers.xpac");
        var sumotool = new SsrPackFile($"{game}/resource/sumotoolresources.xpac");
        var common = new SsrPackFile($"F:/sart/ssr/pack/base.xpac");

        string cpp = string.Empty;

        var package = sumotool.GetSumoToolPackage("resource/sumotoolresources/fe_missions");
        var siff = package.GetLocaleSiff();
        var text = siff.LoadResource<TextPack>(SiffResourceType.TextPack);
        
        
        // Console.WriteLine(text[SlUtil.SumoHash("MISSION_NAME_1")]);
        // Console.WriteLine(text[SlUtil.SumoHash("MISSION_TXT_1")]);
        // Console.WriteLine(text[SlUtil.SumoHash("RULES_1")]);

        int missionIndex = 0;
        var missionparams = common.GetExcelData("resource/missionparams");
        foreach (var column in missionparams.Worksheets[0]!.Columns)
        {
            if (column.GetInt("IsRealMission") != 0)
            {
                cpp += "{ ";

                string name = string.Join(' ', text[SlUtil.SumoHash("MISSION_NAME_" + missionIndex)].Split(" ")[1..]);
                string type = text[SlUtil.SumoHash("RULES_" + missionIndex)];
                
                string presence;
                switch (column.GetString("Rule1"))
                {
                    case "kScoreType_Eliminator":
                        presence = "kPresenceDetails_Elimination";
                        break;
                    case "kScoreType_GrandPrix":
                        presence = "kPresenceDetails_GrandPrix";
                        break;
                    case "kScoreType_Race":
                        presence = "kPresenceDetails_Race";
                        break;
                    case "kScoreType_BossBattle":
                        presence = "kPresenceDetails_Boss";
                        break;
                    default:
                        presence = "kPresenceDetails_Rank";
                        break;
                }
                
                Console.WriteLine($"{name} : {column.GetString("Rule1")}");
                
                cpp += $"sumohash(\"{column.GetString("MissionHash")}\"), ";
                cpp += "{ ";
                cpp += $"\"{name}\", \"{type}\", {presence} ";
                
                cpp += "}";
                cpp += " },\n";
                
            }
            
            
            

            missionIndex++;
        }
        
        Console.WriteLine(cpp);
        


        // var trackparams = common.GetExcelData("resource/trackparams");
        // foreach (var worksheet in trackparams.Worksheets)
        // foreach (var column in worksheet.Columns)
        // {
        //     string name = column.GetString("Name");
        //     if (string.IsNullOrEmpty(name)) continue;
        //
        //     var package = sumotool.GetSumoToolPackage("resource/sumotoolresources/loading/" + name);
        //     var siff = package.GetLocaleSiff();
        //     var pack = siff.LoadResource<TexturePack>(SiffResourceType.TexturePack);
        //     var text = siff.LoadResource<TextPack>(SiffResourceType.TextPack);
        //
        //     string trackDisplayName = CleanupStringName(text[1007664124]);
        //     string trackDisplayGroup = CleanupStringName(text[739021175]);
        //     
        //     // 128426693
        //     // 932607134
        //     // 38464378
        //     // 128426693
        //     // 932607134
        //
        //
        //     using var image = pack.GetSprite(128426693).GetImage();
        //     image.SaveAsPng("C:/Users/Aidan/Desktop/tracks/" + name + ".png");
        //     
        //     
        //     
        //
        //     cpp += "{ ";
        //
        //     cpp += $"sumohash(\"{name}\"), \"{trackDisplayGroup}\"";
        //     
        //     cpp += " },\n";
        //
        //     continue;
        //     
        //     string CleanupStringName(string s)
        //     {
        //         s = string.Join(' ', s.Split(" ").Select(x => x[0] + x.ToLower()[1..]));
        //         //s = string.Join('-', s.Split("-").Select(x => x[0] + x.ToLower()[1..]));
        //         return s;
        //     }
        // }
        //
        // Console.WriteLine(cpp);
    }
    
    public static void Run()
    {
        var tracks = new SsrPackFile($"{game}/resource/tracks.xpac");
        var ai = new SsrPackFile($"{game}/resource/ai.xpac");
        var racers = new SsrPackFile($"{game}/resource/racers.xpac");
        
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
        
        //PublishPackage("resource/sumotoolresources/ingame/commonhud");
        
        Console.WriteLine("[] - Converting X360 Mecha Sonic Siff files...");
        
        PublishSiff("resource/racers/mechasonic");
        PublishSiff("resource/select/mechasonicselect");
        PublishSiff("resource/tracks/doomeggzone_dlc");
        PublishSiff("resource/tracks/doomeggzone_dlc_pcrt_sh_data");
        
        // racers.SetFile("resource/racers/soniccar.zif",
        //     File.ReadAllBytes($"{game}/resource/racers/mechasonic.zif"));
        // racers.SetFile("resource/racers/soniccar.zig",
        //     File.ReadAllBytes($"{game}/resource/racers/mechasonic.zig"));
        
        
        // tracks.SetFile("resource/tracks/seasidehill_easy.zif",
        //     File.ReadAllBytes($"{game}/resource/tracks/doomeggzone_dlc.zif"));
        // tracks.SetFile("resource/tracks/seasidehill_easy.zig",
        //     File.ReadAllBytes($"{game}/resource/tracks/doomeggzone_dlc.zig"));
        //
        // tracks.SetFile("resource/tracks/seasidehill_easy_pcrt_sh_data.zif",
        //     File.ReadAllBytes($"{game}/resource/tracks/doomeggzone_dlc_pcrt_sh_data.zif"));
        // tracks.SetFile("resource/tracks/seasidehill_easy_pcrt_sh_data.zig",
        //     File.ReadAllBytes($"{game}/resource/tracks/doomeggzone_dlc_pcrt_sh_data.zig"));
        //
        // ai.SetFile("resource/ai/ai_seasidehill_easy.txt", File.ReadAllBytes($"{xbox}/resource/ai/ai_doomeggzone_dlc.txt"));
        
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
            
            // keyframes
            // objects
            
            if (siff.HasResource(SiffResourceType.SceneLibrary))
                target.SetResource(siff.LoadResource<SceneLibrary>(SiffResourceType.SceneLibrary), SiffResourceType.SceneLibrary);
            
            // font
            
            if (siff.HasResource(SiffResourceType.TextPack))
                target.SetResource(siff.LoadResource<TextPack>(SiffResourceType.TextPack), SiffResourceType.TextPack);
        }
    }

    private static void PublishFile(string path, byte[] data)
    {
        path = $"{game}\\{path}";
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
            string local = "F:/sart/ssr/DLC/PS3/Extract/" + regional;
            if (!File.Exists(local)) continue;
            
            byte[] data = ConvertPackageToWin32(local);
            if (regional.EndsWith("_en.stz"))
                PublishFile(regional.Replace("_en.stz", "_us.stz"), data);
            PublishFile(regional, data);
        }
    }
    
    private static void PublishSiff(string path)
    {
        string local = "F:/sart/ssr/DLC/XBOX/Extract/" + path;
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