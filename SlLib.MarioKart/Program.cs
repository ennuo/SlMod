using SlLib.MarioKart;
var importer = new TrackImporter(new TrackImportConfig
{ 
    CourseId = "gwii_moomoomeadows", 
    TrackSource = "skiesofarcadia",
    TrackTarget = "seasidehill2"
});

// var importer = new TrackImporter(new TrackImportConfig
// { 
//     CourseId = "du_mutecity", 
//     TrackSource = "seasidehill2",
//     TrackTarget = "seasidehill2"
// });


importer.Import();