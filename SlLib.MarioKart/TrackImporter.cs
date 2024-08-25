using System.Numerics;
using AampLibraryCSharp;
using AGraphicsLibrary;
using CafeLibrary;
using SlLib.Enums;
using SlLib.Extensions;
using SlLib.Resources;
using SlLib.Resources.Database;
using SlLib.Resources.Scene;
using SlLib.Resources.Scene.Definitions;
using SlLib.Resources.Scene.Instances;
using SlLib.Resources.Skeleton;
using SlLib.SumoTool;
using SlLib.SumoTool.Siff;
using SlLib.SumoTool.Siff.NavData;
using SlLib.Utilities;
using TurboLibrary;
using Path = System.IO.Path;
using Vector3 = System.Numerics.Vector3;
using Vector4 = System.Numerics.Vector4;

namespace SlLib.MarioKart;

public class TrackImporter
{
    private static string SharedAssetsPath = $@"{KartConstants.GameRoot}\gamemodes\gamemodeassets\shared_assets\shared_assets";
    private static SlResourceDatabase SharedAssets = SlResourceDatabase.Load($"{SharedAssetsPath}.cpu.spc", $"{SharedAssetsPath}.gpu.spc", inMemory: true);
    
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
    private SeInstanceFolderNode _furnitureFolder;
    private SeInstanceFolderNode _aiFolder;
    private SeInstanceFolderNode _cameraFolder;
    
    public TrackImporter(TrackImportConfig config)
    {
        _path = $"course/{config.CourseId.ToLower()}";
        _target = config.TrackTarget;
        _name = config.CourseId.ToLower();
        _source = config.TrackSource;
        _database = new SlResourceDatabase(SlPlatform.Win32);
        _course = new CourseDefinition($"{KartConstants.MarioRoot}/{_path}/course_muunt.byaml");
        _navigation = new Navigation
        {
            NameHash = 1088279993,
            // NameHash = SlUtil.SumoHash(config.TrackSource),
            Version = 9
        };
        
        SetupSceneLayout(config.CourseId);
    }
    
    public void Import()
    {
        Console.WriteLine($"[TrackImporter] Importing {_name}...");
        SetupTrackModel();
        SetupTrackLighting();
        SetupTrackCollision();
        ImportMapObjects();
        ImportWeaponPods();
        ImportScenicCameras();
        ImportNavigation();
        
        // Make sure to flush any nodes we've created
        Console.WriteLine("[TrackImporter] Flushing scenegraph...");
        _database.Scene.RecomputePositions();
        _database.FlushSceneGraph();
        
        // Flush all the data to the target course directory
        Console.WriteLine("[TrackImporter] Packing navigation data...");
        string build = $@"{KartConstants.PublishRoot}\levels\{_target}\{_target}";
        var siff = new SiffFile(SlPlatform.Win32.GetDefaultContext());
        siff.SetResource(_navigation, SiffResourceType.Navigation);
        siff.BuildKSiff(out byte[] cpuData, out _, compressed: false);
        File.WriteAllBytes(@$"{build}.navpc", cpuData);
        // Copy generated database to target track
        Console.WriteLine("[TrackImporter] Flushing track database...");
        _database.Save($"{build}.cpu.spc", $"{build}.gpu.spc");
    }
    
    private void SetupSceneLayout(string trackFolderName)
    {
        // This folder contains data for the track area collision and model
        _trackFolder = SeInstanceNode.CreateObject<SeInstanceFolderNode>(SeDefinitionFolderNode.Default);
        _trackFolder.UidName = trackFolderName;
        _trackFolder.Parent = _database.Scene;

        _aiFolder = SeInstanceNode.CreateObject<SeInstanceFolderNode>(SeDefinitionFolderNode.Default);
        _aiFolder.UidName = "TrackAI";
        _aiFolder.Parent = _database.Scene;

        _furnitureFolder = SeInstanceNode.CreateObject<SeInstanceFolderNode>(SeDefinitionFolderNode.Default);
        _furnitureFolder.UidName = "TrackFurniture";
        _furnitureFolder.Parent = _database.Scene;
        
        _cameraFolder = SeInstanceNode.CreateObject<SeInstanceFolderNode>(SeDefinitionFolderNode.Default);
        _cameraFolder.UidName = "Cameras";
        _cameraFolder.Parent = _database.Scene;
    }
    
