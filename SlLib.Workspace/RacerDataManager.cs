using System.Numerics;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SlLib.Excel;
using SlLib.Extensions;
using SlLib.Filesystem;
using SlLib.IO;
using SlLib.Resources;
using SlLib.Resources.Database;
using SlLib.SumoTool;
using SlLib.SumoTool.Siff;
using SlLib.SumoTool.Siff.Entry;
using SlLib.Utilities;

namespace SlLib.Workspace;

public class RacerDataManager
{
    private static readonly string[] LanguageExtensions = ["en", "fr", "ge", "it", "jp", "sp", "us"];
    private static readonly string[] CommonSumoToolPackages =
    [
        "ui/frontend/commonprojects/commonprojects",
        "ui/frontend/newfe/mainfe",
        "ui/frontend/leaderboard/leaderboard",
        "ui/frontend/progressionrewards/progressionrewards",
        "ui/frontend/resultsstandard/resultsstandard",
        "ui/frontend/top3sequence/top3sequence",
        "ui/frontend/versus_overlay_screen/versus_overlay_screen",
        "ui/frontend/hud_elements/hud_elements"
    ];
    
    private readonly IFileSystem _fs;
    private readonly ExcelData _racerData;
    private readonly Worksheet _racers;
    private readonly string _buildFolder;
    private readonly List<SumoTexturePack> _texturePackHandles = [];
    private readonly List<SumoSceneDatabase> _databaseHandles = [];
    private readonly SlPlatformContext _platformInfo = SlPlatform.Win32.GetDefaultContext();
    
    public RacerDataManager(IFileSystem fs, string buildFolder)
    {
        _buildFolder = buildFolder;
        _fs = fs;
        if (!fs.DoesExcelDataExist("gamedata/racers"))
            throw new FileNotFoundException("Racers file doesn't exist in filesystem!");

        _racerData = fs.GetExcelData("gamedata/racers");
        
        Worksheet? worksheet = _racerData.GetWorksheet("Racers");
        _racers = worksheet ?? throw new NullReferenceException("Racers worksheet was null!");
    }
    
