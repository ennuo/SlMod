using System.Text.Json.Serialization;
using SlLib.Serialization;

namespace SlLib.Resources.Database;

public class SlResPtr<T> where T : ISumoResource, new()
{
    private readonly SlResourceDatabase? _database;
    public readonly int Id;

    private T? _instance;
    private bool _loaded;

    /// <summary>
    ///     Constructs a lazy resource pointer from database and ID.
    /// </summary>
    /// <param name="database">Database the resource is contained in</param>
    /// <param name="id">ID of the resource</param>
    public SlResPtr(SlResourceDatabase? database, int id)
    {
        _database = database;
        Id = id;
    }

    /// <summary>
    ///     Constructs a resource pointer from a resource.
    /// </summary>
    /// <param name="resource">Resource reference</param>
    public SlResPtr(T? resource)
    {
        _loaded = true;
        if (resource == null) return;

        Id = resource.Header.Id;
        _instance = resource;
    }

    /// <summary>
    ///     Creates a null resource pointer.
    /// </summary>
    private SlResPtr()
    {
        _loaded = true;
    }

    [JsonIgnore]
    public T? Instance
    {
        get
        {
            // If we've already tried to load the resource, return the instance.
            if (_loaded) return _instance;

            // Only want to try to load the resource once.
            _loaded = true;

            // Can't lazy load the resource if it's null or if there's no attached database.
            if (Id == 0 || _database == null) return _instance;

            _instance = _database.FindResourceByHash<T>(Id);

            return _instance;
        }
    }

    public bool IsEmpty => Id == 0;

    /// <summary>
    ///     Creates a null resource pointer.
    /// </summary>
    /// <returns>Null resource pointer.</returns>
    public static SlResPtr<T> Empty()
    {
        return new SlResPtr<T>();
    }

    public static implicit operator int(SlResPtr<T> ptr)
    {
        return ptr.Id;
    }

    public static implicit operator T?(SlResPtr<T> ptr)
    {
        return ptr.Instance;
    }
}