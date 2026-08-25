using System.Net.Http.Json;

namespace Oddify.IntegrationTests.Modules.Fixtures;

internal static class JogadoresFactory
{
    public static async Task<Guid> GivenJogador(HttpClient client, Guid equipeId, string? nome = null, string posicao = "Atacante")
    {
        HttpResponseMessage response = await client.PostAsJsonAsync("jogadores", new
        {
            IdExterno = $"jogador-{Guid.NewGuid()}",
            EquipeId = equipeId,
            Nome = nome ?? "Jogador de Teste",
            Posicao = posicao,
        });

        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<Guid>();
    }
}
