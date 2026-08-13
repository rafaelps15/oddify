using System.Net;
using System.Net.Http.Json;
using FluentAssertions;

namespace Oddify.IntegrationTests.Modules.Fixtures;

[Collection(IntegrationTestCollection.Name)]
public sealed class EquipesTests(OddifyWebAppFactory factory) : IAsyncLifetime
{
    private readonly HttpClient _client = factory.CreateClient();

    public Task InitializeAsync() => factory.ResetDatabaseAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task GetEquipes_should_filter_by_ids_across_different_ligas()
    {
        Guid ligaAId = await CriarLigaAsync();
        Guid ligaBId = await CriarLigaAsync();

        Guid equipeAId = await CriarEquipeAsync(ligaAId, "Time A");
        Guid equipeBId = await CriarEquipeAsync(ligaBId, "Time B");
        await CriarEquipeAsync(ligaBId, "Time C");

        HttpResponseMessage response = await _client.GetAsync($"equipes?ids={equipeAId}&ids={equipeBId}");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        List<EquipeResponse>? equipes = await response.Content.ReadFromJsonAsync<List<EquipeResponse>>();

        equipes.Should().NotBeNull();
        equipes!.Should().HaveCount(2);
        equipes.Should().Contain(e => e.Id == equipeAId && e.Nome == "Time A");
        equipes.Should().Contain(e => e.Id == equipeBId && e.Nome == "Time B");
    }

    [Fact]
    public async Task GetEquipes_should_still_filter_by_ligaId_when_ids_is_not_provided()
    {
        Guid ligaId = await CriarLigaAsync();
        Guid equipeId = await CriarEquipeAsync(ligaId, "Time da Casa");

        HttpResponseMessage response = await _client.GetAsync($"equipes?ligaId={ligaId}");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        List<EquipeResponse>? equipes = await response.Content.ReadFromJsonAsync<List<EquipeResponse>>();

        equipes.Should().NotBeNull();
        equipes!.Should().ContainSingle(e => e.Id == equipeId);
    }

    private async Task<Guid> CriarLigaAsync()
    {
        HttpResponseMessage response = await _client.PostAsJsonAsync("ligas", new
        {
            IdExterno = $"liga-{Guid.NewGuid()}",
            Nome = "Liga de Teste",
            MediaDeGols = 2.5m,
            FatorCasa = 1.1m
        });

        return await response.Content.ReadFromJsonAsync<Guid>();
    }

    private async Task<Guid> CriarEquipeAsync(Guid ligaId, string nome)
    {
        HttpResponseMessage response = await _client.PostAsJsonAsync("equipes", new
        {
            IdExterno = $"equipe-{Guid.NewGuid()}",
            Nome = nome,
            LigaId = ligaId
        });

        return await response.Content.ReadFromJsonAsync<Guid>();
    }

    private sealed record EquipeResponse(Guid Id, string IdExterno, string Nome, Guid LigaId);
}
