using System.Numerics;
using Collada141;
using SlLib.Resources;
using SlLib.Resources.Database;
using SlLib.Resources.Scene;
using SlLib.Resources.Scene.Definitions;
using SlLib.Resources.Scene.Instances;
using SlLib.SumoTool;
using SlLib.SumoTool.Siff;
using SlLib.SumoTool.Siff.NavData;
using SlLib.Utilities;
using TurboLibrary;
using Path = System.IO.Path;

namespace SlLib.MarioKart;

public class TrackImporter
{
    /// <summary>
    ///     The relative path to the course directory.
    /// </summary>
    private string _path;
    
    /// <summary>
    ///     The internal name of the course.
    /// </summary>
    private string _name;
    
    /// <summary>
    ///     The transformed track ID
    /// </summary>
    private string _source;

    /// <summary>
    ///     Track to write to.
    /// </summary>
    private string _target;
    
    /// <summary>
    ///     Database to copy all converted assets to.
    /// </summary>
    private SlResourceDatabase _database;
    
    /// <summary>
    ///     Mario Kart course definition.
    /// </summary>
    private CourseDefinition _course;
    
    /// <summary>
    ///     Converted navigation for Transformed.
    /// </summary>
    private Navigation _navigation;
    
    private SeInstanceFolderNode _trackFolder;
    private SeInstanceFolderNode _lightingFolder;
    
    public TrackImporter(TrackImportConfig config)
    {
        _path = $"course/{config.CourseId}";
        _target = config.TrackTarget;
        _name = config.CourseId;
        _source = config.TrackSource;
        _database = new SlResourceDatabase(SlPlatform.Win32);
        _course = new CourseDefinition($"{KartConstants.MarioRoot}/{_path}/course_muunt.byaml");
        _navigation = new Navigation
        {
            NameHash = 1088279993,
            // NameHash = SlUtil.SumoHash(config.TrackSource),
            Version = 9
        };
        
        SetupSceneLayout();
    }
    
    public void Import()
    {
        SetupTrackModel();
        SetupTrackLighting();
        SetupTrackCollision();
        ImportNavigation();
        
        // Make sure to flush any nodes we've created
        _database.FlushSceneGraph();
        
        // Flush all the data to the target course directory
        string build = $@"{KartConstants.PublishRoot}\levels\{_target}\{_target}";
        var siff = new SiffFile(SlPlatform.Win32.GetDefaultContext());
        siff.SetResource(_navigation, SiffResourceType.Navigation);
        siff.BuildKSiff(out byte[] cpuData, out _, compressed: false);
        File.WriteAllBytes(@$"{build}.navpc", cpuData);
        // Copy generated database to target track
        _database.Save($"{build}.cpu.spc", $"{build}.gpu.spc");
    }
    
    private void SetupSceneLayout()
    {
        // This folder contains data for the track area collision and model
        _trackFolder = SeInstanceNode.CreateObject<SeInstanceFolderNode>(SeDefinitionFolderNode.Default);
        _trackFolder.UidName = _name;
        _trackFolder.Parent = _database.Scene;
    }
    
    private void SetupTrackLighting()
    {
        string sourceDatabasePath = Path.Join(KartConstants.GameRoot, $"levels/{_source}/{_source}");
        SlResourceDatabase database =
            SlResourceDatabase.Load($"{sourceDatabasePath}.cpu.spc", $"{sourceDatabasePath}.gpu.spc");
        
        // Find the lighting project folder, in every standard level, all data we want should be under this folder.
        SeGraphNode? lighting = database.Scene.FindFirstDescendentByPartialName("lighting");
        if (lighting == null) lighting = database.Scene.FindFirstDescendentByPartialName("Lighting");
        if (lighting == null) throw new FileNotFoundException($"Unable to find source lighting folder in {_source}");
        
        // Because this database is temporary, we can just steal the pointer
        lighting.Parent = _database.Scene;
        lighting.UidName = "Lighting"; // Just keep it upper-case because I want it to be
        
        // If there's any entities attached to the lighting scene, make sure we're copying the resources to our database
        var nodes = lighting.FindDescendantsThatDeriveFrom<SeInstanceEntityNode>();
        foreach (SeInstanceEntityNode node in nodes)
        {
            if (node.Definition is not SeDefinitionEntityNode entity) continue;
            SlModel? model = entity.Model;
            if (model != null)
                database.CopyResourceByHash<SlModel>(_database, model.Header.Id, dependencies: true);
        }
    }

    private void SetupTrackCollision()
    {
        // string cachedCollisionData = Path.Join(KartConstants.ResourceCache, $"{_name}.collision.bin");
        // if (File.Exists(cachedCollisionData))
        // {
        //     
        // }
        
        var importer = new CollisionImporter($"{_path}/course_kcl.szs", _name);
        SlResourceCollision collision = importer.Import();
        _database.AddResource(collision);
        
        (SeDefinitionCollisionNode def, SeInstanceCollisionNode inst) =
            SeNodeBase.Create<SeDefinitionCollisionNode, SeInstanceCollisionNode>();
        
        inst.Parent = _trackFolder;
        def.UidName = collision.Header.Name;
        inst.UidName = $"se_collision_{_name}";
    }
    
