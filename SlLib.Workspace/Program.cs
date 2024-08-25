using System.Numerics;
using System.Runtime.Serialization;
using System.Security.AccessControl;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using SharpGLTF.Schema2;
using SharpGLTF.Validation;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing;
using SixLabors.ImageSharp.PixelFormats;
using SlLib.Audio;
using SlLib.Extensions;
using SlLib.Filesystem;
using SlLib.IO;
using SlLib.Resources;
using SlLib.Resources.Collision;
using SlLib.Resources.Database;
using SlLib.Resources.Scene.Definitions;
using SlLib.Serialization;
using SlLib.SumoTool;
using SlLib.SumoTool.Siff;
using SlLib.SumoTool.Siff.Entry;
using SlLib.SumoTool.Siff.Keyframe;
using SlLib.SumoTool.Siff.Objects;
using SlLib.Utilities;
using SlLib.Workspace;
using Path = System.IO.Path;
using Scene = SlLib.SumoTool.Siff.Entry.Scene;

const string gameDirectory =
    @"C:\Program Files (x86)\Steam\steamapps\common\Sonic & All-Stars Racing Transformed\Data\";
const string outputDirectory = @"F:\sart\build";
// const string outputDirectory = gameDirectory;
const string workDirectory = @"F:\sart\";
const bool loadConversionCache = false;

SlResourceDatabase shaderCache, textureCache;
IFileSystem fs, fs64;
IFileSystem ssr, workfs, publishfs;
SetupDataCaches();

DoRacerReplacements();
//GridTests();

return;

void CollisionTreeHierarchy()
{
    SlResourceDatabase database = fs.GetSceneDatabase("levels/seasidehill2/seasidehill2");
    var collision = database.FindResourceByPartialName<SlResourceCollision>("seasidehill2_collision.mb") ??
                    throw new FileNotFoundException("Come on, man");
    
    Console.WriteLine("DEBUGGER MARK");


}

void CollisionTesting()
{
    List<SlCollisionMaterial> materials = [];
    List<string> files = Directory.EnumerateFiles("F:/sart/game/pc/", "*.cpu.spc", SearchOption.AllDirectories).ToList();
    foreach (var file in files)
    {
        SlResourceDatabase database;
        try
        {
            database = SlResourceDatabase.Load(file, file.Replace(".cpu.", ".gpu."));
            var collisions = database.GetResourcesOfType<SlResourceCollision>();
            foreach (var collision in collisions)
            {
                materials.AddRange(collision.Mesh.Materials);
            }
        }
        catch (Exception ex)
        {
            continue;
            
        }
        

    }
    
    materials.Sort((a, z) => a.Type - z.Type);
    string json = JsonSerializer.Serialize(materials, new JsonSerializerOptions { WriteIndented = true, IncludeFields = true });
    File.WriteAllText("C:/Users/Aidan/Desktop/materials.json", json);
}

