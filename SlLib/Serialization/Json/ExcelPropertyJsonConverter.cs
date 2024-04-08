using System.Text.Json;
using System.Text.Json.Serialization;
using SlLib.Lookup;
using SlLib.Utilities;

namespace SlLib.Serialization.Json;

public class ExcelPropertyJsonConverter : JsonConverter<int>
{
    public override int Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        switch (reader.TokenType)
        {
            case JsonTokenType.String:
            {
                string? value = reader.GetString();
                return string.IsNullOrEmpty(value) ? 0 : SlUtil.SumoHash(value);
            }
            case JsonTokenType.Number:
                return reader.GetInt32();
            default:
                throw new JsonException("Invalid excel property name type in JSON!");
        }
    }

    public override void Write(Utf8JsonWriter writer, int value, JsonSerializerOptions options)
    {
        string? name = ExcelPropertyNameLookup.GetPropertyName(value);
        if (string.IsNullOrEmpty(name)) writer.WriteNumberValue(value);
        else writer.WriteStringValue(name);
    }
}