    private void SetupTrackModel()
    {
        string cachedCpuDataPath = Path.Join(KartConstants.ResourceCache, $"{_name}.track.cpu.spc");
        string cachedGpuDataPath = Path.Join(KartConstants.ResourceCache, $"{_name}.track.gpu.spc");

        if (File.Exists(cachedCpuDataPath))
        {
            SlResourceDatabase database = SlResourceDatabase.Load(cachedCpuDataPath, cachedGpuDataPath);
            database.CopyTo(_database);
        }
        else
        {
            var importer = new BfresImporter();
            importer.Register($"{_path}/course_model.szs");
            importer.Database.CopyTo(_database);
        
            // Importing the track itself takes a while, so we're just going
            // to cache it for successive runs.
            importer.Database.Save(cachedCpuDataPath, cachedGpuDataPath);   
        }

        var definition = _database.FindNodeByPartialName<SeDefinitionEntityNode>($"{_path}/course_model.szs");
        if (definition == null)
            throw new FileNotFoundException($"Unable to find course model definition in the cached database!");
        
        // Setting the parent will automatically attach it to the scenegraph
        var instance = new SeInstanceAreaNode
        {
            Definition = definition,
            Parent = _trackFolder,
            UidName = $"se_area_{_name}",
            Tag = "Track"
        };
    }

    private void ImportNavigation()
    {
        var line = new NavRacingLine
        {
            Looping = true
        };

        var spatial = new NavSpatialGroup();

        _navigation.RacingLines.Add(line);
        _navigation.SpatialGroups.Add(spatial);

        var path = _course.LapPaths[0].Points;
        List<NavWaypointLink> links = [];
        for (int i = 0; i < path.Count; ++i)
        {
            LapPathPoint point = path[i];
            
            // material properties
                // float 0xbc
                // float  0xc0
            // collision flags = 0xc4
            // collision type = 200 / 0xc8
            
            
            var waypoint = new NavWaypoint
            {
                Name = $"waypoint_0_{i}",
                Permissions = line.Permissions,
                Pos = new Vector3(point.Translate.X, point.Translate.Y, point.Translate.Z) * 0.1f,
            };

            var byamlScale = point.Scale ?? throw new Exception("where the fuck is the scale?");
            var extents = new Vector3(byamlScale.X, 0.0f, 0.0f) * 0.1f / 2.0f;
            
            Matrix4x4 rotation =
                Matrix4x4.CreateRotationZ(point.Rotate.Z) *
                Matrix4x4.CreateRotationY(point.Rotate.Y) *
                Matrix4x4.CreateRotationX(point.Rotate.X);
            var translation =
                Matrix4x4.CreateTranslation(new Vector3(point.Translate.X, point.Translate.Y, point.Translate.Z) * 0.1f);


            var matrix = rotation * translation;
            Vector3 up = Vector3.Normalize(Vector3.TransformNormal(Vector3.UnitY, matrix));
            Vector3 dir = Vector3.Normalize(Vector3.TransformNormal(Vector3.UnitZ, matrix));

            Vector3 right = waypoint.Pos + Vector3.Transform(extents, rotation);
            Vector3 left = waypoint.Pos - Vector3.Transform(extents, rotation);
            
            waypoint.Up = up;
            waypoint.Dir = dir;
            
            var link = new NavWaypointLink
            {
                Right = right,
                Left = left,
                RacingLineLimitLeft = left,
                RacingLineLimitRight = right,
                From = waypoint,
                Width = extents.X,
                RacingLines = [new NavRacingLineRef { RacingLine = line, SegmentIndex = i }],
                SpatialGroup = spatial,
            };

            waypoint.FromLinks = [link];
            
            var segment = new NavRacingLineSeg
            {
                Link = link,
                RacingLine = waypoint.Pos,
                SafeRacingLine = waypoint.Pos
            };
            
            _navigation.Waypoints.Add(waypoint);
            line.Segments.Add(segment);
            spatial.Links.Add(link);
            links.Add(link);
        }


        float distance = 0.0f;
        for (int i = 0; i < links.Count; ++i)
        {
            NavWaypointLink link = links[i];
            NavWaypointLink nextLink = links[(i + 1) % links.Count];
            
            link.To = links[(i + 1) % links.Count].From;
            link.From!.ToLinks = [nextLink];

            link.FromToNormal = Vector3.Normalize((link.From!.Dir + link.To!.Dir) / 2.0f);
            link.Plane.Normal = link.From!.Dir;
            link.Plane.Const = Vector3.Dot(link.From!.Pos, link.Plane.Normal);
            
            nextLink.FromToNormal = Vector3.Normalize(nextLink.From!.Pos - link.From!.Pos);
            
            float delta = Vector3.Distance(link.From!.Pos, link.To!.Pos);
            line.Segments[i].RacingLineLength = delta;
            link.Length = delta;
            link.From!.TrackDist = distance;
            distance += delta;
            
        }

        line.TotalLength = distance;

        var startWaypoint = _navigation.Waypoints.First();
        foreach (var waypoint in _navigation.Waypoints)
            waypoint.Pos.Y += 0.5f;

        var startMarker = new NavTrackMarker
        {
            Dir = startWaypoint.Dir,
            Flags = 16,
            JumpSpeedPercentage = 0.65f,
            Pos = startWaypoint.Pos,
            Radius = 15.0f,
            Text = string.Empty,
            Type = 2,
            Up = startWaypoint.Up,
            Waypoint = startWaypoint
        };

        _navigation.TrackMarkers.Add(startMarker);
        _navigation.NavStarts.Add(new NavStart
        {
            DriveMode = 2,
            TrackMarker = startMarker
        });

        _navigation.TotalTrackDist = distance;
        
        // TODO: Properly calculate this
        _navigation.LowestPoint = -500.0f;
        _navigation.HighestPoint = 500.0f;
    }
}