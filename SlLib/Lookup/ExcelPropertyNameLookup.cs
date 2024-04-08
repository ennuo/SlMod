using System.Text.Json;

namespace SlLib.Lookup;

public static class ExcelPropertyNameLookup
{
    private static readonly Dictionary<uint, string> Lookup;

    static ExcelPropertyNameLookup()
    {
        const string path = "Data/excel.lookup.json";
        if (!File.Exists(path))
            throw new FileNotFoundException($"{path} is missing!");
        string json = File.ReadAllText(path);
        Lookup = JsonSerializer.Deserialize<Dictionary<uint, string>>(json)!;
    }

    public static string? GetPropertyName(int hash)
    {
        Lookup.TryGetValue((uint)hash, out string? name);
        return name;
    }
}