void GridTests()
{
    SumoToolPackage package = workfs.GetSumoToolPackage("build/ui/frontend/newfe/mainfe", "en");
    SiffFile locale = package.GetLocaleSiff();
    var objects = locale.LoadResource<ObjectDefLibrary>(SiffResourceType.ObjectDefLibrary);
    var keyframes = locale.LoadResource<KeyframeLibrary>(SiffResourceType.KeyFrameLibrary);
    var scenes = locale.LoadResource<SceneLibrary>(SiffResourceType.SceneLibrary);
    var textures = locale.LoadResource<TexturePack>(SiffResourceType.TexturePack);
    
    Scene characterSelectScene = scenes.FindScene("CHARACTER_SELECT") ?? throw new Exception("Couldn't find character select scene");
    GroupObject mainGroup = objects.GetObjectDef<GroupObject>("CHARACTER_SELECT/MAINGROUP") ?? throw new Exception("Couldn't find main group!");
    
    
    const int gridSizeX = 6;
    const int gridSizeY = 9;

    string[] selectFormatStrings = new string[]
    {
        "CHARACTER_SELECT/CHAR_%d_MOVE",
        "CHARACTER_SELECT/CHAR_%d",
        "CHARACTER_SELECT/CHAR_GROUP_%d",
        // some unknown one, seems to be the background frame
        // some unknown one, seems to be background color
        "CHARACTER_SELECT\\CHAR_GLOW_%d",
        "CHARACTER_SELECT\\CHAR_STATE_%d",
        "CHARACTER_SELECT\\MAXED_ICON_%d",
        "CHARACTER_SELECT\\PROGRESS_%d",
        // some unknown one, seems to be foreground frame
        "CHARACTER_SELECT/FLARE_%d",
    };

    var gridGroup = objects.FindGroupContaining("CHARACTER_SELECT/CHAR_0_MOVE") ??
                    throw new Exception("Couldn't find character group!");

    for (int i = 0; i < gridSizeX * gridSizeY; ++i)
    {
        var state = objects.GetObjectDef<TextureObject>("CHARACTER_SELECT/CHAR_STATE_" + i);
        if (state == null)
            AddCharacterSlot(i);
    }
    
    for (int i = 0; i < gridSizeX * gridSizeY; ++i)
    {
        int xpos = i % gridSizeX;
        int ypos = i / gridSizeX;
        
        string[] paths = {
            "CHARACTER_SELECT/CHAR_" + i,
            "CHARACTER_SELECT/HILITE_P1_" + i + "_POS",
            "CHARACTER_SELECT/CHAR_" + i + "_MOVE",
            "CHARACTER_SELECT/MAXED_ICON_" + i
        };
        
        foreach (string path in paths)
        {
            KeyframeEntry? keyframe = keyframes.GetKeyframe(path);
            if (keyframe == null) continue;
            
            bool isCharacter = path == paths[0];
            bool isHighlight = path.Contains("HILITE_");
            
            foreach (KeyframeData frame in keyframe.Data)
            {
                frame.Scale *= 0.8f;
                if (isCharacter)
                {
                    frame.X = -0.3671875f + (xpos * 0.0859375f);
                    frame.Y = -0.2763889f - (ypos * -0.1083334f);
                    
                    frame.X -= 0.025f;
                }
            }   
            
            if (isHighlight)
            {
                for (int j = 0; j < gridSizeX * gridSizeY; ++j)
                {
                    KeyframeData? frame = keyframe.GetKeyFrame("POS_" + j);
                    if (frame == null)
                    {
                        frame = new KeyframeData
                        {
                            Hash = SlUtil.SumoHash($"POS_" + j),
                            FrameNumber = 92 + j
                        };
                        
                        keyframe.Data.Add(frame);
                    }
                    
                    frame.Scale = new Vector2(0.6f);

                    int cposx = j % gridSizeX;
                    int cposy = j / gridSizeX;
                    
                    frame.X = -0.206f + (cposx * 0.07f) - (cposx * 0.0015f);
                    frame.Y = -0.146f + (cposy * 0.0875f) - (cposy * 0.001f);
                }
            }
            
            Console.WriteLine($"{path}: {keyframe.Data[0].X},{keyframe.Data[0].Y}");
        }
    }
    
    locale.SetResource(keyframes, SiffResourceType.KeyFrameLibrary);
    locale.SetResource(objects, SiffResourceType.ObjectDefLibrary);
    package.SetLocaleData(locale);

    byte[] data = package.Save(compress: true);
    File.WriteAllBytes($"{gameDirectory}/ui/frontend/newfe/mainfe_en.stz", data);
    File.WriteAllBytes($"{gameDirectory}/ui/frontend/newfe/mainfe_us.stz", data);

    return;

    void AddCharacterSlot(int index)
    {
        GroupObject move = DuplicateObjectDef<GroupObject>("CHARACTER_SELECT\\CHAR_0_MOVE", $"CHARACTER_SELECT\\CHAR_{index}_MOVE");
        GroupObject ch = DuplicateObjectDef<GroupObject>("CHARACTER_SELECT\\CHAR_0", $"CHARACTER_SELECT\\CHAR_{index}");

        GroupObject group = DuplicateObjectDefByHash<GroupObject>(-1310814773, $"CHARACTER_SELECT\\CHAR_GROUP_{index}");
        GroupObject flare = DuplicateObjectDef<GroupObject>("CHARACTER_SELECT\\FLARE_0", $"CHARACTER_SELECT\\FLARE_{index}");

        DuplicateObjectDefByHash<TextureObject>(-681311288, $"CHARACTER_SELECT\\CHAR_{index}_BACKGROUND_A");
        DuplicateObjectDefByHash<TextureObject>(-1134212054, $"CHARACTER_SELECT\\CHAR_{index}_BACKGROUND_B");
        DuplicateObjectDef<TextureObject>("CHARACTER_SELECT\\CHAR_GLOW_0", $"CHARACTER_SELECT\\CHAR_GLOW_{index}");
        DuplicateObjectDef<TextureObject>("CHARACTER_SELECT\\CHAR_STATE_0", $"CHARACTER_SELECT\\CHAR_STATE_{index}");
        DuplicateObjectDef<TextureObject>("CHARACTER_SELECT\\MAXED_ICON_0", $"CHARACTER_SELECT\\MAXED_ICON_{index}");
        DuplicateObjectDefByHash<TextureObject>(860070039, $"CHARACTER_SELECT\\CHAR_{index}_FOREGROUND");

        DuplicateObjectDefByHash<TextureObject>(525478085, $"CHARACTER_SELECT\\FLARE_{index}_A");
        DuplicateObjectDefByHash<TextureObject>(-527029543, $"CHARACTER_SELECT\\FLARE_{index}_B");
        
        move.ObjectHashes.Add(SlUtil.SumoHash($"CHARACTER_SELECT\\CHAR_{index}"));
        
        ch.ObjectHashes.Add(SlUtil.SumoHash($"CHARACTER_SELECT\\CHAR_GROUP_{index}"));
        ch.ObjectHashes.Add(SlUtil.SumoHash($"CHARACTER_SELECT\\FLARE_{index}"));
        
        group.ObjectHashes.Add(SlUtil.SumoHash($"CHARACTER_SELECT\\CHAR_{index}_BACKGROUND_A"));
        group.ObjectHashes.Add(SlUtil.SumoHash($"CHARACTER_SELECT\\CHAR_{index}_BACKGROUND_B"));
        group.ObjectHashes.Add(SlUtil.SumoHash($"CHARACTER_SELECT\\CHAR_GLOW_{index}"));
        group.ObjectHashes.Add(SlUtil.SumoHash($"CHARACTER_SELECT\\CHAR_STATE_{index}"));
        group.ObjectHashes.Add(SlUtil.SumoHash($"CHARACTER_SELECT\\MAXED_ICON_{index}"));
        group.ObjectHashes.Add(SlUtil.SumoHash($"CHARACTER_SELECT\\CHAR_{index}_FOREGROUND"));
        
        flare.ObjectHashes.Add(SlUtil.SumoHash($"CHARACTER_SELECT\\FLARE_{index}_A"));
        flare.ObjectHashes.Add(SlUtil.SumoHash($"CHARACTER_SELECT\\FLARE_{index}_B"));
        
        gridGroup.ObjectHashes.Add(SlUtil.SumoHash($"CHARACTER_SELECT\\CHAR_{index}_MOVE"));
    }

    T DuplicateObjectDefByHash<T>(int target, string name) where T : IObjectDef
    {
        return DuplicateObjectDefInternal<T>(target, SlUtil.SumoHash(name));
    }

    T DuplicateObjectDef<T>(string name, string target) where T : IObjectDef
    {
        return DuplicateObjectDefInternal<T>(SlUtil.SumoHash(name), SlUtil.SumoHash(target));
    }
    
    T DuplicateObjectDefInternal<T>(int original, int copy) where T : IObjectDef
    {
        var def = objects.GetObjectDef<T>(original);
        if (def == null)
            throw new KeyNotFoundException($"Key was not found in object definition map!");

        var saveContext = new ResourceSaveContext();
        ISaveBuffer buffer =
            saveContext.Allocate(def.GetSizeForSerialization(SlPlatform.Win32, SlPlatform.Win32.DefaultVersion));
        saveContext.SaveObject(buffer, def, 0);
        (byte[] cpuData, byte[] _) = saveContext.Flush();

        var loadContext = new ResourceLoadContext(cpuData);
        
        int keyframeHash;
        IObjectDef clone;
        switch (def.ObjectType)
        {
            case "TXTR":
            {
                var obj = loadContext.LoadObject<TextureObject>();
                clone = obj;
                keyframeHash = obj.KeyframeHash;
                obj.KeyframeHash = copy;
                break;
            }
            case "GROP":
            {
                var obj = loadContext.LoadObject<GroupObject>();
                clone = obj;
                keyframeHash = obj.KeyframeHash;
                obj.KeyframeHash = copy;
                obj.ObjectHashes.Clear();
                break;
            }
            default: throw new SerializationException("Unknown object definition type!");
        }
        
        KeyframeEntry? keyframe = keyframes.GetKeyframe(keyframeHash);
        if (keyframe == null) throw new KeyNotFoundException("Failed to load keyframe!");
        
        keyframes.Keyframes.Add(keyframe.Duplicate(copy));
        objects.Objects[copy] = clone;
        
        return (T)clone;
    }




}

