using System.Text.Json;
using SlLib.Excel;
using SlLib.Extensions;
using SlLib.Filesystem;
using SlLib.Resources;
using SlLib.Resources.Database;
using SlLib.Resources.Scene.Definitions;
using SlLib.Utilities;
using SlLib.Workspace;

const string racerId = "gum";
const string outputFolderPath = "C:/Users/Aidan/Desktop/Scratch/";

var fs = new MappedFileSystem("F:/cache/sonic/pc");

ExcelData racerData;
SlResourceDatabase database;

using (new ContextualTimer("ExcelData::Load"))
{
    if (!fs.DoesExcelDataExist("gamedata/racers"))
    {
        Console.WriteLine("RacerData doesn't exist!");
        return;        
    }
    
    racerData = fs.GetExcelData("gamedata/racers");
}

Worksheet? worksheet = racerData.GetWorksheet("Racers");
if (worksheet == null)
{
    Console.WriteLine("ExcelData didn't contain Racers worksheet!");
    return;
}

Column? column = worksheet.GetColumnByName(racerId);
if (column == null)
{
    Console.WriteLine($"Column for {racerId} doesn't exist in racer data!");
    return;
}

var options = new JsonSerializerOptions { IncludeFields = true, WriteIndented = true };
string json = JsonSerializer.Serialize(column, options);
File.WriteAllText($"{outputFolderPath}/racer.json", json);

string mayaFile = column.GetString("CharacterMayaFile");
string entityName = column.GetString("CharacterEntity");

using (new ContextualTimer("SlResourceDatabase::Load"))
{
    string path = $"characters/{entityName}/{entityName}";
    if (!fs.DoesSceneExist(path))
    {
        Console.WriteLine($"{path} doesn't exist!");
        return;
    }
    
    database = fs.GetSceneDatabase(path);
}

string entityPartialPath = $"{mayaFile}.mb:se_entity_{entityName}.model";
var entities = database.GetNodesOfType<SeDefinitionEntityNode>();

json = JsonSerializer.Serialize(entities, options);
File.WriteAllText($"{outputFolderPath}/entity.json", json);

SlModel? model;
using (new ContextualTimer("SlModel::Load"))
{
    model = database.FindResourceByHash<SlModel>(-1650896032);
    if (model == null) return;
}

SlSkeleton? skeleton;
using (new ContextualTimer("SlSkeleton::Load"))
{
    skeleton = model.Resource.Skeleton.Instance;
}

using (new ContextualTimer("SlModel::ReSerializeToDatabase"))
{
    database.AddResource(model);
}

using (new ContextualTimer("SlResourceDatabase::SaveToFile"))
{
    database.Save($"{outputFolderPath}/database.cpu.spc", $"{outputFolderPath}/database.gpu.spc");
}

var package = fs.GetSumoToolPackage("ui/viewermodel/animviewer");
package.Save("C:/users/aidan/desktop/animviewer.stz");