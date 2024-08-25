using SlLib.MarioKart;

// var importer = new TrackImporter(new TrackImportConfig
// { 
//     CourseId = "Gwii_MooMooMeadows", 
//     TrackSource = "skiesofarcadia",
//     TrackTarget = "seasidehill2"
// });

var importer = new TrackImporter(new TrackImportConfig
{ 
    CourseId = "Gu_Cake", 
    TrackSource = "seasidehill2",
    TrackTarget = "seasidehill2"
});

importer.Import();