void Ouji()
{
    // Local/Remote Character
    {
        var database = new SlResourceDatabase(SlPlatform.Win32);
        var importer = new SlSceneImporter(new SlImportConfig(database, "F:/sart/import/ouji/export/ouji_car.glb")
        {
            VirtualFilePath = "assets/default/characters/ouji",
            VirtualSceneName = "ouji_car",
            ImportType = SlImportType.Car
        });
        
        
        importer.ImportHierarchy();

        importer = new SlSceneImporter(new SlImportConfig(database, "F:/sart/import/ouji/export/ouji.glb")
        {
            VirtualFilePath = "assets/default/characters/ouji",
            VirtualSceneName = "ouji",
            ImportType = SlImportType.Character
        });
        
        importer.ImportHierarchy();
        
        importer = new SlSceneImporter(new SlImportConfig(database, "F:/sart/import/ouji/export/ouji_plane.glb")
        {
            VirtualFilePath = "assets/default/characters/ouji",
            VirtualSceneName = "ouji_plane",
            ImportType = SlImportType.Plane
        });
        
        importer.ImportHierarchy();
        
        importer = new SlSceneImporter(new SlImportConfig(database, "F:/sart/import/ouji/export/ouji_boat.glb")
        {
            VirtualFilePath = "assets/default/characters/ouji",
            VirtualSceneName = "ouji_boat",
            ImportType = SlImportType.Boat
        });
        
        importer.ImportHierarchy();
        
        importer = new SlSceneImporter(new SlImportConfig(database, "F:/sart/import/ouji/export/ouji_transform.glb")
        {
            VirtualFilePath = "assets/default/characters/ouji",
            VirtualSceneName = "ouji_transform",
            ImportType = SlImportType.Transform
        });
        
        importer.ImportHierarchy();
        
        
        database.Save($"{outputDirectory}/localcharacters/ouji/ouji.cpu.spc",
            $"{outputDirectory}/localcharacters/ouji/ouji.gpu.spc", inMemory: true);
        
        database.Save($"{outputDirectory}/characters/ouji/ouji.cpu.spc",
            $"{outputDirectory}/characters/ouji/ouji.gpu.spc", inMemory: true);
    }

    // Frontend Character
    {
        var database = new SlResourceDatabase(SlPlatform.Win32);
        var importer = new SlSceneImporter(new SlImportConfig(database, "F:/sart/import/ouji/export/fe/ouji_car.glb")
        {
            VirtualFilePath = "assets/default/characters/ouji",
            VirtualSceneName = "ouji_car",
            ImportType = SlImportType.Car
        });
        importer.ImportHierarchy();
        
        importer = new SlSceneImporter(new SlImportConfig(database, "F:/sart/import/ouji/export/fe/ouji.glb")
        {
            VirtualFilePath = "assets/default/characters/ouji",
            VirtualSceneName = "ouji",
            ImportType = SlImportType.Character
        });
        
        importer.ImportHierarchy();
        
        database.Save($"{outputDirectory}/fecharacters/ouji_fe/ouji_fe.cpu.spc",
            $"{outputDirectory}/fecharacters/ouji_fe/ouji_fe.gpu.spc", inMemory: true);
    }
    
    DoRacerReplacements();
}


void DoAkTests()
{
    string path = $"C:\\Program Files (x86)\\Steam\\steamapps\\common\\Sonic & All-Stars Racing Transformed\\Data\\sound\\SoundBanks.pck";
    var package = AkFilePackage.Load(path);
    
    Console.WriteLine(package.SoundBanks.Count);
    Console.WriteLine(package.StreamedFiles.Count);
    Console.WriteLine(package.LanguageMap.Count);
    Console.WriteLine(package.Externals.Count);

    return;
    
    
    int hash = SlUtil.HashString("chvo_ryo");

    // 1870
    // 1871

    using var fs = File.OpenRead(path);
    foreach (var bank in package.SoundBanks)
    {

        fs.Seek(bank.StartBlock, SeekOrigin.Begin);
        byte[] data = new byte[bank.FileSize];
        fs.ReadExactly(data);
        
        
        int offset = 0;
        string name = ((uint)bank.FileId).ToString();
        while (offset < data.Length)
        {
            int tag = data.ReadInt32(offset);
            int size = data.ReadInt32(offset + 4);
            
            if (tag == 0x44495453)
            {
                name = Encoding.UTF8.GetString(data, offset + 0x15, data[offset + 0x14]);
            }
        
            offset += size + 0x8;
        }
        
        File.WriteAllBytes($"C:/Users/Aidan/Desktop/Banks/{name}.bnk", data);

        
        // Console.WriteLine($"Bank ID: " + bank.FileId); 
        // Console.WriteLine($"BlockSize: " + bank.BlockSize);
        // Console.WriteLine($"StartBlock: " + bank.StartBlock);
        // Console.WriteLine($"NumBlocks: " + bank.FileSize);
        // Console.WriteLine();
    }
    
}