    public void RegisterRacer(string id, RacerImportSetting settings)
    {
        Column? racer = _racers.GetColumnByName(id);
        if (racer == null)
            throw new NullReferenceException(
                $"Could not find racer {id} and adding new racers is currently unsupported!");
        
        if (string.IsNullOrEmpty(settings.InternalId))
            settings.InternalId = id;
        
        Console.WriteLine($"[RacerDataManager] Starting import stages for {settings.DisplayName} over {racer.GetString("DisplayName")}");
        RegisterRacerSpritesInternal(id, racer, settings);
        Console.WriteLine($"[RacerDataManager] Registering models for {settings.DisplayName}");

        string scene = racer.GetString("CharacterMayaFile") + ".mb:";
        List<SlResourceDatabase> databases =
        [
            OpenScene($"localcharacters/{id}/{id}"),
            OpenScene($"fecharacters/{id}_fe/{id}_fe"),
            OpenScene($"characters/{id}/{id}")
        ];

        if (!string.IsNullOrEmpty(settings.GlbSourcePath))
        {
            List<SlModel> models = [];
            
            // Pull the root character models from each database
            foreach (SlResourceDatabase database in databases)
            {
                SlModel? model = database.GetResourcesOfType<SlModel>().Find(model =>
                {
                    string name = model.Header.Name;
                    if (!name.Contains(scene)) return false;
                    if (!name.Contains($"se_entity_{id}|")) return false;
                    return !name.Contains("skeleton") && !name.Contains("car") && !name.Contains("boat") && !name.Contains("plane") && !name.Contains("transform");
                });

                if (model == null)
                    throw new Exception($"Couldn't find root model for {settings.DisplayName} in database!");

                models.Add(model);
            }
        
            // Create a temporary database to store all generated assets in
            var workspace = new SlResourceDatabase(SlPlatform.Win32);
            SlSkeleton? skeleton = models.First().Resource.Skeleton.Instance;
            var config = new SlImportConfig(workspace, settings.GlbSourcePath)
            {
                Skeleton = skeleton,
                BoneRemapCallback = settings.GlbBoneRemapCallback,
                VirtualFilePath = "assets/default/characters/" + settings.InternalId,
                VirtualSceneName = settings.InternalId,
            };
            
            SlModel import = new SlSceneImporter(config).Import();
            
            // For each database, replace the root model with this newly imported one,
            // and copy all resources from the workspace directory
            for (int i = 0; i < databases.Count; ++i)
            {
                SlResourceDatabase database = databases[i];
                SlModel model = models[i];
                
                model.Materials = import.Materials;
                model.WorkArea = import.WorkArea;
                model.Resource.Segments = import.Resource.Segments;
                model.Resource.RenderCommands = import.Resource.RenderCommands;
                model.Resource.PlatformResource = import.Resource.PlatformResource;
                
                workspace.CopyTo(database);
                database.AddResource(model);
            }
        }

        foreach (var config in settings.MaterialConstantReplacements)
        {
            foreach (SlResourceDatabase database in databases)
            {
                var material = database.FindResourceByPartialName<SlMaterial2>(config.PartialName);
                if (material == null) continue;

                bool modified = false;
                foreach (string constant in config.Constants.Keys)
                {
                    if (!material.HasConstant(constant)) continue;
                    modified = true;
                    material.SetConstant(constant, config.Constants[constant]);
                }

                if (modified)
                {
                    Console.WriteLine($"Replacing {material.Header.Name} due to changed constants");
                    database.AddResource(material);   
                }
            }
        }
        
        foreach (var config in settings.TextureReplacements)
        {
            using var image = Image.Load<Rgba32>(config.Texture);
            var importedTexture = new SlTexture(string.Empty, image, false);
            foreach (SlResourceDatabase database in databases)
            {
                var existing = database.FindResourceByPartialName<SlTexture>(config.PartialName);
                if (existing == null) continue;
                importedTexture.Header = existing.Header;
                database.AddResource(importedTexture);
            }
        }
        
        Console.WriteLine($"[RacerDataManager] Finished import stages for {settings.DisplayName}");
    }
    
    public void RegisterCommonSprite(int hash, string texture)
    {
        Console.WriteLine($"Registering common sprite [{(uint)hash}]={texture}");
        using var image = Image.Load<Rgba32>(texture);
        foreach (string pack in CommonSumoToolPackages) 
            OpenTexturePack(pack).AddSprite(hash, image);
    }
    
    public void RegisterRaceResults(string id, int hash, string texture)
    {
        Console.WriteLine($"Registering race results package with [{(uint)hash}]={texture} [hash={SlUtil.SumoHash("CHAR_" + id.ToUpper())}]");
        // Specifically for race results, it's pointless to load the existing packs,
        // since they generally only contain a single sprite.
        var pack = new TexturePack();
        pack.AddSprite(hash, texture);
        
        var scene = new SceneLibrary
        {
            Scenes = [new SceneTableEntry(SlUtil.SumoHash("CHAR_" + id.ToUpper()))]
        };
            
        var siff = new SiffFile(_platformInfo);
        siff.SetResource(pack, SiffResourceType.TexturePack);
        siff.SetResource(scene, SiffResourceType.SceneLibrary);
        var package = new SumoToolPackage(_platformInfo);
        package.SetLocaleData(siff);
            
        // Make sure we're saving a file for each language extension
        byte[] data = package.Save();
        string path = $"ui/frontend/raceresults/raceresults{id}";
        foreach (string extension in LanguageExtensions)
            PublishFile($"{path}_{extension}.stz", data);
    }
    
