using SlLib.Archives;
using SlLib.Resources;
using SlLib.Resources.Database;

var pack = new SlPackFile("F:/cache/sonic/frontend");

var xpac = new SsrPackFile(
    "C:\\Program Files (x86)\\Steam\\steamapps\\common\\Sonic and SEGA All Stars Racing\\Resource\\Base.xpac");


byte[]? racerParamData = xpac.GetFile("resource/racerparams.dat");
if (racerParamData != null) File.WriteAllBytes("C:/Users/Aidan/Desktop/racerparams.dat", racerParamData);


byte[]? cpuFile = pack.GetFile("fecharacters/sonic_fe/sonic_fe.cpu.spc");
byte[]? gpuFile = pack.GetFile("fecharacters/sonic_fe/sonic_fe.gpu.spc");
if (cpuFile != null && gpuFile != null)
{
    var database = new SlResourceDatabase(cpuFile, gpuFile);
    foreach (SlResourceChunk chunk in database)
        if (chunk.Type == SlResourceType.SlModel)
        {
            Console.WriteLine("found a model, test?");

            byte[] data = new byte[chunk.Data.Count];
            chunk.Data.CopyTo(data);
            File.WriteAllBytes("C:/Users/Aidan/Desktop/model.bin", data);

            var model = database.LoadResource<SlModel>(chunk.Id);
            if (model != null) break;
        }
}