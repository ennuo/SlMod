namespace SlLib.Serialization;

public interface IWritable
{
    void Save(ResourceSaveContext context);
}