using BfresLibrary;
using BfresLibrary.Switch;
using DirectXTexNet;
using SlLib.IO;
using SlLib.MarioKart;
using SlLib.Resources;
using SlLib.Resources.Database;

var importer = new TrackImporter(new TrackImportConfig
{ 
    CourseId = "gwii_moomoomeadows", 
    TrackSource = "skiesofarcadia",
    TrackTarget = "seasidehill2"
});
importer.Import();