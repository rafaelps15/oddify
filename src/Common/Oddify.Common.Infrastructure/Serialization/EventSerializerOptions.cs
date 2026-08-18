using System.Text.Json;

namespace Oddify.Common.Infrastructure.Serialization;

// Reutilizado em qualquer lugar que serializa/desserializa domain event ou integration event pra
// JSON (outbox, inbox, IOutboxWriter) — PropertyNameCaseInsensitive é o que permite o construtor
// primário de um evento (parâmetro camelCase, ex. bancaId) casar com a propriedade PascalCase
// serializada (BancaId) na hora de reconstruir o objeto.
internal static class EventSerializerOptions
{
    public static readonly JsonSerializerOptions Instance = new() { PropertyNameCaseInsensitive = true };
}