void SquirrelCube()
{
    // flags = 0x12 for pickupstar
    var model = workfs.GetSceneDatabase("pickupstar").GetResourcesOfType<SlModel>()[0];
    
    var database = new SlResourceDatabase(SlPlatform.Win32);
    var importer = new SlSceneImporter(new SlImportConfig(database, "F:/sart/import/cubetest1/cubetest1.glb"));
    importer.ImportHierarchy();
    
    database.Save("F:/sart/export/cubetest1.cpu.spc", "F:/sart/export/cubetest1.gpu.spc");
}

void AnimResearch()
{
    // FIRST ANIM SAMPLED: assets/default/frontend/arena_islands/scenes/afterburner_arena.mb:se_entity_afterburner_arena|se_animator_afterburner|se_anim_stream_afterburner|idle01.anim
    // CA 77 EB 07 35 E2 B0 3E F7 57 17 E0 C4 3D DF C1
    
    // 0x48 - should be 0x54 bytes?
    // 0x9c - should be 0x20 bytes?
    
    // unpack
        // + 0x18 = 1 for rotation, 3 for position, 3 for scale, 1 for attributes
    
    // EvaluateFrames - Rotation
        // param_1 = rotation_joint_data
        // param_2 = ???
        // param_3 = num_rotation_joints
        // param_4 = SlAnimUnpack*?
            // 0x0 = *(leaf + 0x4) // basis data for each bone? (THE FLOATS BETTER NOT BE DELTAS FROM THIS)
            // 0x4 = *(leaf + 0xc) // frame data, data for remaining frames
            // 0x8 = *(leaf + *(leaf + 0x20) + 0x4) + *(leaf + 0x20) (basis data from next leaf)
            // 0xc = *(leaf + 0x14) // bits for each bone indicating which frames have keys
            // 0x10 = *(leaf + 0x1e) // unknown bits for each bone (bones that are animated, but have no keys in this set of frames?)
            // 0x14 = *(leaf + 0x16) // end of data marker?
            // 0x18 = 1
            // 0x20 = 0 
            // 0x24 = 0
            // 0x28 = FrameDataOffset
            // ...
            // 0x30 = (leaf.NumFrames + 7) >> 3
            // 0x34 = rotation_commands
            // 0x38 = compression_type
        // param_5 = ???
        // param_6 = ???
        // param_7 = rotation_type
        // param_8 = 0x30
        // param_9 = function (interpolation function?)
        // param_10 = 4 (num components?)
        // param_11 = sumo_bitmask_128_0
        // param_12 = sumo_bitmask_128_1
    // EvaluateFrames - Position
        // 0x0 = *(leaf + 0x6)
        // 0x4 = *(leaf + 0xe)
        // 0x8 = *(leaf + *(leaf + 0x20) + 0x6) + *(leaf + 0x20)
        // 0xc = *(leaf + 0x14) + (num_rotation_joints * ((num_frames + 7) >> 3)
        // 0x10 = *(leaf + 0x1e) + ((num_rotation_joints + 7) >> 3)
        // 0x14 = *(leaf + 0x18)
    // EvaluateFrames - Scales
        // 0x0 = *(leaf + 0x8)
        // 0x4 = *(leaf + 0x10)
        // 0x8 = *(leaf + *(leaf + 0x20) + 0x8) + *(leaf + 0x20)
        // 0xc = *(leaf + 0x14) + (num_rotation_joints * ((num_frames + 7) >> 3)) + (num_position_joints * ((num_frames + 7) >> 3))
        // 0x10 = *(leaf + 0x1e) + ((num_rotation_joints + 7) >> 3) + ((num_position_joints + 7) >> 3))
        // 0x14 = *(leaf + 0x1a)
    // EvaluateFrames - Attributes
        // 0x0 = *(leaf + 0xa)
        // 0x4 = *(leaf + 0x12)
        // 0x8 = *(leaf + *(leaf + 0x20) + 0xa) + *(leaf + 0x20)
        // 0xc = *(leaf + 0x14) + (num_rotation_joints * ((num_frames + 7) >> 3)) + (num_position_joints * ((num_frames + 7) >> 3)) + (num_scale_joints * ((num_frames + 7) >> 3)) 
        // 0x10 = *(leaf + 0x1e) + ((num_rotation_joints + 7) >> 3) + ((num_position_joints + 7) >> 3)) + ((num_scale_joints + 7) >> 3))
        // 0x14 = *(leaf + 0x1c)
    
    SlResourceDatabase database = workfs.GetSceneDatabase("pickupstar");
    SlAnim anim = database.FindResourceByPartialName<SlAnim>($"idle01.anim") ??
                  throw new FileNotFoundException("Couldn't find animation!");
    SlSkeleton skeleton = database.FindResourceByPartialName<SlSkeleton>("se_animator_pickup_star") ??
                          throw new FileNotFoundException("Couldn't find skeleton!");
    
    if (database.GetRawResourceByPartialName<SlAnim>($"idle01.anim", out byte[] cpuData, out byte[] gpuData))
        File.WriteAllBytes($"C:/Users/Aidan/Desktop/idle01.anim", cpuData);
    
    
    string json = JsonSerializer.Serialize(anim, new JsonSerializerOptions { WriteIndented = true, IncludeFields = true });
    File.WriteAllText($"C:/Users/Aidan/Desktop/idle01.json", json);
    
    SlSceneExporter.Export(database, "C:/Users/Aidan/Desktop/PickUpStar");
    
    int offset = 0;
    for (int i = 0; i < anim.ConstantAttributeFrameCommands.Count; ++i)
    {
        float value = SlUtil.DecompressValueBitPacked(anim.ConstantAttributeFrameCommands[i], anim.AttributeAnimData, ref offset);
        var attribute = skeleton.Attributes[anim.ConstantAttributeIndices[i]];
        Console.WriteLine($"Attribute [{i}:{attribute.Name}] Default = {attribute.Default}, Current = {attribute.Value}, Animated = {value}");   
    }    
}

