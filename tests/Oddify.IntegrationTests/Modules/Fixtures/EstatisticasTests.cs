using System.Net;
using System.Net.Http.Json;
using FluentAssertions;

namespace Oddify.IntegrationTests.Modules.Fixtures;

[Collection(IntegrationTestCollection.Name)]
public sealed class EstatisticasTests(OddifyWebAppFactory factory) : IAsyncLifetime
{
    private readonly HttpClient _client = factory.CreateClient();

    public Task InitializeAsync() => factory.ResetDatabaseAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task GetEstatisticasDeEquipe_should_return_the_stats_registered_for_the_partida()
    {
        (Guid ligaId, Guid equipeCasaId, Guid equipeVisitanteId) = await CriarLigaComDuasEquipesAsync();
        Guid partidaId = await CriarPartidaAsync(ligaId, equipeCasaId, equipeVisitanteId);

        await RegistrarEstatisticaEquipeAsync(partidaId, equipeCasaId, gols: 2, finalizacoes: 10, escanteios: 5, posse: 55.5m);
        await RegistrarEstatisticaEquipeAsync(partidaId, equipeVisitanteId, gols: 1, finalizacoes: 6, escanteios: 3, posse: 44.5m);

        List<EstatisticaEquipeResponse> resultado = await GetEstatisticasDeEquipeAsync(partidaId);

        resultado.Should().HaveCount(2);
        resultado.Should().Contain(e => e.EquipeId == equipeCasaId && e.Gols == 2 && e.Finalizacoes == 10 && e.Escanteios == 5 && e.Posse == 55.5m);
        resultado.Should().Contain(e => e.EquipeId == equipeVisitanteId && e.Gols == 1 && e.Finalizacoes == 6 && e.Escanteios == 3 && e.Posse == 44.5m);
    }

    [Fact]
    public async Task GetEstatisticasDeEquipe_should_return_an_empty_list_when_nothing_was_registered()
    {
        (Guid ligaId, Guid equipeCasaId, Guid equipeVisitanteId) = await CriarLigaComDuasEquipesAsync();
        Guid partidaId = await CriarPartidaAsync(ligaId, equipeCasaId, equipeVisitanteId);

        List<EstatisticaEquipeResponse> resultado = await GetEstatisticasDeEquipeAsync(partidaId);

        resultado.Should().BeEmpty();
    }

    [Fact]
    public async Task GetEstatisticasDeEquipe_should_not_return_stats_from_a_different_partida()
    {
        (Guid ligaId, Guid equipeCasaId, Guid equipeVisitanteId) = await CriarLigaComDuasEquipesAsync();
        Guid partida1Id = await CriarPartidaAsync(ligaId, equipeCasaId, equipeVisitanteId);
        Guid partida2Id = await CriarPartidaAsync(ligaId, equipeCasaId, equipeVisitanteId);

        await RegistrarEstatisticaEquipeAsync(partida1Id, equipeCasaId, gols: 3, finalizacoes: 12, escanteios: 6, posse: 60m);

        List<EstatisticaEquipeResponse> resultado = await GetEstatisticasDeEquipeAsync(partida2Id);

        resultado.Should().BeEmpty();
    }

    [Fact]
    public async Task GetEstatisticasDeJogador_should_return_the_stats_registered_for_the_partida()
    {
        (Guid ligaId, Guid equipeCasaId, Guid equipeVisitanteId) = await CriarLigaComDuasEquipesAsync();
        Guid partidaId = await CriarPartidaAsync(ligaId, equipeCasaId, equipeVisitanteId);
        Guid jogadorId = await CriarJogadorAsync(equipeCasaId);

        await RegistrarEstatisticaJogadorAsync(partidaId, jogadorId, gols: 1, assistencias: 2, minutos: 90, titular: true, nota: 8.2m);

        List<EstatisticaJogadorResponse> resultado = await GetEstatisticasDeJogadorAsync(partidaId);

        resultado.Should().ContainSingle(e =>
            e.JogadorId == jogadorId && e.Gols == 1 && e.Assistencias == 2 && e.Minutos == 90 && e.Titular && e.Nota == 8.2m);
    }

    [Fact]
    public async Task GetEstatisticasDeJogador_should_return_an_empty_list_when_nothing_was_registered()
    {
        (Guid ligaId, Guid equipeCasaId, Guid equipeVisitanteId) = await CriarLigaComDuasEquipesAsync();
        Guid partidaId = await CriarPartidaAsync(ligaId, equipeCasaId, equipeVisitanteId);

        List<EstatisticaJogadorResponse> resultado = await GetEstatisticasDeJogadorAsync(partidaId);

        resultado.Should().BeEmpty();
    }

