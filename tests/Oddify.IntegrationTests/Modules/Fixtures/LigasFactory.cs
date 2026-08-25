using System.Net.Http.Json;

namespace Oddify.IntegrationTests.Modules.Fixtures;

internal static class LigasFactory
{
    public static async Task<Guid> GivenLiga(HttpClient client, string? nome = null, decimal mediaDeGols = 2.5m, decimal fatorCasa = 1.1m)
    {
        HttpResponseMessage response = await client.PostAsJsonAsync("ligas", new
        {
            IdExterno = $"liga-{Guid.NewGuid()}",
            Nome = nome ?? "Liga de Teste",
            MediaDeGols = mediaDeGols,
            FatorCasa = fatorCasa,
        });

        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<Guid>();
    }
}
