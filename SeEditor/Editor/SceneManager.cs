using SeEditor.Managers;
using SlLib.Resources.Database;
using SlLib.Resources.Scene;
using SlLib.SumoTool;
using SlLib.SumoTool.Siff;

namespace SeEditor.Editor;

public class SceneManager
{
    /// <summary>
    ///     The currently loaded scene.
    /// </summary>
    public static Scene Current { get; private set; } = new();
    
    /// <summary>
    ///     Whether to only render navigation paths.
    /// </summary>
    public static bool RenderNavigationOnly { get; set; }

    /// <summary>
    ///     Whether rendering is enabled.
    /// </summary>
    public static bool DisableRendering { get; set; } = false;

    /// <summary>
    ///     Loads a scene and sets it as the active scene.
    /// </summary>
    /// <param name="path">Path to the database file</param>
    /// <param name="extension">Optional platform extension, defaults to Win32</param>
    public static void LoadScene(string path, string extension = "pc")
    {
        SlResourceDatabase database = SlFile.GetSceneDatabase(path, extension) ??
                                       throw new FileNotFoundException(
                                           $"Unable to load scene database at {path}, because it doesn't exist!");
        var scene = new Scene
        {
            SourceFileName = path,
            Database = database,
        };
        
        string navFilePath = $"{path}.nav{extension}";
        byte[]? navFileData = SlFile.GetFile(navFilePath);
        if (navFileData != null)
        {
            string sceneFilePath = $"{path}.cpu.s{extension}";
            SlPlatform platform = SlPlatform.GuessPlatformFromExtension(sceneFilePath);
            try
            {
                SiffFile siff = SiffFile.Load(platform.GetDefaultContext(), navFileData);
                if (!siff.HasResource(SiffResourceType.Navigation))
                    Console.WriteLine($"Siff file does not contain navigation data!");
                else 
                    scene.Navigation = siff.LoadResource<Navigation>(SiffResourceType.Navigation);
            }
            catch (Exception)
            {
                Console.WriteLine("An error occurred while loading the navigation data for the scene!");
            }
        }
        else
        {
            Console.WriteLine("Could not find a navigation file for this scene!");   
        }
        
        SetActiveScene(scene);
    }

    /// <summary>
    ///     Sets a new scene to be edited.
    /// </summary>
    /// <param name="scene">New active scene</param>
    public static void SetActiveScene(Scene scene)
    {
        Current = scene;
        
        // Make sure the new default active camera is the new scene's camera
        SceneCamera.Active = scene.Camera;
        
        // Clear all selection data.
        Selection.ActiveNode = null;
        Selection.Clipboard.Clear();
    }
}