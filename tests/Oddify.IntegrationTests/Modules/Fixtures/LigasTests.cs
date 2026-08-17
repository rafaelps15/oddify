using System.Net;
using System.Net.Http.Json;
using FluentAssertions;

namespace Oddify.IntegrationTests.Modules.Fixtures;

[Collection(IntegrationTestCollection.Name)]
public sealed class LigasTests(OddifyWebAppFactory factory) : IAsyncLifetime
{
    private readonly HttpClient _client = factory.CreateClient();

    public Task InitializeAsync() => factory.ResetDatabaseAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task CriarLiga_should_return_ok_and_persist_when_request_is_valid()
    {
        HttpResponseMessage criarResponse = await _client.PostAsJsonAsync("ligas", new
        {
            IdExterno = "liga-teste-1",
            Nome = "Liga de Teste",
            MediaDeGols = 2.5m,
            FatorCasa = 1.1m,
        });

        criarResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        Guid ligaId = await criarResponse.Content.ReadFromJsonAsync<Guid>();
        ligaId.Should().NotBeEmpty();

        HttpResponseMessage getResponse = await _client.GetAsync($"ligas/{ligaId}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        LigaResponse? liga = await getResponse.Content.ReadFromJsonAsync<LigaResponse>();
        liga.Should().NotBeNull();
        liga.Nome.Should().Be("Liga de Teste");
        liga.Calibrada.Should().BeFalse();
    }

    [Fact]
    public async Task CriarLiga_should_return_conflict_when_idExterno_already_registered()
    {
        var request = new
        {
            IdExterno = "liga-duplicada",
            Nome = "Liga Original",
            MediaDeGols = 2.5m,
            FatorCasa = 1.1m,
        };

        HttpResponseMessage primeiraResposta = await _client.PostAsJsonAsync("ligas", request);
        primeiraResposta.StatusCode.Should().Be(HttpStatusCode.OK);

        HttpResponseMessage segundaResposta = await _client.PostAsJsonAsync("ligas", request with { Nome = "Outra Liga" });

        segundaResposta.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task CalibrarLiga_should_set_calibrada_to_true()
    {
        HttpResponseMessage criarResponse = await _client.PostAsJsonAsync("ligas", new
        {
            IdExterno = "liga-para-calibrar",
            Nome = "Liga a Calibrar",
            MediaDeGols = 2.5m,
            FatorCasa = 1.1m,
        });

        Guid ligaId = await criarResponse.Content.ReadFromJsonAsync<Guid>();

        HttpResponseMessage calibrarResponse = await _client.PutAsync($"ligas/{ligaId}/calibrar", content: null);
        calibrarResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        LigaResponse? liga = await (await _client.GetAsync($"ligas/{ligaId}")).Content.ReadFromJsonAsync<LigaResponse>();
        liga!.Calibrada.Should().BeTrue();
    }

    private sealed record LigaResponse(Guid Id, string IdExterno, string Nome, decimal MediaDeGols, decimal FatorCasa, bool Calibrada);
}