void ObjectDefTests()
{
    // We need to get the GUI modifications working...
    SumoToolPackage package = fs.GetSumoToolPackage("ui/frontend/tutorials/versus_beat_player");
    SiffFile siff = package.GetLocaleSiff();
    var def = siff.LoadResource<ObjectDefLibrary>(SiffResourceType.ObjectDefLibrary);
}

void DoRingReplacement()
{
    var sharedAssetsScene = SlResourceDatabase.Load("F:/sart/allstar-coin.cpu.spc",
        "F:/sart/allstar-coin.gpu.spc");
    var medalEntityModel = sharedAssetsScene.FindResourceByPartialName<SlModel>("se_animator_pickup_star|se_entity_medal.model")!;
    
    var ringDatabase = fs64.GetSceneDatabase("gamemodes/gamemodeassets/ringrace_assets/ringrace_assets", "p2");
    // var medalDatabase = new SlResourceDatabase(SlPlatform.Win32);
    var medalDatabase = SlResourceDatabase.Load("C:/Users/Aidan/Desktop/allstar-coin.cpu.spc",
        "C:/Users/Aidan/Desktop/allstar-coin.gpu.spc");
    ConvertAndRegisterModel("se_entity_ring_gold_a", ringDatabase, medalDatabase);

    medalDatabase.Save("C:/Users/Aidan/Desktop/out/ring.cpu.spc", "C:/Users/Aidan/Desktop/out/ring.gpu.spc");
    
    SlSceneExporter.Export(medalDatabase, "C:/Users/Aidan/Desktop/test1");
    
    var files = Directory.EnumerateFiles($"{gameDirectory}", "*.cpu.spc", SearchOption.AllDirectories);
    foreach (string file in files)
    {
        try
        {
            SlResourceDatabase database = fs.GetSceneDatabase(file.Replace(gameDirectory, string.Empty).Replace(".cpu.spc", ""));
            if (database.ContainsResourceWithHash(medalEntityModel.Resource.Skeleton.Id))
            {
                medalDatabase.CopyTo(database);
                database.Save($"{file}", $"{file.Replace(".cpu.spc", ".gpu.spc")}", inMemory: true);
                Console.WriteLine(file);
            }
        }
        catch
        {
            continue;
        }
    }
}

