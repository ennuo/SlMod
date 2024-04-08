using System.Text.Json;
using SlLib.Excel;
using SlLib.Filesystem;
using SlLib.Resources;
using SlLib.Resources.Database;
using SlLib.Resources.Scene.Definitions;
using SlLib.Utilities;
using SlLib.Workspace;

const string racerId = "gum";
const string outputFolderPath = "C:/Users/Aidan/Desktop/Scratch/";

ExcelData racerData;
SlPackFile frontendFileSystem;
SlPackFile gameAssetsFileSystem;
SlPackFile gameDataFileSystem;
SlResourceDatabase database;

using (new ContextualTimer("FrontendFileSystem::SlPackFile::Load"))
{
    frontendFileSystem = new SlPackFile("F:/cache/sonic/Frontend");
}

using (new ContextualTimer("GameDataFileSystem::SlPackFile::Load"))
{
    gameDataFileSystem = new SlPackFile("F:/cache/sonic/GameData");
}

using (new ContextualTimer("GameAssetsFileSystem::SlPackFile::Load"))
{
    gameAssetsFileSystem = new SlPackFile("F:/cache/sonic/GameAssets");
}

using (new ContextualTimer("ExcelData::Load"))
{
    if (!gameDataFileSystem.DoesFileExist("gamedata/racers.zat"))
    {
        Console.WriteLine("RacerData doesn't exist!");
        return;
    }

    byte[] data = gameDataFileSystem.GetFile("gamedata/racers.zat");
    CryptUtil.DecodeBuffer(data);

    racerData = ExcelData.Load(data);
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
    string cpuFilePath = path + ".cpu.spc";
    string gpuFilePath = path + ".gpu.spc";

    if (!gameAssetsFileSystem.DoesFileExist(cpuFilePath) || !gameAssetsFileSystem.DoesFileExist(gpuFilePath))
    {
        Console.WriteLine($"{path} doesn't exist!");
        return;
    }

    using Stream cpuStream = gameAssetsFileSystem.GetFileStream(cpuFilePath, out int cpuStreamSize);
    using Stream gpuStream = gameAssetsFileSystem.GetFileStream(gpuFilePath, out _);

    database = SlResourceDatabase.Load(cpuStream, cpuStreamSize, gpuStream);
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


// using (new ContextualTimer("SlModel::ReSerializeToDatabase"))
// {
//     database.AddResource(model);
// }

using (new ContextualTimer("SlResourceDatabase::SaveToFile"))
{
    database.Save($"{outputFolderPath}/database.cpu.spc", $"{outputFolderPath}/database.gpu.spc");
}