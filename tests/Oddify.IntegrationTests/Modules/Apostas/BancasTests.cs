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
            Nome = "Banca principal",
            SaldoInicial = 1000m,
            PercentualPorEntrada = 0.05m,
            PerfilDeRisco = 1, // Moderado
            ModoPaperTrading = true,
        });

        criarResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        Guid bancaId = await criarResponse.Content.ReadFromJsonAsync<Guid>();
        bancaId.Should().NotBeEmpty();

        HttpResponseMessage getResponse = await _client.GetAsync($"bancas/{bancaId}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        BancaResponse? banca = await getResponse.Content.ReadFromJsonAsync<BancaResponse>();
        banca.Should().NotBeNull();
        banca.Nome.Should().Be("Banca principal");
        banca.SaldoAtual.Should().Be(1000m);
        banca.PercentualPorEntrada.Should().Be(0.05m);
        banca.PerfilDeRisco.Should().Be(1);
        banca.ModoPaperTrading.Should().BeTrue();
    }

    [Fact]
    public async Task GetBanca_should_return_not_found_for_unknown_id()
    {
        HttpResponseMessage response = await _client.GetAsync($"bancas/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetBancas_should_list_the_bancas_just_created()
    {
        await _client.PostAsJsonAsync("bancas", new
        {
            Nome = "Banca principal",
            SaldoInicial = 1000m,
            PercentualPorEntrada = 0.05m,
            PerfilDeRisco = 1, // Moderado
            ModoPaperTrading = true,
        });
        await _client.PostAsJsonAsync("bancas", new
        {
            Nome = "Banca paper trading",
            SaldoInicial = 500m,
            PercentualPorEntrada = 0.03m,
            PerfilDeRisco = 0, // Conservador
            ModoPaperTrading = true,
        });

        HttpResponseMessage response = await _client.GetAsync("bancas");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        List<BancaResponse>? bancas = await response.Content.ReadFromJsonAsync<List<BancaResponse>>();
        bancas.Should().NotBeNull();
        bancas.Should().HaveCount(2);
        bancas.Select(b => b.Nome).Should().BeEquivalentTo("Banca principal", "Banca paper trading");
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

    [Fact]
    public async Task GetSugestaoDeStake_should_return_the_same_stake_KellyCalculator_would_produce()
    {
        // probabilidade 0.60, odd 2.0 -> kelly = (0.60*2.0 - 1) / (2.0 - 1) = 0.20
        // stake = saldo * kelly * fracaoDeKelly(0.25) = 1000 * 0.20 * 0.25 = 50 (mesma conta de KellyCalculatorTests)
        HttpResponseMessage response = await _client.GetAsync("bancas/sugestao-de-stake?saldoDaBanca=1000&odd=2.0&probabilidade=0.60");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        SugestaoDeStakeResponse? sugestao = await response.Content.ReadFromJsonAsync<SugestaoDeStakeResponse>();

        sugestao.Should().NotBeNull();
        sugestao.StakeSugerido.Should().Be(50m);
        sugestao.ProbabilidadeImplicita.Should().Be(0.5m);
        sugestao.Vantagem.Should().Be(0.10m);
        sugestao.FracaoDeKelly.Should().Be(0.25m);
        sugestao.TetoDeStakeSobreABanca.Should().Be(0.05m);
    }

    [Fact]
    public async Task GetSugestaoDeStake_should_use_the_explicit_fracaoDeKelly_when_provided()
    {
        // kelly = (0.52*2.0 - 1) / (2.0 - 1) = 0.04 -> stake = 1000 * 0.04 * 1 = 40, abaixo do
        // teto de 5% da banca (50) — escolhido pra não confundir "efeito da fração" com "efeito
        // do teto" no mesmo teste (ver KellyCalculatorTests pros dois cenários separados).
        HttpResponseMessage response = await _client.GetAsync(
            "bancas/sugestao-de-stake?saldoDaBanca=1000&odd=2.0&probabilidade=0.52&fracaoDeKelly=1.0");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        SugestaoDeStakeResponse? sugestao = await response.Content.ReadFromJsonAsync<SugestaoDeStakeResponse>();

        sugestao!.FracaoDeKelly.Should().Be(1.0m);
        sugestao.StakeSugerido.Should().Be(40m);
    }

    [Fact]
    public async Task GetSugestaoDeStake_should_return_problem_when_odd_is_not_greater_than_one()
    {
        HttpResponseMessage response = await _client.GetAsync("bancas/sugestao-de-stake?saldoDaBanca=1000&odd=1.0&probabilidade=0.60");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetAnalisesDisponiveisParaAposta_should_return_an_empty_list_when_nothing_was_synced_yet()
    {
        HttpResponseMessage response = await _client.GetAsync("apostas-multiplas/analises-disponiveis");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        List<object>? disponiveis = await response.Content.ReadFromJsonAsync<List<object>>();
        disponiveis.Should().BeEmpty();
    }

    private sealed record BancaResponse(Guid Id, string Nome, decimal SaldoAtual, decimal PercentualPorEntrada, int PerfilDeRisco, bool ModoPaperTrading);

    private sealed record SugestaoDeStakeResponse(
        decimal ProbabilidadeImplicita,
        decimal Vantagem,
        decimal StakeSugerido,
        decimal FracaoDeKelly,
        decimal TetoDeStakeSobreABanca);
}