void DoRacerReplacements()
{ 
    var manager = new RacerDataManager(fs, outputDirectory);
    
    manager.RegisterRaceResults("ouji", SlUtil.SumoHash("PrinceRender.png"), "F:/sart/import/ouji/ui/PrinceRender.png");
    manager.RegisterCommonSprite(SlUtil.SumoHash("PrinceIcon.png"), "F:/sart/import/ouji/ui/PrinceIcon.png");
    manager.RegisterCommonSprite(SlUtil.SumoHash("PrinceMiniMapIcon.png"), "F:/sart/import/ouji/ui/PrinceMiniMapIcon.png");
    manager.RegisterCommonSprite(SlUtil.SumoHash("PrinceRenderVS.png"), "F:/sart/import/ouji/ui/PrinceRenderVS.png");

    manager.RegisterRacer("summer", new RacerDataManager.RacerImportSetting
    {
        GlbSourcePath = $"{workDirectory}/import/kratos/Kratos2.glb",
        GlbBoneRemapCallback = SkeletonUtil.MapGreekSkeleton,
        DisplayName = "Kratos",
        RaceResultsPortrait = $"{workDirectory}/import/kratos/KratosRender.png",
        VersusPortrait = $"{workDirectory}/import/kratos/KratosRenderVs.png",
        CharSelectIcon = $"{workDirectory}/import/kratos/KratosIcon2.png",
        MiniMapIcon = $"{workDirectory}/import/kratos/KratosMinimapIcon.png",
        InternalId = "kratos"
    });
    
    manager.RegisterRacer("aiai", new RacerDataManager.RacerImportSetting
    {
        GlbSourcePath = $"{workDirectory}/import/aiai_classic/ClassicAiAiFixed2.glb",
        GlbBoneRemapCallback = SkeletonUtil.MapMonkeySkeleton,
        DisplayName = "Aiai",
        RaceResultsPortrait = $"{workDirectory}/import/aiai_classic/CAiAiRender.png",
        VersusPortrait = $"{workDirectory}/import/aiai_classic/CAiAiRenderVs.png",
        CharSelectIcon = $"{workDirectory}/import/aiai_classic/CAiAiIcon.png",
        MiniMapIcon = $"{workDirectory}/import/aiai_classic/CAiAiMinimapIcon.png",
        InternalId = "classic_aiai"
    });
    
    manager.RegisterRacer("aiai", new RacerDataManager.RacerImportSetting
    {
        GlbSourcePath = $"{workDirectory}/import/minifaust/minifaust.glb",
        GlbBoneRemapCallback = SkeletonUtil.MapFaustSkeleton,
        DisplayName = "Mini Faust",
        InternalId = "minifaust",
        RaceResultsPortrait = $"{workDirectory}/import/minifaust/MiniFaustRender.png",
        VersusPortrait = $"{workDirectory}/import/minifaust/MiniFaustRenderVs.png",
        CharSelectIcon = $"{workDirectory}/import/minifaust/MiniFaustIcon.png",
        MiniMapIcon = $"{workDirectory}/import/minifaust/MiniFaustMinimapIcon.png",
    });
    
    manager.RegisterRacer("meemee", new RacerDataManager.RacerImportSetting
    {
        GlbSourcePath = $"{workDirectory}/import/jackfrost/jackfrost.glb",
        GlbBoneRemapCallback = SkeletonUtil.MapBipedSkeleton,
        DisplayName = "Jack Frost",
        RaceResultsPortrait = $"{workDirectory}/import/jackfrost/JackFrostRender.png",
        VersusPortrait = $"{workDirectory}/import/jackfrost/JackFrostRenderVs.png",
        CharSelectIcon = $"{workDirectory}/import/jackfrost/JackFrostIcon.png",
        MiniMapIcon = $"{workDirectory}/import/jackfrost/JackFrostMinimapIcon.png",
        InternalId = "jackfrost",
        TextureReplacements = 
        [
            new RacerDataManager.TextureReplacementConfig
            {
                PartialName = "meemee_carmain_diff.tga",
                Texture = $"{workDirectory}/import/jackfrost/jackfrost_carmain_diffuse.png",
            },
            new RacerDataManager.TextureReplacementConfig
            {
                PartialName = "meemeelod_mainbody_diff.tga",
                Texture = $"{workDirectory}/import/jackfrost/jackfrostlod_mainbody_diff.png",
            },
            new RacerDataManager.TextureReplacementConfig
            {
                PartialName = "meemee_plane_boat_diff.tga",
                Texture = $"{workDirectory}/import/jackfrost/jackfrost_plane_boat_diff.png",
            },
            new RacerDataManager.TextureReplacementConfig
            {
                PartialName = "meemeercbomb_diff.tga",
                Texture = $"{workDirectory}/import/jackfrost/jackfrostrcbomb_diff.png",
            },
            new RacerDataManager.TextureReplacementConfig
            {
                PartialName = "meemee_carpaint_pink1_diff.tga",
                Texture = $"{workDirectory}/import/jackfrost/jackfrost_carpaint_diff.png",
            },
            new RacerDataManager.TextureReplacementConfig
            {
                PartialName = "meemee_carpaint_red_diff.tga",
                Texture = $"{workDirectory}/import/jackfrost/jackfrost_carpaint_alt_diff.png",
            },
            new RacerDataManager.TextureReplacementConfig
            {
                PartialName = "asm_meemee_missile_diff.tga",
                Texture = $"{workDirectory}/import/jackfrost/asm_jackfrost_missile_diff.png",
            },
            new RacerDataManager.TextureReplacementConfig
            {
                PartialName = "asm_meemee_missile_3ds_diff.tga",
                Texture = $"{workDirectory}/import/jackfrost/asm_jackfrost_missile_diff.png",
            },
            new RacerDataManager.TextureReplacementConfig
            {
                PartialName = "asm_meemee_petals_diff.tga",
                Texture = $"{workDirectory}/import/jackfrost/asm_jackfrost_snowflakes_diff.png",
            },
        ],
        MaterialConstantReplacements = 
        [
            new RacerDataManager.MaterialConstantReplacementConfig
            {
                PartialName = "meemee_glow.material",
                Constants = new Dictionary<string, Vector4>()
                {
                    ["gColourMul"] = Vector4.One
                }
            }
        ]
    });
    
    manager.RegisterRacer("danicapatrick", new RacerDataManager.RacerImportSetting
    {
        GlbSourcePath = $"{workDirectory}/import/miku/mikitm_sart_test2.glb",
        DisableVehicleLevelOfDetails = true,
        GlbBoneRemapCallback = SkeletonUtil.MapSekaiSkeleton,
        DisplayName = "Hatsune Miku",
        RaceResultsPortrait = $"{workDirectory}/import/miku/mikuentry.png",
        VersusPortrait = $"{workDirectory}/import/miku/mikuportrait.png",
        CharSelectIcon = $"{workDirectory}/import/miku/mikuicon.png",
        MiniMapIcon = $"{workDirectory}/import/miku/mikuminimap_icon_v2.png",
        InternalId = "miku",
        TextureReplacements = 
        [
            new RacerDataManager.TextureReplacementConfig
            {
                PartialName = "danicar_diff.tga",
                Texture = $"{workDirectory}/import/miku/mikucar_diff.png",
            }
        ]
    });

    manager.RegisterRacer("gum", new RacerDataManager.RacerImportSetting
    {
        GlbSourcePath = $"{workDirectory}/import/chigusa/chigusa_sart_gum.glb",
        GlbBoneRemapCallback = SkeletonUtil.MapFortniteMediumSkeleton,
        DisplayName = "Chigusa",
        RaceResultsPortrait = $"{workDirectory}/import/chigusa/chigusa_raceresults.png",
        VersusPortrait = $"{workDirectory}/import/chigusa/chigusa_racericon_big.png",
        CharSelectIcon = $"{workDirectory}/import/chigusa/chigusa_racericon_small.png",
        MiniMapIcon = $"{workDirectory}/import/chigusa/chigusa_mapicon.png",
        InternalId = "chigusa"
    });
    
    manager.RegisterRacer("dragon", new RacerDataManager.RacerImportSetting
    {
        GlbSourcePath = $"{workDirectory}/import/kiryu/kiryu.glb",
        GlbBoneRemapCallback = SkeletonUtil.MapKiryuSkeleton,
        DisplayName = "Kiryu",
        RaceResultsPortrait = $"{workDirectory}/import/kiryu/kiryu_raceresults_v2.png",
        VersusPortrait = $"{workDirectory}/import/kiryu/kiryu_racericon_big_v2.png",
        CharSelectIcon = $"{workDirectory}/import/kiryu/kiryu_racericon_small_v2.png",
        MiniMapIcon = $"{workDirectory}/import/kiryu/kiryu_mapicon.png",
        InternalId = "kiryu",
        TextureReplacements = 
        [
            new RacerDataManager.TextureReplacementConfig
            {
                PartialName = "ryo_ip_specific_diff.tga",
                Texture = $"{workDirectory}/import/kiryu/kiryu_allstar.png",
            }
        ]
    });
    
    manager.RegisterRacer("eggman", new RacerDataManager.RacerImportSetting
    {
        GlbSourcePath = $"{workDirectory}/import/eggman_nega/eggman_nega_sart.glb",
        GlbBoneRemapCallback = SkeletonUtil.MapEggmanNegaSkeleton,
        DisplayName = "Eggman Nega",
        RaceResultsPortrait = $"{workDirectory}/import/eggman_nega/eggman_raceresults.png",
        VersusPortrait = $"{workDirectory}/import/eggman_nega/eggman_racericon_big.png",
        CharSelectIcon = $"{workDirectory}/import/eggman_nega/eggman_racericon_small.png",
        MiniMapIcon = $"{workDirectory}/import/eggman_nega/eggman_mapicon.png",
        InternalId = "eggman_nega",
        TextureReplacements = 
        [
            new RacerDataManager.TextureReplacementConfig
            {
                PartialName = "eggmvehiclemain_diff.tga",
                Texture = $"{workDirectory}/import/eggman_nega/eggmvehiclemain_diff.png",
            },
            new RacerDataManager.TextureReplacementConfig
            {
                PartialName = "eggmvehiclecarpaint-red_diff.tga",
                Texture = $"{workDirectory}/import/eggman_nega/eggmvehiclecarpaint-red_diff.png",
            }
        ]
    });
    
    manager.RegisterRacer("alexkidd", new RacerDataManager.RacerImportSetting
    {
        GlbSourcePath = $"{workDirectory}/import/sackboy/sackboy_sart_alexkidd.glb",
        GlbBoneRemapCallback = SkeletonUtil.MapBipedSkeleton,
        DisplayName = "Sackboy",
        InternalId = "sackboy",
        RaceResultsPortrait = $"{workDirectory}/import/sackboy/SackboyRender.png",
        VersusPortrait = $"{workDirectory}/import/sackboy/SackboyRenderVs.png",
        CharSelectIcon = $"{workDirectory}/import/sackboy/SackboyIcon.png",
        MiniMapIcon = $"{workDirectory}/import/sackboy/SackboyMinimapIcon.png",
    });

    manager.Flush();      
}

