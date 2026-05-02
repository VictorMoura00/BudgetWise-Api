using System.Text.Json;
using System.Text.Json.Serialization;

namespace BudgetWise.Api.Converters;

public sealed class NullableDateOnlyConverter : JsonConverter<DateOnly?>
{
    public override DateOnly? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
            return null;

        var value = reader.GetString();

        if (string.IsNullOrWhiteSpace(value))
            return null;

        return DateOnly.Parse(value);
    }

    public override void Write(Utf8JsonWriter writer, DateOnly? value, JsonSerializerOptions options)
    {
        if (value is null)
            writer.WriteNullValue();
        else
            writer.WriteStringValue(value.Value.ToString("yyyy-MM-dd"));
    }
}