    private void RegisterRacerSpritesInternal(string id, Column racer, RacerImportSetting settings)
    {
        Console.WriteLine($"[RacerDataManager] Registering sprites for {settings.DisplayName}...");
        if (!string.IsNullOrEmpty(settings.RaceResultsPortrait))
            RegisterRaceResults(id, (int)racer.GetUint("ImageUnlockHash"), settings.RaceResultsPortrait);

        List<(int Hash, Image<Rgba32> Image)> sprites = [];
        if (!string.IsNullOrEmpty(settings.CharSelectIcon))
            sprites.Add(((int)racer.GetUint("CharSelectIconHash"), Image.Load<Rgba32>(settings.CharSelectIcon)));
        if (!string.IsNullOrEmpty(settings.VersusPortrait))
            sprites.Add(((int)racer.GetUint("CharSelectBigIconHash"), Image.Load<Rgba32>(settings.VersusPortrait)));
        if (!string.IsNullOrEmpty(settings.MiniMapIcon))
        {
            var hashes = new HashSet<int>
            {
                (int)racer.GetUint("MiniMapIcon"),
                (int)racer.GetUint("MiniMapIcon_Car"),
                (int)racer.GetUint("MiniMapIcon_Boat"),
                (int)racer.GetUint("MiniMapIcon_Plane")
            };

            var image = Image.Load<Rgba32>(settings.MiniMapIcon);
            foreach (int hash in hashes)
                sprites.Add((hash, image));
        }

        foreach (string stz in CommonSumoToolPackages)
        {
            SumoTexturePack pack = OpenTexturePack(stz);
            Console.WriteLine($"[RacerDataManager]\tFinding and replacing sprites in {pack.Path}...");
            foreach ((int hash, var image) in sprites)
                pack.SetSprite(hash, image);
        }
        
        foreach ((int hash, var image) in sprites)
            image.Dispose();
    }

    private void PublishTexturePack(SumoTexturePack pack)
    {
        if (!_texturePackHandles.Contains(pack))
            throw new Exception("Texture pack handle isn't registered by this manager!");

        if (pack.HasChanges)
        {
            foreach (SumoTexturePackRegion region in pack.Regions)
            {
                region.Siff.SetResource(region.Pack, SiffResourceType.TexturePack);
                var package = new SumoToolPackage(_platformInfo);
                package.SetLocaleData(region.Siff);
                PublishFile($"{pack.Path}_{region.Extension}.stz", package.Save());
            }   
        }
        
        _texturePackHandles.Remove(pack);
    }
    