void DoPuyoModelConversion()
{
    SlResourceDatabase winWeaponsDatabase = fs.GetSceneDatabase("weapons/weapons");
    SlResourceDatabase wiiWeaponsDatabase =
        SlResourceDatabase.Load($"{workDirectory}/game/wiiu/weapons.cpu.swu", $"{workDirectory}/game/wiiu/weapons.gpu.swu");

    ConvertAndRegisterModel("se_entity_puffer", wiiWeaponsDatabase, winWeaponsDatabase);
    
    winWeaponsDatabase.Save(
        $@"{outputDirectory}\weapons\weapons.cpu.spc",
        $@"{outputDirectory}\weapons\weapons.gpu.spc",
        inMemory: true);   
}

void DoResourceRemap(ISumoResource resource)
{
    var remap = new Dictionary<int, string>
    {
        { 563761785, "assets/default/track_furniture/scenes/pickupstar_entity.mb:se_entity_root_pickup_star|se_animator_pickup_star|se_entity_medal.model" },
        { -385663326, "assets/default/track_furniture/scenes/pickupstar_entity.mb:se_entity_root_pickup_star|se_animator_pickup_star.skeleton" },
        { 2008611284, "assets/default/track_furniture/scenes/pickupstar_entity.mb:se_entity_root_pickup_star|se_animator_pickup_star|se_anim_stream_pickup_star|hit01.anim" },
        { 182476444, "assets/default/track_furniture/scenes/pickupstar_entity.mb:se_entity_root_pickup_star|se_animator_pickup_star|se_anim_stream_pickup_star|idle01.anim" }
    };
    
    if (remap.TryGetValue(resource.Header.Id, out string? path))
    {
        resource.Header.SetName(path);
    }
}

