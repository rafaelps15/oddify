using System.Net;
using System.Net.Http.Json;
using FluentAssertions;

namespace Oddify.IntegrationTests.Modules.Fixtures;

[Collection(IntegrationTestCollection.Name)]
public sealed class PartidasTests(OddifyWebAppFactory factory) : IAsyncLifetime
{
    private readonly HttpClient _client = factory.CreateClient();

    public Task InitializeAsync() => factory.ResetDatabaseAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task CriarPartida_should_persist_rodada_and_temporada()
    {
        (Guid ligaId, Guid equipeCasaId, Guid equipeVisitanteId) = await CriarLigaComDuasEquipesAsync();

        Guid partidaId = await CriarPartidaAsync(ligaId, equipeCasaId, equipeVisitanteId, rodada: 4, temporada: 2026);

        PartidaResponse? partida = await (await _client.GetAsync($"partidas/{partidaId}")).Content.ReadFromJsonAsync<PartidaResponse>();

        partida.Should().NotBeNull();
        partida.Rodada.Should().Be(4);
        partida.Temporada.Should().Be(2026);
    }

    [Fact]
    public async Task GetPartidas_should_filter_by_status_agendadas_and_encerradas()
    {
        (Guid ligaId, Guid equipeCasaId, Guid equipeVisitanteId) = await CriarLigaComDuasEquipesAsync();

        Guid agendadaId = await CriarPartidaAsync(ligaId, equipeCasaId, equipeVisitanteId, rodada: 1, temporada: 2026);
        Guid encerradaId = await CriarPartidaAsync(ligaId, equipeCasaId, equipeVisitanteId, rodada: 1, temporada: 2026);
        await RegistrarResultadoAsync(encerradaId, 2, 1);

        List<PartidaResponse> agendadas = await GetPartidasAsync(ligaId: ligaId, status: "Agendadas");
        List<PartidaResponse> encerradas = await GetPartidasAsync(ligaId: ligaId, status: "Encerradas");

        agendadas.Should().ContainSingle(p => p.Id == agendadaId);
        agendadas.Should().NotContain(p => p.Id == encerradaId);
        encerradas.Should().ContainSingle(p => p.Id == encerradaId);
        encerradas.Should().NotContain(p => p.Id == agendadaId);
    }

    [Fact]
    public async Task GetPartidas_should_filter_by_rodada_and_temporada()
    {
        (Guid ligaId, Guid equipeCasaId, Guid equipeVisitanteId) = await CriarLigaComDuasEquipesAsync();

        Guid rodada1Id = await CriarPartidaAsync(ligaId, equipeCasaId, equipeVisitanteId, rodada: 1, temporada: 2026);
        Guid rodada2Id = await CriarPartidaAsync(ligaId, equipeCasaId, equipeVisitanteId, rodada: 2, temporada: 2026);

        List<PartidaResponse> somenteRodada1 = await GetPartidasAsync(ligaId: ligaId, rodada: 1, temporada: 2026);

        somenteRodada1.Should().ContainSingle(p => p.Id == rodada1Id);
        somenteRodada1.Should().NotContain(p => p.Id == rodada2Id);
    }

    [Fact]
    public async Task GetPartidas_should_filter_by_ids()
    {
        (Guid ligaId, Guid equipeCasaId, Guid equipeVisitanteId) = await CriarLigaComDuasEquipesAsync();

        Guid partida1Id = await CriarPartidaAsync(ligaId, equipeCasaId, equipeVisitanteId, rodada: 1, temporada: 2026);
        Guid partida2Id = await CriarPartidaAsync(ligaId, equipeCasaId, equipeVisitanteId, rodada: 2, temporada: 2026);
        await CriarPartidaAsync(ligaId, equipeCasaId, equipeVisitanteId, rodada: 3, temporada: 2026);

        List<PartidaResponse> resultado = await GetPartidasAsync(ids: [partida1Id, partida2Id]);

        resultado.Should().HaveCount(2);
        resultado.Should().Contain(p => p.Id == partida1Id);
        resultado.Should().Contain(p => p.Id == partida2Id);
    }

    [Fact]
    public async Task GetRodadasDisponiveis_should_return_distinct_ordered_rodadas()
    {
        (Guid ligaId, Guid equipeCasaId, Guid equipeVisitanteId) = await CriarLigaComDuasEquipesAsync();

        await CriarPartidaAsync(ligaId, equipeCasaId, equipeVisitanteId, rodada: 3, temporada: 2026);
        await CriarPartidaAsync(ligaId, equipeCasaId, equipeVisitanteId, rodada: 1, temporada: 2026);
        await CriarPartidaAsync(ligaId, equipeCasaId, equipeVisitanteId, rodada: 3, temporada: 2026);

        HttpResponseMessage response = await _client.GetAsync($"partidas/rodadas-disponiveis?ligaId={ligaId}&temporada=2026");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        int[]? rodadas = await response.Content.ReadFromJsonAsync<int[]>();

        rodadas.Should().Equal(1, 3);
    }