    private void SetupTrackLighting()
    {
        Console.WriteLine($"[TrackImporter] Stealing lighting data from {_source}...");
        
        string sourceDatabasePath = Path.Join(KartConstants.GameRoot, $"levels/{_source}/{_source}");
        SlResourceDatabase database =
            SlResourceDatabase.Load($"{sourceDatabasePath}.cpu.spc", $"{sourceDatabasePath}.gpu.spc", inMemory: true);
        
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
        
        // this just has sh data?
        // byte[] data = szs.Decode(File.ReadAllBytes($"{KartConstants.MarioRoot}/{_path}/course_bglpbd.szs"));
        // var env = new EnvironmentGraphics(AampFile.LoadFile(new MemoryStream(data)));


        byte[] data = File.ReadAllBytes($"{KartConstants.MarioRoot}/{_path}/course.bgenv");
        SarcData sarc = SARC_Parser.UnpackRamN(new MemoryStream(data));
        
        foreach (var file in sarc.Files)
        {
            if (file.Key != "course_area.baglenv") continue;
            
            var env = new EnvironmentGraphics(AampFile.LoadFile(new MemoryStream(file.Value)));
            SeInstanceLightNode? sun = _database
                .FindNodesThatDeriveFrom<SeInstanceLightNode>()
                .First(light => light.LightType == SeInstanceLightNode.SeLightType.Directional);
            DirectionalLight? directional = env.DirectionalLights.Find(light => light.Name == "MainLight0");
            
            if (sun == null || directional == null) throw new Exception("Couldn't find sun in lighting data!");

            sun.Color = new Vector3(directional.DiffuseColor.R, directional.DiffuseColor.G, directional.DiffuseColor.B);
            sun.IntensityMultiplier = directional.Intensity;
            sun.Rotation = MathUtils.LookRotation(
                new Vector3(directional.Direction.X, directional.Direction.Y, directional.Direction.Z), Vector3.UnitY);
        }
    }

    private void SetupTrackCollision()
    {
        Console.WriteLine("[TrackImporter] Importing track collision data, this may take a while...");

        SlResourceCollision collision;
        string cachedCollisionData = Path.Join(KartConstants.ResourceCache, $"{_name}.collision.bin");
        // if (File.Exists(cachedCollisionData))
        // {
        //     var context = new ResourceLoadContext(File.ReadAllBytes(cachedCollisionData));
        //     collision = context.LoadReference<SlResourceCollision>();
        //     _database.AddResource(collision);
        // }
        // else
        {
            var importer = new CollisionImporter($"{_path}/course_kcl.szs", _name);
            collision = importer.Import();
            
            _database.AddResource(collision);
            // Avoid reserializing it to cache it
            if (_database.GetRawResourceByPartialName<SlResourceCollision>(collision.Header.Name, out byte[]? cpuData, out _))
                File.WriteAllBytes(cachedCollisionData, cpuData);
        }
        
        (SeDefinitionCollisionNode def, SeInstanceCollisionNode inst) =
            SeNodeBase.Create<SeDefinitionCollisionNode, SeInstanceCollisionNode>();
        
        inst.Parent = _trackFolder;
        def.UidName = collision.Header.Name;
        inst.UidName = $"se_collision_{_name}";
        
        Console.WriteLine("[TrackImporter] Finished importing track collision data!");
    }
    
