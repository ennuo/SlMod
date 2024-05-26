﻿using System.IO.Compression;
using System.Numerics;
using System.Runtime.Serialization;
using System.Text.Json;
using System.Text.Json.Serialization;
using DirectXTexNet;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.PixelFormats;
using SlLib.Extensions;
using SlLib.Filesystem;
using SlLib.IO;
using SlLib.Resources;
using SlLib.Resources.Database;
using SlLib.Resources.Model;
using SlLib.Resources.Model.Commands;
using SlLib.Resources.Scene.Definitions;
using SlLib.Resources.Skeleton;
using SlLib.Serialization;
using SlLib.SumoTool;
using SlLib.SumoTool.Siff;
using SlLib.Utilities;
using SlLib.Workspace;


const string gameDirectory =
    @"C:\Program Files (x86)\Steam\steamapps\common\Sonic & All-Stars Racing Transformed\Data\";
const string outputDirectory = @"F:\sart\build";
const string workDirectory = @"F:\sart\";

const bool loadConversionCache = true;
const bool doRingReplacement = true;
const bool doPuyoModelConversion = true;
const bool doRacerReplacements = false;
const bool doObjectDefTests = false;

SlResourceDatabase shaderCache, textureCache;
IFileSystem fs, fs64;
IFileSystem gameFs = new MappedFileSystem(gameDirectory);
SetupDataCaches();

var sharedAssetsScene = SlResourceDatabase.Load("C:/Users/Aidan/Desktop/allstar-coin.cpu.spc",
    "C:/Users/Aidan/Desktop/allstar-coin.gpu.spc");
var medalEntityModel = sharedAssetsScene.FindResourceByPartialName<SlModel>("se_animator_pickup_star|se_entity_medal.model")!;

if (doObjectDefTests)
{
    // We need to get the GUI modifications working...
    SumoToolPackage package = fs.GetSumoToolPackage("ui/frontend/tutorials/versus_beat_player");
    SiffFile siff = package.GetLocaleSiff();
    var def = siff.LoadResource<ObjectDefLibrary>(SiffResourceType.ObjectDefLibrary);
}

if (doRingReplacement)
{
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

if (doRacerReplacements)
{
     var manager = new RacerDataManager(fs, outputDirectory);
    manager.RegisterRacer("danicapatrick", new RacerDataManager.RacerImportSetting
    {
        GlbSourcePath = $"{workDirectory}/import/miku/mikitm_sart_test2.glb",
        GlbBoneRemapCallback = SkeletonUtil.MapSekaiSkeleton,
        DisplayName = "Hatsune Miku",
        RaceResultsPortrait = $"{workDirectory}/import/miku/mikuentry.png",
        VersusPortrait = $"{workDirectory}/import/miku/mikuportrait.png",
        CharSelectIcon = $"{workDirectory}/import/miku/mikuicon.png",
        MiniMapIcon = $"{workDirectory}/import/miku/mikuminimap_icon.png",
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
    });

    manager.RegisterRacer("dragon", new RacerDataManager.RacerImportSetting
    {
        GlbSourcePath = $"{workDirectory}/import/kiryu/kiryu.glb",
        GlbBoneRemapCallback = SkeletonUtil.MapKiryuSkeleton,
        DisplayName = "Kiryu",
        RaceResultsPortrait = $"{workDirectory}/import/kiryu/kiryu_raceresults.png",
        VersusPortrait = $"{workDirectory}/import/kiryu/kiryu_racericon_big.png",
        CharSelectIcon = $"{workDirectory}/import/kiryu/kiryu_racericon_small.png",
        MiniMapIcon = $"{workDirectory}/import/kiryu/kiryu_mapicon.png",
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
        DisplayName = "Sackboy"
    });

    manager.Flush();   
}

// Convert Puyo to Win32
if (doPuyoModelConversion)
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

return;

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
    
    if (partialName == "se_entity_ring_gold_a")
    {
        model.CullSphere = medalEntityModel.CullSphere;
        
        SlModelSegment segment = model.Resource.Segments[0];
        segment.Sector.V0 = medalEntityModel.Resource.Segments[0].Sector.V0;
        segment.Sector.V1 = medalEntityModel.Resource.Segments[0].Sector.V1;
        
        var vertices =
            segment.Format.Get(segment.VertexStreams, SlVertexUsage.Position, segment.VertexStart, segment.Sector.NumVerts);
        for (int i = 0; i < vertices.Length; ++i)
        {
            vertices[i].X += 10.0f;
            vertices[i].Z += (0.2775f / 2.31059f);
        }
        
        segment.Format.Set(segment.VertexStreams, SlVertexUsage.Position, vertices, segment.VertexStart);

        segment.VertexStreams[0].Data = segment.VertexStreams[0]
            .Data[0..(segment.Sector.NumVerts * segment.VertexStreams[0].Stride)];
        segment.VertexStreams[0].Count = segment.Sector.NumVerts;
        segment.VertexStreams[1].Data = segment.VertexStreams[1]
            .Data[0..(segment.Sector.NumVerts * segment.VertexStreams[1].Stride)];
        segment.VertexStreams[1].Count = segment.Sector.NumVerts;
        segment.VertexStreams[2].Data = segment.VertexStreams[2]
            .Data[0..(segment.Sector.NumVerts * segment.VertexStreams[2].Stride)];
        segment.VertexStreams[2].Count = segment.Sector.NumVerts;
        
        
        model.Resource.CullSpheres = medalEntityModel.Resource.CullSpheres;
        model.Resource.CullSphereAttributeIndex = medalEntityModel.Resource.CullSphereAttributeIndex;
        model.Resource.Skeleton = medalEntityModel.Resource.Skeleton;
        model.Resource.Segments = [model.Resource.Segments[0]];
        model.Resource.RenderCommands = medalEntityModel.Resource.RenderCommands;
        model.Resource.Flags = medalEntityModel.Resource.Flags;
    }
    
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

    if (partialName == "se_entity_ring_gold_a")
    {
        model.Materials = medalEntityModel.Materials;
        foreach (var material in model.Materials)
        {
            sharedAssetsScene.CopyResourceByHash<SlMaterial2>(targetDatabase, material.Id);
            if (material.Instance == null) continue;
            
            sharedAssetsScene.CopyResourceByHash<SlShader>(targetDatabase, material.Instance.Shader.Id);
            foreach (SlConstantBuffer buffer in material.Instance.ConstantBuffers)
                sharedAssetsScene.CopyResourceByHash<SlConstantBufferDesc>(targetDatabase, buffer.ConstantBufferDesc.Id);
        }
    }
    
    // Finally register the fully converted resource
    targetDatabase.AddResource(model);
}

void SetupDataCaches()
{
    fs = new MappedFileSystem($"{workDirectory}\\game\\pc");
    fs64 = new MappedFileSystem("F:/games/Team Sonic Racing/data");

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