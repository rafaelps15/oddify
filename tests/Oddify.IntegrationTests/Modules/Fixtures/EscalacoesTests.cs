using System.Net;
using System.Net.Http.Json;
using FluentAssertions;

namespace Oddify.IntegrationTests.Modules.Fixtures;

[Collection(IntegrationTestCollection.Name)]
public sealed class EscalacoesTests(OddifyWebAppFactory factory) : IAsyncLifetime
{
    private readonly HttpClient _client = factory.CreateClient();

    public Task InitializeAsync() => factory.ResetDatabaseAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task GetEscalacoes_should_return_the_escalacao_with_its_jogadores_nested()
    {
        (Guid ligaId, Guid equipeCasaId, Guid equipeVisitanteId) = await CriarLigaComDuasEquipesAsync();
        Guid partidaId = await CriarPartidaAsync(ligaId, equipeCasaId, equipeVisitanteId);
        Guid jogadorId = await CriarJogadorAsync(equipeCasaId);

        Guid escalacaoId = await RegistrarEscalacaoAsync(partidaId, equipeCasaId, "4-4-2", "Técnico de Teste");
        await RegistrarEscalacaoJogadorAsync(escalacaoId, jogadorId, titular: true, posicao: "Atacante", numero: 9);

        List<EscalacaoResponse> resultado = await GetEscalacoesAsync(partidaId);

        resultado.Should().ContainSingle();
        EscalacaoResponse escalacao = resultado.Single();
        escalacao.EquipeId.Should().Be(equipeCasaId);
        escalacao.Formacao.Should().Be("4-4-2");
        escalacao.Tecnico.Should().Be("Técnico de Teste");
        escalacao.Jogadores.Should().ContainSingle(j =>
            j.JogadorId == jogadorId && j.Titular && j.Posicao == "Atacante" && j.Numero == 9);
    }

    [Fact]
    public async Task GetEscalacoes_should_return_the_escalacao_with_an_empty_jogadores_list_when_none_was_registered_yet()
    {
        (Guid ligaId, Guid equipeCasaId, Guid equipeVisitanteId) = await CriarLigaComDuasEquipesAsync();
        Guid partidaId = await CriarPartidaAsync(ligaId, equipeCasaId, equipeVisitanteId);

        await RegistrarEscalacaoAsync(partidaId, equipeCasaId, "4-3-3", "Outro Técnico");

        List<EscalacaoResponse> resultado = await GetEscalacoesAsync(partidaId);

        resultado.Should().ContainSingle();
        resultado.Single().Jogadores.Should().BeEmpty();
    }

    [Fact]
    public async Task GetEscalacoes_should_return_an_empty_list_when_nothing_was_registered()
    {
        (Guid ligaId, Guid equipeCasaId, Guid equipeVisitanteId) = await CriarLigaComDuasEquipesAsync();
        Guid partidaId = await CriarPartidaAsync(ligaId, equipeCasaId, equipeVisitanteId);

        List<EscalacaoResponse> resultado = await GetEscalacoesAsync(partidaId);

        resultado.Should().BeEmpty();
    }

    [Fact]
    public async Task GetEscalacoes_should_return_both_teams_when_both_were_registered()
    {
        (Guid ligaId, Guid equipeCasaId, Guid equipeVisitanteId) = await CriarLigaComDuasEquipesAsync();
        Guid partidaId = await CriarPartidaAsync(ligaId, equipeCasaId, equipeVisitanteId);

        await RegistrarEscalacaoAsync(partidaId, equipeCasaId, "4-4-2", "Técnico Casa");
        await RegistrarEscalacaoAsync(partidaId, equipeVisitanteId, "3-5-2", "Técnico Visitante");

        List<EscalacaoResponse> resultado = await GetEscalacoesAsync(partidaId);

        resultado.Should().HaveCount(2);
        resultado.Should().Contain(e => e.EquipeId == equipeCasaId);
        resultado.Should().Contain(e => e.EquipeId == equipeVisitanteId);
    }

    [Fact]
    public async Task RegistrarEscalacaoJogador_should_fail_when_the_escalacao_does_not_exist()
    {
        (_, Guid equipeCasaId, _) = await CriarLigaComDuasEquipesAsync();
        Guid jogadorId = await CriarJogadorAsync(equipeCasaId);

        HttpResponseMessage response = await _client.PostAsJsonAsync("escalacoes-de-jogador", new
        {
            EscalacaoId = Guid.NewGuid(),
            JogadorId = jogadorId,
            Titular = true,
            Posicao = "Atacante",
            Numero = 9
        });

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task RegistrarEscalacao_should_fail_when_the_partida_does_not_exist()
    {
        (_, Guid equipeCasaId, _) = await CriarLigaComDuasEquipesAsync();

        HttpResponseMessage response = await _client.PostAsJsonAsync("escalacoes", new
        {
            PartidaId = Guid.NewGuid(),
            EquipeId = equipeCasaId,
            Formacao = "4-4-2",
            Tecnico = "Técnico de Teste"
        });

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task RegistrarEscalacao_should_fail_when_the_equipe_does_not_exist()
    {
        (Guid ligaId, Guid equipeCasaId, Guid equipeVisitanteId) = await CriarLigaComDuasEquipesAsync();
        Guid partidaId = await CriarPartidaAsync(ligaId, equipeCasaId, equipeVisitanteId);

        HttpResponseMessage response = await _client.PostAsJsonAsync("escalacoes", new
        {
            PartidaId = partidaId,
            EquipeId = Guid.NewGuid(),
            Formacao = "4-4-2",
            Tecnico = "Técnico de Teste"
        });

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    private async Task<Guid> RegistrarEscalacaoAsync(Guid partidaId, Guid equipeId, string formacao, string tecnico)
    {
        HttpResponseMessage response = await _client.PostAsJsonAsync("escalacoes", new
        {
            PartidaId = partidaId,
            EquipeId = equipeId,
            Formacao = formacao,
            Tecnico = tecnico
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        return await response.Content.ReadFromJsonAsync<Guid>();
    }

    private async Task RegistrarEscalacaoJogadorAsync(Guid escalacaoId, Guid jogadorId, bool titular, string posicao, int? numero)
    {
        HttpResponseMessage response = await _client.PostAsJsonAsync("escalacoes-de-jogador", new
        {
            EscalacaoId = escalacaoId,
            JogadorId = jogadorId,
            Titular = titular,
            Posicao = posicao,
            Numero = numero
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    private async Task<List<EscalacaoResponse>> GetEscalacoesAsync(Guid partidaId)
    {
        HttpResponseMessage response = await _client.GetAsync($"escalacoes?partidaId={partidaId}");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        return (await response.Content.ReadFromJsonAsync<List<EscalacaoResponse>>())!;
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

    private sealed record EscalacaoResponse(Guid Id, Guid PartidaId, Guid EquipeId, string Formacao, string Tecnico, List<EscalacaoJogadorResponse> Jogadores);

    private sealed record EscalacaoJogadorResponse(Guid EscalacaoJogadorId, Guid JogadorId, bool Titular, string Posicao, int? Numero);
}
