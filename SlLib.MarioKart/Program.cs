using SlLib.IO;
using SlLib.MarioKart;
using SlLib.Resources;
using SlLib.Resources.Database;




var database = SlResourceDatabase.Load($"{KartConstants.GameRoot}/levels/classic_b/classic_b.cpu.spc",
    $"{KartConstants.GameRoot}/levels/classic_b/classic_b.gpu.spc");
SlSceneExporter.Export(database, "C:/Users/Aidan/Desktop/");

var importer = new TrackImporter(new TrackImportConfig
{ 
    CourseId = "gwii_moomoomeadows", 
    TrackSource = "skiesofarcadia",
    TrackTarget = "seasidehill2"
});

// var importer = new TrackImporter(new TrackImportConfig
// { 
//     CourseId = "gu_cake", 
//     TrackSource = "seasidehill2",
//     TrackTarget = "seasidehill2"
// });

// var importer = new TrackImporter(new TrackImportConfig
// { 
//     CourseId = "dgc_babypark", 
//     TrackSource = "seasidehill2",
//     TrackTarget = "seasidehill2"
// });


importer.Import();