using System.Net.Http.Json;

namespace Oddify.IntegrationTests.Modules.Fixtures;

internal static class PartidasFactory
{
    public static async Task<Guid> GivenPartida(
        HttpClient client,
        Guid ligaId,
        Guid equipeCasaId,
        Guid equipeVisitanteId,
        int rodada = 1,
        int temporada = 2026,
        DateTime? dataUtc = null)
    {
        HttpResponseMessage response = await client.PostAsJsonAsync("partidas", new
        {
            IdExterno = $"partida-{Guid.NewGuid()}",
            LigaId = ligaId,
            EquipeCasaId = equipeCasaId,
            EquipeVisitanteId = equipeVisitanteId,
            DataUtc = dataUtc ?? DateTime.UtcNow,
            Rodada = rodada,
            Temporada = temporada,
        });

        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<Guid>();
    }

    // Não é um "Given" (não cria, transiciona uma Partida já existente) — passo de arrange usado
    // condicionalmente pelos testes que precisam de uma partida encerrada, por isso fica fora do
    // padrão GivenX mas ainda na Factory por ser reaproveitado entre PartidasTests/EstatisticasTests.
    public static async Task RegistrarResultado(HttpClient client, Guid partidaId, int golsCasa, int golsVisitante)
    {
        HttpResponseMessage response = await client.PutAsJsonAsync(
            $"partidas/{partidaId}/registrar-resultado",
            new { GolsCasa = golsCasa, GolsVisitante = golsVisitante });

        response.EnsureSuccessStatusCode();
    }
}
