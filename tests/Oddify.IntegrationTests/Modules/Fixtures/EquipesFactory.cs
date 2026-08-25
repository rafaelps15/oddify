using System.Net.Http.Json;

namespace Oddify.IntegrationTests.Modules.Fixtures;

internal static class EquipesFactory
{
    public static async Task<Guid> GivenEquipe(HttpClient client, Guid ligaId, string? nome = null)
    {
        HttpResponseMessage response = await client.PostAsJsonAsync("equipes", new
        {
            IdExterno = $"equipe-{Guid.NewGuid()}",
            Nome = nome ?? "Time de Teste",
            LigaId = ligaId,
        });

        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<Guid>();
    }

    // Composto: liga + as duas equipes que a maioria dos testes de Partidas/Estatisticas/Escalacoes
    // precisa como pré-condição — reúne as três chamadas num único Given, no mesmo espírito do
    // GivenUser do add-tests (create + confirm num só passo).
    public static async Task<(Guid LigaId, Guid EquipeCasaId, Guid EquipeVisitanteId)> GivenLigaComDuasEquipes(HttpClient client)
    {
        Guid ligaId = await LigasFactory.GivenLiga(client);
        Guid equipeCasaId = await GivenEquipe(client, ligaId, "Time da Casa");
        Guid equipeVisitanteId = await GivenEquipe(client, ligaId, "Time Visitante");

        return (ligaId, equipeCasaId, equipeVisitanteId);
    }
}