    private async Task<(Guid LigaId, Guid EquipeCasaId, Guid EquipeVisitanteId)> CriarLigaComDuasEquipesAsync()
    {
        HttpResponseMessage ligaResponse = await _client.PostAsJsonAsync("ligas", new
        {
            IdExterno = $"liga-{Guid.NewGuid()}",
            Nome = "Liga de Teste",
            MediaDeGols = 2.5m,
            FatorCasa = 1.1m
        });
        Guid ligaId = await ligaResponse.Content.ReadFromJsonAsync<Guid>();

        HttpResponseMessage casaResponse = await _client.PostAsJsonAsync("equipes", new
        {
            IdExterno = $"equipe-casa-{Guid.NewGuid()}",
            Nome = "Time da Casa",
            LigaId = ligaId
        });
        Guid equipeCasaId = await casaResponse.Content.ReadFromJsonAsync<Guid>();

        HttpResponseMessage visitanteResponse = await _client.PostAsJsonAsync("equipes", new
        {
            IdExterno = $"equipe-visitante-{Guid.NewGuid()}",
            Nome = "Time Visitante",
            LigaId = ligaId
        });
        Guid equipeVisitanteId = await visitanteResponse.Content.ReadFromJsonAsync<Guid>();

        return (ligaId, equipeCasaId, equipeVisitanteId);
    }

    private async Task<Guid> CriarPartidaAsync(Guid ligaId, Guid equipeCasaId, Guid equipeVisitanteId)
    {
        HttpResponseMessage response = await _client.PostAsJsonAsync("partidas", new
        {
            IdExterno = $"partida-{Guid.NewGuid()}",
            LigaId = ligaId,
            EquipeCasaId = equipeCasaId,
            EquipeVisitanteId = equipeVisitanteId,
            DataUtc = DateTime.UtcNow,
            Rodada = 1,
            Temporada = 2026
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        return await response.Content.ReadFromJsonAsync<Guid>();
    }

    private async Task<Guid> CriarJogadorAsync(Guid equipeId)
    {
        HttpResponseMessage response = await _client.PostAsJsonAsync("jogadores", new
        {
            IdExterno = $"jogador-{Guid.NewGuid()}",
            EquipeId = equipeId,
            Nome = "Jogador de Teste",
            Posicao = "Atacante"
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        return await response.Content.ReadFromJsonAsync<Guid>();
    }

    private async Task RegistrarEstatisticaEquipeAsync(
        Guid partidaId, Guid equipeId, int gols, int finalizacoes, int escanteios, decimal posse)
    {
        HttpResponseMessage response = await _client.PostAsJsonAsync("estatisticas-de-equipe", new
        {
            PartidaId = partidaId,
            EquipeId = equipeId,
            Gols = gols,
            Finalizacoes = finalizacoes,
            Escanteios = escanteios,
            Posse = posse
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    private async Task RegistrarEstatisticaJogadorAsync(
        Guid partidaId, Guid jogadorId, int gols, int assistencias, int minutos, bool titular, decimal nota)
    {
        HttpResponseMessage response = await _client.PostAsJsonAsync("estatisticas-de-jogador", new
        {
            PartidaId = partidaId,
            JogadorId = jogadorId,
            Gols = gols,
            Assistencias = assistencias,
            Minutos = minutos,
            Titular = titular,
            Nota = nota
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    private async Task<List<EstatisticaEquipeResponse>> GetEstatisticasDeEquipeAsync(Guid partidaId)
    {
        HttpResponseMessage response = await _client.GetAsync($"estatisticas-de-equipe?partidaId={partidaId}");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        return (await response.Content.ReadFromJsonAsync<List<EstatisticaEquipeResponse>>())!;
    }

    private async Task<List<EstatisticaJogadorResponse>> GetEstatisticasDeJogadorAsync(Guid partidaId)
    {
        HttpResponseMessage response = await _client.GetAsync($"estatisticas-de-jogador?partidaId={partidaId}");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        return (await response.Content.ReadFromJsonAsync<List<EstatisticaJogadorResponse>>())!;
    }

    private sealed record EstatisticaEquipeResponse(
        Guid Id, Guid PartidaId, Guid EquipeId, int Gols, int Finalizacoes, int Escanteios, decimal Posse);

    private sealed record EstatisticaJogadorResponse(
        Guid Id, Guid PartidaId, Guid JogadorId, int Gols, int Assistencias, int Minutos, bool Titular, decimal Nota);
}