void ConvertAndRegisterModel(string partialName, SlResourceDatabase sourceDatabase, SlResourceDatabase targetDatabase)
{
    var model = sourceDatabase.FindResourceByPartialName<SlModel>(partialName, instance: true);
    if (model == null)
        throw new NullReferenceException("Couldn't find model with partial name " + partialName);
    
    model.Convert(targetDatabase.Platform);
    DoResourceRemap(model);
    
    // if (partialName == "se_entity_ring_gold_a")
    // {
    //     model.CullSphere = medalEntityModel.CullSphere;
    //     
    //     SlModelSegment segment = model.Resource.Segments[0];
    //     segment.Sector.Center = medalEntityModel.Resource.Segments[0].Sector.Center;
    //     segment.Sector.Extents = medalEntityModel.Resource.Segments[0].Sector.Extents;
    //     
    //     var vertices =
    //         segment.Format.Get(segment.VertexStreams, SlVertexUsage.Position, segment.VertexStart, segment.Sector.NumVerts);
    //     for (int i = 0; i < vertices.Length; ++i)
    //     {
    //         vertices[i].X += 10.0f;
    //         vertices[i].Z += (0.2775f / 2.31059f);
    //     }
    //     
    //     segment.Format.Set(segment.VertexStreams, SlVertexUsage.Position, vertices, segment.VertexStart);
    //
    //     segment.VertexStreams[0].Data = segment.VertexStreams[0]
    //         .Data[0..(segment.Sector.NumVerts * segment.VertexStreams[0].Stride)];
    //     segment.VertexStreams[0].Count = segment.Sector.NumVerts;
    //     segment.VertexStreams[1].Data = segment.VertexStreams[1]
    //         .Data[0..(segment.Sector.NumVerts * segment.VertexStreams[1].Stride)];
    //     segment.VertexStreams[1].Count = segment.Sector.NumVerts;
    //     segment.VertexStreams[2].Data = segment.VertexStreams[2]
    //         .Data[0..(segment.Sector.NumVerts * segment.VertexStreams[2].Stride)];
    //     segment.VertexStreams[2].Count = segment.Sector.NumVerts;
    //     
    //     
    //     model.Resource.CullSpheres = medalEntityModel.Resource.CullSpheres;
    //     model.Resource.CullSphereAttributeIndex = medalEntityModel.Resource.CullSphereAttributeIndex;
    //     model.Resource.Skeleton = medalEntityModel.Resource.Skeleton;
    //     model.Resource.Segments = [model.Resource.Segments[0]];
    //     model.Resource.RenderCommands = medalEntityModel.Resource.RenderCommands;
    //     model.Resource.Flags = medalEntityModel.Resource.Flags;
    // }
    
    // Register this model's skeleton in the target database
    // There's no platform specific data, so need for conversion
    SlSkeleton? skeleton = model.Resource.Skeleton.Instance;
    if (skeleton != null)
    {
        int basisSkeletonId = skeleton.Header.Id;
        DoResourceRemap(skeleton);
        model.Resource.Skeleton = new SlResPtr<SlSkeleton>(skeleton);
        //targetDatabase.AddResource(skeleton);
        
        // Find all animations that belong to this skeleton
        var nodes = sourceDatabase.GetNodesOfType<SeDefinitionAnimationStreamNode>();
        foreach (SeDefinitionAnimationStreamNode node in nodes)
        {
            SlAnim? animation = node.Animation.Instance;
            if (animation == null || animation.Skeleton.Id != basisSkeletonId) continue;
            
            Console.WriteLine(animation.Header.Name);
            DoResourceRemap(animation);
            animation.Skeleton = new SlResPtr<SlSkeleton>(skeleton);
            
            // The difference between platforms is just the endianness, so we
            // can directly add the animation to the target database.
            //targetDatabase.AddResource(animation);
        }
    }
    
    // Convert all materials to target platform
    for (int i = 0; i < model.Materials.Count; ++i)
    {
        SlMaterial2? material = model.Materials[i].Instance;
        if (material == null)
            throw new NullReferenceException("Failed to load material!");

        string shaderName = Path.GetFileNameWithoutExtension(sourceDatabase.GetResourceNameFromHash(material.Shader.Id));
        if (shaderName.StartsWith("vp"))
        {
            if (shaderName.Contains("_silo"))
            {
                bool hasCoordinates = shaderName.Contains("_vt_");
                bool hasTangents = shaderName.Contains("_vtg_");
                bool hasNormals = shaderName.Contains("_vn_");
                bool hasDiffuseTexture = shaderName.Contains("_ct_");
                bool hasNormalTexture = shaderName.Contains("_nt_");

                List<string> tsrAttributes = shaderName.Split("_").ToList();
                List<string> attributes = ["vp"];
                if (hasNormals) attributes.Add("vn");
                if (hasCoordinates) attributes.Add("vt");
                if (hasTangents) attributes.Add("vtg");
                if (hasNormals) attributes.Add("tnv");
                if (hasTangents) attributes.Add("ttv");
                
                attributes.Add(hasDiffuseTexture ? "ct" : "cm");
                if (hasNormalTexture) attributes.Add("nt");
                //attributes.Add(hasSpecularTexture ? "st" : sm);
                attributes.Add("sm");
                
                attributes.Add("cma");
                foreach (string attribute in new [] {"s", "u", "p", "b", "f", "co", "so", "go"})
                {
                    if (!tsrAttributes.Contains(attribute)) continue;
                    attributes.Add(attribute);
                }
                
                attributes.Add("god");
                attributes.Add("sod");
                
                shaderName = string.Join('_', attributes);
            } 
            else shaderName = shaderName[..shaderName.LastIndexOf('_')];
        }

        var convertedMaterial = shaderCache.FindResourceByPartialName<SlMaterial2>(shaderName, instance: true);
        if (convertedMaterial == null)
            throw new NullReferenceException($"Couldn't find conversion material for {shaderName}!");
        
        foreach (SlSampler sampler in material.Samplers)
        {
            string textureName = sampler.Header.Name;
            
            // Shadow texture is built-in, it will never be in the database.
            if (textureName is "gShadowTexture" or "gLightTexture") continue;
            if (sampler.Texture.IsEmpty) continue;
            
            int textureId = sampler.Texture.Id;
            if (!targetDatabase.ContainsResourceWithHash(textureId))
            {
                if (textureCache.ContainsResourceWithHash(textureId))
                {
                    //textureCache.CopyResourceByHash<SlTexture>(targetDatabase, textureId);   
                }
                else
                {
                    //targetDatabase.AddResource(sampler.Texture.Instance!);
                }
            }
        }
        
        // Copy all relevant constant data to the new material
        //material.PrintConstantValues();
        
        convertedMaterial.CopyDataFrom(material);
        convertedMaterial.Header.SetName(material.Header.Name);
        
        // Register all resources
        shaderCache.CopyResourceByHash<SlShader>(targetDatabase, convertedMaterial.Shader.Id);
        foreach (SlConstantBuffer buffer in convertedMaterial.ConstantBuffers)
            shaderCache.CopyResourceByHash<SlConstantBufferDesc>(targetDatabase, buffer.ConstantBufferDesc.Id);
        //targetDatabase.AddResource(convertedMaterial);
        
        model.Materials[i] = new SlResPtr<SlMaterial2>(convertedMaterial);
    }

    // if (partialName == "se_entity_ring_gold_a")
    // {
    //     model.Materials = medalEntityModel.Materials;
    //     foreach (var material in model.Materials)
    //     {
    //         sharedAssetsScene.CopyResourceByHash<SlMaterial2>(targetDatabase, material.Id);
    //         if (material.Instance == null) continue;
    //         
    //         sharedAssetsScene.CopyResourceByHash<SlShader>(targetDatabase, material.Instance.Shader.Id);
    //         foreach (SlConstantBuffer buffer in material.Instance.ConstantBuffers)
    //             sharedAssetsScene.CopyResourceByHash<SlConstantBufferDesc>(targetDatabase, buffer.ConstantBufferDesc.Id);
    //     }
    // }
    
    // Finally register the fully converted resource
    targetDatabase.AddResource(model);
}

void SetupDataCaches()
{
    fs = new MappedFileSystem($"{workDirectory}\\game\\pc");
    fs64 = new MappedFileSystem("F:/games/Team Sonic Racing/data");
    ssr = new MappedFileSystem($"{workDirectory}\\ssr\\pc\\resource");
    publishfs = new MappedFileSystem(gameDirectory);
    workfs = new MappedFileSystem(workDirectory);
    
    if (loadConversionCache)
    {
        shaderCache =
            SlResourceDatabase.Load($"{workDirectory}/cache/shadercache.cpu.spc", $"{workDirectory}/cache/shadercache.gpu.spc", inMemory: true);
        textureCache =
            SlResourceDatabase.Load($"{workDirectory}/cache/texturecache.cpu.spc", $"{workDirectory}/cache/texturecache.gpu.spc", inMemory: true);   
    }
    else
    {
        shaderCache = null!;
        textureCache = null!;
    }
}

struct AkBankSubHeader
{
    public int Tag;
    public int Size;
};