    private void SetupTrackModel()
    {
        Console.WriteLine($"[TrackImporter] Initializing track model, checking if cached entry exists...");
        string cachedCpuDataPath = Path.Join(KartConstants.ResourceCache, $"{_name}.track.cpu.spc");
        string cachedGpuDataPath = Path.Join(KartConstants.ResourceCache, $"{_name}.track.gpu.spc");

        if (File.Exists(cachedCpuDataPath))
        {
            Console.WriteLine("[TrackImporter] Cached track exists! Loading model data from cache!");
            SlResourceDatabase database = SlResourceDatabase.Load(cachedCpuDataPath, cachedGpuDataPath);
            database.CopyTo(_database);
        }
        else
        {
            Console.WriteLine("[TrackImporter] No track cache exists, importing track bfres model...");
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
        
        // Just use the bounding box of the model for determining the bottom/top of the navigation
        SlModel? model = definition.Model;
        if (model == null)
        {
            _navigation.HighestPoint = 100.0f;
            _navigation.LowestPoint = -100.0f;
        }
        else
        {
            _navigation.LowestPoint = model.CullSphere.BoxCenter.Y - model.CullSphere.Extents.Y; 
            _navigation.HighestPoint = model.CullSphere.BoxCenter.Y + model.CullSphere.Extents.Y;
        }
        
        Console.WriteLine("[TrackImporter] Finished importing track model!");
    }

    private void ImportMapObjects()
    {
        var folder = SeInstanceNode.CreateObject<SeInstanceFolderNode>(SeDefinitionFolderNode.Default);
        folder.UidName = "Environment";
        folder.Parent = _database.Scene;

        foreach (string id in _course.MapObjResList)
        {
            string lower = id.ToLower();
            string local = $"mapobj/{lower}/{lower}";

            string bfres = $"{local}.bfres";
            string kcl = $"{local}.kcl";
            
            if (!File.Exists($"{KartConstants.MarioRoot}/{bfres}")) continue;
            
            var importer = new BfresImporter();
            importer.Register(bfres);
            importer.Database.CopyTo(_database);
            
            if (File.Exists($"{KartConstants.MarioRoot}/{kcl}"))
            {
                var entity = _database.FindNodeByPartialName<SeDefinitionEntityNode>(bfres);
                if (entity != null)
                {
                    SlResourceCollision collision = new CollisionImporter(kcl, lower).Import();
                    var def = new SeDefinitionCollisionNode
                    {
                        UidName = collision.Header.Name,
                        Parent = entity
                    };
                    _database.AddResource(collision);
                }
            }
        }

        foreach (Obj obj in _course.Objs)
        {
            if (!KartConstants.ObjectList.TryGetValue(obj.ObjId, out string? name))
                continue;
            if (name is "ItemBox" or "Coin" or "Choropoo" or "Sun") continue;
            if (name.StartsWith("VR")) continue;
            
            var entity = _database.FindNodeByPartialName<SeDefinitionEntityNode>($"mapobj/{name.ToLower()}/{name.ToLower()}.bfres");
            if (entity?.Instance(folder) is not SeInstanceEntityNode instance) continue;
            
            Matrix4x4 rotation =
                Matrix4x4.CreateRotationZ(obj.Rotate.Z) *
                Matrix4x4.CreateRotationY(obj.Rotate.Y) *
                Matrix4x4.CreateRotationX(obj.Rotate.X);
            var translation =
                Matrix4x4.CreateTranslation(new Vector3(obj.Translate.X, obj.Translate.Y, obj.Translate.Z) * KartConstants.GameScale);
            var scale = Matrix4x4.CreateScale(new Vector3(obj.Scale.X, obj.Scale.Y, obj.Scale.Z));
                
            Matrix4x4 matrix = scale * rotation * translation;
            instance.WorldMatrix = matrix;
            Matrix4x4.Decompose(matrix, out instance.Scale, out instance.Rotation, out instance.Translation);
        }
    }

    private void ImportWeaponPods()
    {
        var folder = SeInstanceNode.CreateObject<SeInstanceFolderNode>(SeDefinitionFolderNode.Default);
        folder.UidName = "Pickups";
        folder.Parent = _database.Scene;
        
        SeDefinitionEntityNode root =
            SharedAssets.FindNodeByPartialName<SeDefinitionEntityNode>("se_entity_root_pickup_red") ??
            throw new FileNotFoundException("Could not find pickup in database!");

        var weaponDef = new WeaponPodDefinitionNode
        {
            UidName = "ItemBox"
        };
        
        foreach (Obj obj in _course.Objs)
        {
            if (!KartConstants.ObjectList.TryGetValue(obj.ObjId, out string? name)) continue;
            if (name != "ItemBox") continue;
            
            var weaponInst = SeInstanceNode.CreateObject<WeaponPodInstanceNode>(weaponDef);
            weaponInst.Parent = folder;
            if (root.Instance(weaponInst) is not SeInstanceEntityNode instance) continue;
            
            Matrix4x4 rotation =
                Matrix4x4.CreateRotationZ(obj.Rotate.Z) *
                Matrix4x4.CreateRotationY(obj.Rotate.Y) *
                Matrix4x4.CreateRotationX(obj.Rotate.X);
            var translation =
                Matrix4x4.CreateTranslation(new Vector3(obj.Translate.X, obj.Translate.Y, obj.Translate.Z) * KartConstants.GameScale);
                
            var scale = Matrix4x4.CreateScale(new Vector3(obj.Scale.X, obj.Scale.Y, obj.Scale.Z) * 2.20264317181f);
                
            Matrix4x4 matrix = scale * rotation * translation;
            weaponInst.WorldMatrix = matrix;
            Matrix4x4.Decompose(matrix, out weaponInst.Scale, out weaponInst.Rotation, out weaponInst.Translation);
        }
        
        if (folder.FirstChild != null)
        {
            SharedAssets.CopyTo(_database);
            //SharedAssets.CopyResources(_database, root);
        }
    }

    private void ImportScenicCameras()
    {
        if (_course.IntroCameras.Count == 0) return;
        
        var folder = SeInstanceNode.CreateObject<SeInstanceFolderNode>(SeDefinitionFolderNode.Default);
        folder.UidName = "TrackIntroCameras";
        folder.Parent = _cameraFolder;

        (var entityDef, var entityInst) = 
            SeNodeBase.Create<SeDefinitionEntityNode, SeInstanceEntityNode>("se_entity_trackintro_camera.model");
        (var animatorDef, var animatorInst) =
            SeNodeBase.Create<SeDefinitionAnimatorNode, SeInstanceAnimatorNode>("se_animator_trackintrocameras.skeleton");
        (var animDef, var animInst) =
            SeNodeBase.Create<SeDefinitionAnimatorNode, SeInstanceAnimatorNode>("trackintrocameras.anim");
        
        entityInst.Parent = folder;
        animatorDef.Parent = entityDef;
        animatorInst.Parent = entityInst;
        animInst.Parent = animatorInst;
        animDef.Parent = animatorDef;
        
        var animator = new SlSkeleton();
        animator.Header.SetName(animatorDef.UidName);
        var animation = new SlAnim();
        animation.Header.SetName(animDef.UidName);
        animation.FrameRate = 30.0f;
        animation.FrameCount = 2;
        animation.BoneCount = (short)_course.IntroCameras.Count;
        animation.PositionType = 0;
        animation.RotationType = 0;
        animation.ScaleType = 0;
        animation.BlendBranches =
        [
            new SlAnim.SlAnimBlendBranch(),
            new SlAnim.SlAnimBlendBranch()
            {
                FrameOffset = 1,
                Leaf = new SlAnim.SlAnimBlendLeaf
                {
                    FrameOffset = 1
                }
            }
        ];
        animation.PositionAnimData = new byte[_course.IntroCameras.Count * 0xc];
        animation.RotationAnimData = new byte[_course.IntroCameras.Count * 0x10];
        
        for (int i = 0; i < _course.IntroCameras.Count; ++i)
        {
            IntroCamera camera = _course.IntroCameras[i];
            string name = $"SE_CAMERA_ScenicCamera0{i + 1}";
            (var cameraDef, var cameraInst) =
                SeNodeBase.Create<SeDefinitionCameraNode, SeInstanceCameraNode>(name.ToLower());

            PathPoint cameraPoint = camera.Path.Points.First();
            PathPoint lookAtPoint = camera.LookAtPath.Points.First();
            
            // Not animating right now, we'll just pull the first point
            var cameraPos = new Vector3(cameraPoint.Translate.X, cameraPoint.Translate.Y, cameraPoint.Translate.Z) * KartConstants.GameScale;
            var lookAtPos = new Vector3(lookAtPoint.Translate.X, lookAtPoint.Translate.Y, lookAtPoint.Translate.Z) * KartConstants.GameScale;
            var cameraRotation = MathUtils.LookRotation(Vector3.Normalize(lookAtPos - cameraPos), Vector3.UnitY);
            
            cameraInst.VerticalFov = camera.Fovy;
            cameraInst.Translation = cameraPos;
            cameraInst.Rotation = cameraRotation;
            
            cameraDef.Translation = cameraPos;
            cameraDef.Rotation = cameraRotation;
            cameraDef.VerticalFov = camera.Fovy;
            
            cameraInst.Projection = Matrix4x4.CreatePerspectiveFieldOfView(cameraInst.VerticalFov * ((float)(Math.PI * 2) / 360), cameraInst.AspectRatio, cameraInst.NearPlane, cameraInst.FarPlane);
            cameraInst.View = Matrix4x4.CreateLookAt(cameraPos, lookAtPos, Vector3.UnitY);
            cameraInst.ViewProjection = cameraInst.View * cameraInst.Projection;
            
            Matrix4x4 matrix = Matrix4x4.CreateFromQuaternion(cameraInst.Rotation) *
                               Matrix4x4.CreateTranslation(cameraInst.Translation);
            Matrix4x4.Invert(matrix, out Matrix4x4 invMatrix);
            
            animator.Joints.Add(new SlJoint
            {
                Name = name,
                Translation = cameraInst.Translation,
                Rotation = cameraInst.Rotation,
                Scale = Vector3.One,
                Parent = -1,
                BindPose = matrix,
                InverseBindPose = invMatrix
            });
            
            cameraDef.Parent = animatorDef;
            cameraInst.Parent = animatorInst;

            animation.ConstantRotationJoints.Add((short)i);
            animation.ConstantPositionJoints.Add((short)i);
            animation.ConstantRotationFrameCommands.Add(128);
            animation.ConstantPositionFrameCommands.Add(96);
            
            animation.PositionAnimData.WriteFloat3(cameraPos, i * 0xc);
            animation.RotationAnimData.WriteFloat4(new Vector4(cameraRotation.X, cameraRotation.Y, cameraRotation.Z, cameraRotation.W), i * 0x10);
        }

        _database.AddResource(animation);
        _database.AddResource(animator);
    }

    private void ImportNavigation()
    {
        Console.WriteLine("[TrackImporter] Converting navigation data...");
        
        var line = new NavRacingLine
        {
            Looping = true,
            Permissions =  0x17
        };
        
        // 0f = plane, 27 = boat, 17 = car

        int spatialGroupSplit = 2;
        
        var spatial = new NavSpatialGroup();

        _navigation.RacingLines.Add(line);
        _navigation.SpatialGroups.Add(spatial);

        List<NavWaypointLink> links = [];
        foreach (var lapPath in _course.LapPaths)
        { 
            var path = lapPath.Points;
            for (int i = 0; i < path.Count; ++i)
            {
                LapPathPoint point = path[i];
                var waypoint = new NavWaypoint
                {
                    Name = $"waypoint_0_{i}",
                    Permissions = line.Permissions,
                    Pos = new Vector3(point.Translate.X, point.Translate.Y, point.Translate.Z) * KartConstants.GameScale,
                };
                
                waypoint.Flags |= (int)SurfaceFlags.Sticky;

                var byamlScale = point.Scale ?? throw new Exception("where the fuck is the scale?");
                var extents = new Vector3(byamlScale.X, 0.0f, 0.0f) * KartConstants.GameScale / 2.0f;
                var halfExtents = extents / 2.0f;
                
                Quaternion quat = MathUtils.ToQuaternion(new Vector3(point.Rotate.X, point.Rotate.Y, point.Rotate.Z));
                Vector3 up = Vector3.Transform(Vector3.UnitY, quat);
                Vector3 dir = Vector3.Transform(Vector3.UnitZ, quat);
                
                Vector3 right = waypoint.Pos - Vector3.Transform(extents, quat);
                Vector3 left = waypoint.Pos + Vector3.Transform(extents, quat);
                
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
                    SpatialGroup = spatial
                };
                
                link.CrossSection.AddRange([
                    left,
                    waypoint.Pos + Vector3.Transform(halfExtents, quat),
                    waypoint.Pos,
                    waypoint.Pos - Vector3.Transform(halfExtents, quat),
                    right,
                ]);
                
                waypoint.FromLinks = [link];
                
                var segment = new NavRacingLineSeg
                {
                    Link = link,
                    RacingLine = waypoint.Pos,
                    SafeRacingLine = waypoint.Pos
                };
                
                // EnemyPathPoint? aiPathPoint = FindClosestEnemyPathPoint(point);
                EnemyPathPoint? aiPathPoint = null;
                if (aiPathPoint != null)
                {
                    Console.WriteLine("updating with enemy path...")
                        ;
                    Vector3 aiPathCenter =
                        new Vector3(aiPathPoint.Translate.X, aiPathPoint.Translate.Y, aiPathPoint.Translate.Z) *
                        KartConstants.GameScale;

                    var aiByamlScale = aiPathPoint.Scale ?? throw new Exception("come on man");
                    
                    segment.RacingLine = aiPathCenter;
                    segment.SafeRacingLine = aiPathCenter;

                    float radius = Math.Max(aiByamlScale.X, Math.Max(aiByamlScale.Y, aiByamlScale.Z)) * KartConstants.GameScale;
                    var aiExtents = new Vector3(radius, 0.0f, 0.0f);
                    
                    Vector3 aiRight = aiPathCenter - Vector3.Transform(aiExtents, quat);
                    Vector3 aiLeft = aiPathCenter + Vector3.Transform(aiExtents, quat);

                    link.RacingLineLimitLeft = aiLeft;
                    link.RacingLineLimitRight = aiRight;

                    link.Left = aiLeft;
                    link.Right = aiRight;

                    link.Width = radius;
                }
                
                _navigation.Waypoints.Add(waypoint);
                line.Segments.Add(segment);
                spatial.Links.Add(link);
                links.Add(link);
            }   
        }

        float distance = 0.0f;
        for (int i = 0; i < links.Count; ++i)
        {
            NavWaypointLink link = links[i];
            NavWaypointLink nextLink = links[(i + 1) % links.Count];
            
            link.To = links[(i + 1) % links.Count].From;
            link.From!.ToLinks = [nextLink];
            // if (i + 1 != links.Count)
            //     link.From!.UnknownWaypoint = _navigation.Waypoints[^1];
            
            link.FromToNormal = Vector3.Normalize(link.To!.Pos - link.From!.Pos);
            link.Plane.Normal = link.From!.Dir;
            link.Plane.Const = Vector3.Dot(link.From!.Pos, link.Plane.Normal);
            
            float delta = Vector3.Distance(link.From!.Pos, link.To!.Pos);
            line.Segments[i].RacingLineLength = delta;
            link.Length = delta;
            link.From!.TrackDist = distance;
            distance += delta;
        }

        line.TotalLength = distance;

        Obj? start = _course.Objs.Find(x => x.ObjId == 6003);
        if (start == null) throw new Exception($"Could not find track start object!");

        NavWaypoint startWaypoint = _navigation.Waypoints.First();
        Quaternion startRotation = MathUtils.ToQuaternion(new Vector3(start.Rotate.X, start.Rotate.Y, start.Rotate.Z));
        var startMarker = new NavTrackMarker
        {
            Pos = new Vector3(start.Translate.X, start.Translate.Y, start.Translate.Z) * KartConstants.GameScale,
            Up = Vector3.Transform(Vector3.UnitY, startRotation),
            Dir = Vector3.Transform(Vector3.UnitZ, startRotation),
            
            // Pos = startWaypoint.Pos,
            // Up = startWaypoint.Up,
            // Dir = startWaypoint.Dir,
            
            Flags = 16,
            JumpSpeedPercentage = 0.65f,
            Radius = 15.0f,
            Text = string.Empty,
            Type = 2,
            
            // Just link it to the first waypoint, I guess?
            Waypoint = _navigation.Waypoints.First()
        };
        var startingRacingLinkMarker = new NavTrackMarker
        {
            Pos = startMarker.Pos,
            Up = startMarker.Up,
            Dir = startMarker.Dir,
            Type = 0x11,
            Waypoint = _navigation.Waypoints.First()
        };
        
        _navigation.TrackMarkers.Add(startMarker);
        _navigation.TrackMarkers.Add(startingRacingLinkMarker);
        _navigation.NavStarts.Add(new NavStart
        {
            DriveMode = 2,
            TrackMarker = startMarker
        });

        _navigation.TotalTrackDist = distance;
        
        // Gliding sections need separate path assists

        const bool enableGliderTransform = false;
        if (enableGliderTransform && _course.GlidePaths.Count != 0)
        {
            var folder = SeInstanceNode.CreateObject<SeInstanceFolderNode>(SeDefinitionFolderNode.Default);
            folder.UidName = "Gliders";
            folder.Parent = _furnitureFolder;
        
            foreach (GlidePath path in _course.GlidePaths)
            {
                var first = path.Points.First();
                var last = path.Points.Last();
                if (first == null || last == null) continue;
                
                (var startGateDefinition, var startGateInstance) = SeNodeBase.Create<TriggerPhantomDefinitionNode, TriggerPhantomInstanceNode>();
                (var endGateDefinition, var endGateInstance) = SeNodeBase.Create<TriggerPhantomDefinitionNode, TriggerPhantomInstanceNode>();
        
                var firstScale = first.Scale ?? throw new Exception("How is the scale missing?");
                var lastScale = last.Scale ?? throw new Exception("How is the scale missing?");
        
                startGateDefinition.WidthRadius = Math.Max(firstScale.X, Math.Max(firstScale.Y, firstScale.Z)) * KartConstants.GameScale;
                endGateDefinition.WidthRadius = Math.Max(lastScale.X, Math.Max(lastScale.Y, lastScale.Z)) * KartConstants.GameScale;
        
                startGateInstance.Translation = new Vector3(first.Translate.X, first.Translate.Y, first.Translate.Z) *
                                                KartConstants.GameScale;
                endGateInstance.Translation = new Vector3(last.Translate.X, last.Translate.Y, last.Translate.Z) *
                                              KartConstants.GameScale;
        
                startGateInstance.PhantomFlags = (int)VehicleFlags.TriggerCar;
                endGateInstance.PhantomFlags = (int)VehicleFlags.TriggerPlane;
                startGateInstance.SetAllLapMasks(true);
                endGateInstance.SetAllLapMasks(true);
                
                startGateInstance.MessageText[0] = TriggerPhantomHashInfo.TransformPlane;
                startGateInstance.MessageText[1] = TriggerPhantomHashInfo.TransformRespot;
                startGateInstance.LinkedNode[1] = startGateInstance;
                endGateInstance.MessageText[0] = TriggerPhantomHashInfo.TransformCar;
                endGateInstance.MessageText[1] = TriggerPhantomHashInfo.TransformRespot;
                endGateInstance.LinkedNode[1] = endGateInstance;
                
                startGateInstance.SetNameWithTimestamp("Glider_CartoPlane");
                endGateInstance.SetNameWithTimestamp("Glider_PlanetoCar");
                
                startGateInstance.Parent = folder;
                endGateInstance.Parent = folder;
            }
        }
        
        Console.WriteLine("[TrackImporter] Finished setting up navigation data!");

        return;
        
        EnemyPathPoint? FindClosestEnemyPathPoint(LapPathPoint lapPoint)
        {
            EnemyPathPoint? closest = null;
            float distance = float.PositiveInfinity;
            var lapPointPos = new Vector3(lapPoint.Translate.X, lapPoint.Translate.Y, lapPoint.Translate.Z);
            foreach (EnemyPath enemyPath in _course.EnemyPaths)
            {
                if (enemyPath.Points[0].Priority != EnemyPathPoint.AIPriority.Default) continue;
                foreach (EnemyPathPoint enemyPoint in enemyPath.Points)
                {
                    var enemyPointPos = new Vector3(enemyPoint.Translate.X, enemyPoint.Translate.Y, enemyPoint.Translate.Z);
                    float localDistance = Vector3.Distance(lapPointPos, enemyPointPos);
                    if (localDistance > distance) continue;
                    
                    distance = localDistance;
                    closest = enemyPoint;

                }
            }
            
            return closest;
        }
    }
}