    [Fact]
    public async Task GetRodadaMaisRecenteEncerrada_should_return_null_when_no_rodada_is_fully_finished()
    {
        (Guid ligaId, Guid equipeCasaId, Guid equipeVisitanteId) = await CriarLigaComDuasEquipesAsync();
        await CriarPartidaAsync(ligaId, equipeCasaId, equipeVisitanteId, rodada: 1, temporada: 2026);

        HttpResponseMessage response = await _client.GetAsync($"partidas/rodada-mais-recente-encerrada?ligaId={ligaId}&temporada=2026");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        int? rodada = await response.Content.ReadFromJsonAsync<int?>();

        rodada.Should().BeNull();
    }

    [Fact]
    public async Task GetRodadaMaisRecenteEncerrada_should_return_the_highest_rodada_where_every_match_is_finished()
    {
        (Guid ligaId, Guid equipeCasaId, Guid equipeVisitanteId) = await CriarLigaComDuasEquipesAsync();

        // Rodada 1: totalmente encerrada.
        Guid r1Jogo1 = await CriarPartidaAsync(ligaId, equipeCasaId, equipeVisitanteId, rodada: 1, temporada: 2026);
        await RegistrarResultadoAsync(r1Jogo1, 1, 0);

        // Rodada 2: parcialmente encerrada (um jogo ainda agendado) — não deve contar.
        Guid r2Jogo1 = await CriarPartidaAsync(ligaId, equipeCasaId, equipeVisitanteId, rodada: 2, temporada: 2026);
        await RegistrarResultadoAsync(r2Jogo1, 2, 2);
        await CriarPartidaAsync(ligaId, equipeCasaId, equipeVisitanteId, rodada: 2, temporada: 2026);

        HttpResponseMessage response = await _client.GetAsync($"partidas/rodada-mais-recente-encerrada?ligaId={ligaId}&temporada=2026");
        int? rodada = await response.Content.ReadFromJsonAsync<int?>();

        rodada.Should().Be(1);
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

    private async Task<Guid> CriarPartidaAsync(Guid ligaId, Guid equipeCasaId, Guid equipeVisitanteId, int rodada, int temporada)
    {
        HttpResponseMessage response = await _client.PostAsJsonAsync("partidas", new
        {
            IdExterno = $"partida-{Guid.NewGuid()}",
            LigaId = ligaId,
            EquipeCasaId = equipeCasaId,
            EquipeVisitanteId = equipeVisitanteId,
            DataUtc = DateTime.UtcNow,
            Rodada = rodada,
            Temporada = temporada
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        return await response.Content.ReadFromJsonAsync<Guid>();
    }

    private async Task RegistrarResultadoAsync(Guid partidaId, int golsCasa, int golsVisitante)
    {
        HttpResponseMessage response = await _client.PutAsJsonAsync(
            $"partidas/{partidaId}/registrar-resultado",
            new { GolsCasa = golsCasa, GolsVisitante = golsVisitante });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    private async Task<List<PartidaResponse>> GetPartidasAsync(
        Guid? ligaId = null, string? status = null, int? rodada = null, int? temporada = null, IReadOnlyCollection<Guid>? ids = null)
    {
        var query = new List<string>();
        if (ligaId is not null)
        {
            query.Add($"ligaId={ligaId}");
        }

        if (status is not null)
        {
            query.Add($"status={status}");
        }

        if (rodada is not null)
        {
            query.Add($"rodada={rodada}");
        }

        if (temporada is not null)
        {
            query.Add($"temporada={temporada}");
        }

        if (ids is not null)
        {
            query.AddRange(ids.Select(id => $"ids={id}"));
        }

        string url = "partidas" + (query.Count > 0 ? "?" + string.Join('&', query) : string.Empty);

        HttpResponseMessage response = await _client.GetAsync(url);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        List<PartidaResponse>? partidas = await response.Content.ReadFromJsonAsync<List<PartidaResponse>>();

        return partidas!;
    }

    private sealed record PartidaResponse(
        Guid Id,
        string IdExterno,
        Guid LigaId,
        Guid EquipeCasaId,
        Guid EquipeVisitanteId,
        DateTime DataUtc,
        int Situacao,
        int? GolsCasa,
        int? GolsVisitante,
        int Rodada,
        int Temporada);
}
