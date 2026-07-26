using System.Net;
using System.Net.Http.Json;
using FluentAssertions;

namespace Oddify.IntegrationTests.Modules.Apostas;

[Collection(IntegrationTestCollection.Name)]
public sealed class BancasTests(OddifyWebAppFactory factory) : IAsyncLifetime
{
    private readonly HttpClient _client = factory.CreateClient();

    public Task InitializeAsync() => factory.ResetDatabaseAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task CriarBanca_should_return_ok_and_persist_when_request_is_valid()
    {
        HttpResponseMessage criarResponse = await _client.PostAsJsonAsync("bancas", new
        {
            SaldoInicial = 1000m,
            ModoPaperTrading = true,
        });

        criarResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        Guid bancaId = await criarResponse.Content.ReadFromJsonAsync<Guid>();
        bancaId.Should().NotBeEmpty();

        HttpResponseMessage getResponse = await _client.GetAsync($"bancas/{bancaId}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        BancaResponse? banca = await getResponse.Content.ReadFromJsonAsync<BancaResponse>();
        banca.Should().NotBeNull();
        banca!.SaldoAtual.Should().Be(1000m);
        banca.ModoPaperTrading.Should().BeTrue();
    }

    [Fact]
    public async Task GetBanca_should_return_not_found_for_unknown_id()
    {
        HttpResponseMessage response = await _client.GetAsync($"bancas/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task MontarMultipla_should_return_not_found_when_banca_does_not_exist()
    {
        HttpResponseMessage response = await _client.PostAsJsonAsync("apostas-multiplas/montar", new
        {
            BancaId = Guid.NewGuid(),
            AnaliseIds = new[] { Guid.NewGuid(), Guid.NewGuid() },
        });

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    private sealed record BancaResponse(Guid Id, decimal SaldoAtual, bool ModoPaperTrading);
}