    private void PublishFile(string path, byte[] data)
    {
        path = Path.Combine(_buildFolder, path);
        string? directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(directory))
            Directory.CreateDirectory(directory);
        File.WriteAllBytes(path, data);
    }
    
    private SlResourceDatabase OpenScene(string path)
    {
        SumoSceneDatabase? database = _databaseHandles.Find(database => database.Path == path);
        if (database != null) return database.Database;

        database = new SumoSceneDatabase { Path = path };
        if (!_fs.DoesSceneExist(path))
            throw new FileNotFoundException($"{path} doesn't exist in filesystem!");
        database.Database = _fs.GetSceneDatabase(path);
        _databaseHandles.Add(database);
        return database.Database;
    }
    
    private SumoTexturePack OpenTexturePack(string path)
    {
        SumoTexturePack? pack = _texturePackHandles.Find(pack => pack.Path == path);
        if (pack != null) return pack;

        pack = new SumoTexturePack
        {
            Path = path
        };
        
        foreach (string extension in LanguageExtensions)
        {
            if (!_fs.DoesSumoToolFileExist(path, extension)) continue;
            SumoToolPackage package = _fs.GetSumoToolPackage(path, extension);
            if (!package.HasLocaleData())
                throw new NullReferenceException("Sumo tool package has NULL locale data!");

            SiffFile siff = package.GetLocaleSiff();
            if (!siff.HasResource(SiffResourceType.TexturePack))
                throw new NullReferenceException("Siff file doesn't contain texture pack!");

            var region = new SumoTexturePackRegion
            {
                Extension = extension,
                Pack = siff.LoadResource<TexturePack>(SiffResourceType.TexturePack),
                Siff = siff
            };
            
            pack.Regions.Add(region);
        }

        if (pack.Regions.Count == 0)
            throw new Exception($"Sumo texture pack at {path} either doesn't exist or contains no regions!");
        
        _texturePackHandles.Add(pack);
        return pack;
    }

    public void Flush()
    {
        Console.WriteLine("[RacerDataManager] Flushing all texture packs to build directory...");
        List<SumoTexturePack> packs = [.._texturePackHandles];
        foreach (SumoTexturePack pack in packs)
        {
            Console.WriteLine($"[RacerDataManager]\tPublishing {pack.Path} w/ {pack.Regions.Count} regional extensions...");
            PublishTexturePack(pack);   
        }
        
        Console.WriteLine("[RacerDataManager] Flushing all scene databases to build directory...");
        foreach (SumoSceneDatabase database in _databaseHandles)
        {
            Console.WriteLine($"[RacerDataManager]\tPublishing {database.Path}");
            string extension = database.Database.Platform.Extension;
            database.Database.RemoveUnusedResources();
            
            //
            // SlSceneExporter.Export(database.Database, $"F:/sart/export/" + database.Path);
            
            (byte[] cpuData, byte[] gpuData) = database.Database.Save();
            PublishFile($"{database.Path}.cpu.s{extension}", cpuData);
            PublishFile($"{database.Path}.gpu.s{extension}", gpuData);
        }
    }
    
    public class RacerImportSetting
    {
        /// <summary>
        ///     Path to glTF 2.0 Binary file for racer model.
        /// </summary>
        public string? GlbSourcePath;

        /// <summary>
        ///     Internal ID used for asset paths when importing resources.
        /// </summary>
        public string? InternalId;
        
        /// <summary>
        ///     Optional callback for remapping bones in the racer model.
        /// </summary>
        public Func<List<(string Bone, int Parent)>, int, string>? GlbBoneRemapCallback;
        
        /// <summary>
        ///     New display name to use for the racer.
        /// </summary>
        public string DisplayName = string.Empty;
        
        /// <summary>
        ///     Path to image file for race results portrait.
        /// </summary>
        public string RaceResultsPortrait = string.Empty;
        
        /// <summary>
        ///     Path to image file for versus portrait.
        /// </summary>
        public string VersusPortrait = string.Empty;
        
        /// <summary>
        ///     Path to image file for character select icon.
        /// </summary>
        public string CharSelectIcon = string.Empty;
        
        /// <summary>
        ///     Path to image file for minimap icon.
        /// </summary>
        public string MiniMapIcon = string.Empty;

        /// <summary>
        ///     Optional textures to replace.
        /// </summary>
        public List<TextureReplacementConfig> TextureReplacements = [];

        /// <summary>
        ///     Optional material constants to replace.
        /// </summary>
        public List<MaterialConstantReplacementConfig> MaterialConstantReplacements = [];
    }

    public struct TextureReplacementConfig
    {
        /// <summary>
        ///     Partial or full name of the texture to replace.
        /// </summary>
        public string PartialName;
        
        /// <summary>
        ///     Path to image file to use as replacement.
        /// </summary>
        public string Texture;
    }

    public struct MaterialConstantReplacementConfig
    {
        /// <summary>
        ///     Partial or full name of the material to edit.
        /// </summary>
        public string PartialName;

        /// <summary>
        ///     Constant map to replace.
        /// </summary>
        public Dictionary<string, Vector4> Constants;
    }

    private class SumoSceneDatabase
    {
        public string Path;
        public SlResourceDatabase Database;
    }
    
    private class SumoTexturePack
    {
        public string Path;
        public List<SumoTexturePackRegion> Regions = [];
        public bool HasChanges;
        
        public void SetSprite(int id, Image<Rgba32> image)
        {
            foreach (SumoTexturePackRegion region in Regions)
            {
                if (!region.Pack.HasSprite(id)) continue;
                HasChanges = true;
                region.Pack.AddSprite(id, image);
            }
        }
        
        public void AddSprite(int id, Image<Rgba32> image)
        {
            foreach (SumoTexturePackRegion region in Regions)
            {
                HasChanges = true;
                region.Pack.AddSprite(id, image);
            }
        }
    }

    private class SumoTexturePackRegion
    {
        public string Extension;
        public SiffFile Siff;
        public TexturePack Pack;
    }
}