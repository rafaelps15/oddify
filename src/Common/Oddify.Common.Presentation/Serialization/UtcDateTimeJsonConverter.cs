using System.Text.Json;
using System.Text.Json.Serialization;

namespace Oddify.Common.Presentation.Serialization;

// Postgres (via Npgsql) rejeita DateTime com Kind diferente de Utc em colunas
// "timestamp with time zone". Clientes frequentemente enviam datas com offset local
// ou sem Kind explícito (Unspecified) — sem essa normalização na desserialização,
// esses valores só falham tarde demais, como um DbUpdateException 500 dentro do handler.
public sealed class UtcDateTimeJsonConverter : JsonConverter<DateTime>
{
    public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        DateTime value = reader.GetDateTime();

        return value.Kind switch
        {
            DateTimeKind.Utc => value,
            DateTimeKind.Local => value.ToUniversalTime(),
            _ => DateTime.SpecifyKind(value, DateTimeKind.Utc),
        };
    }

    public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
    {
        DateTime utcValue = value.Kind switch
        {
            DateTimeKind.Utc => value,
            DateTimeKind.Local => value.ToUniversalTime(),
            _ => DateTime.SpecifyKind(value, DateTimeKind.Utc),
        };

        writer.WriteStringValue(utcValue);
    }
}
