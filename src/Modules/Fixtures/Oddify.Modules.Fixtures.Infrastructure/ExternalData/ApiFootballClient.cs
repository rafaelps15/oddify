using System.Globalization;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.RegularExpressions;
using Oddify.Common.Domain;
using Oddify.Modules.Fixtures.Application.Abstractions.ExternalData;

namespace Oddify.Modules.Fixtures.Infrastructure.ExternalData;

internal sealed class ApiFootballClient(HttpClient httpClient, OrcamentoDeRequisicoesApiFootball orcamento) : IApiFootballClient
{
    private static readonly JsonSerializerOptions SerializerOptions = new() { PropertyNameCaseInsensitive = true };

    public async Task<Result<IReadOnlyCollection<FixtureExternoDto>>> GetFixturesAsync(
        string ligaIdExterno,
        int temporada,
        CancellationToken cancellationToken = default)
    {
        if (!await orcamento.TentarConsumirAsync())
        {
            return Result.Failure<IReadOnlyCollection<FixtureExternoDto>>(
                Error.Failure("Fixtures.OrcamentoDeRequisicoesEsgotado", "Cota diária de requisições da API-Football foi esgotada."));
        }

        try
        {
            FixturesResponse? resposta = await httpClient.GetFromJsonAsync<FixturesResponse>(
                $"fixtures?league={ligaIdExterno}&season={temporada.ToString(CultureInfo.InvariantCulture)}",
                SerializerOptions,
                cancellationToken);

            IReadOnlyCollection<FixtureExternoDto> fixtures = resposta?.Response is null
                ? []
                : resposta.Response.Select(MapearFixture).ToList();

            return Result.Success(fixtures);
        }
        catch (HttpRequestException ex)
        {
            return Result.Failure<IReadOnlyCollection<FixtureExternoDto>>(
                Error.Failure("Fixtures.ApiFootballIndisponivel", ex.Message));
        }
    }

    public async Task<Result<IReadOnlyCollection<FixtureAoVivoExternoDto>>> GetFixturesAoVivoAsync(
        IReadOnlyCollection<string> ligaIdsExternos,
        CancellationToken cancellationToken = default)
    {
        if (ligaIdsExternos.Count == 0)
        {
            return Result.Success<IReadOnlyCollection<FixtureAoVivoExternoDto>>([]);
        }

        if (!await orcamento.TentarConsumirAsync())
        {
            return Result.Failure<IReadOnlyCollection<FixtureAoVivoExternoDto>>(
                Error.Failure("Fixtures.OrcamentoDeRequisicoesEsgotado", "Cota diária de requisições da API-Football foi esgotada."));
        }

        try
        {
            FixturesResponse? resposta = await httpClient.GetFromJsonAsync<FixturesResponse>(
                $"fixtures?live={string.Join('-', ligaIdsExternos)}",
                SerializerOptions,
                cancellationToken);

            IReadOnlyCollection<FixtureAoVivoExternoDto> fixtures = resposta?.Response is null
                ? []
                : resposta.Response.Select(MapearFixtureAoVivo).ToList();

            return Result.Success(fixtures);
        }
        catch (HttpRequestException ex)
        {
            return Result.Failure<IReadOnlyCollection<FixtureAoVivoExternoDto>>(
                Error.Failure("Fixtures.ApiFootballIndisponivel", ex.Message));
        }
    }

    public async Task<bool> VerificarStatusAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            using HttpResponseMessage resposta = await httpClient.GetAsync("status", cancellationToken);
            return resposta.IsSuccessStatusCode;
        }
        catch (HttpRequestException)
        {
            return false;
        }
    }

    private static FixtureExternoDto MapearFixture(FixtureDto fixture)
    {
        bool encerrada = fixture.Fixture.Status.Short is "FT" or "AET" or "PEN";

        return new FixtureExternoDto(
            fixture.Fixture.Id.ToString(CultureInfo.InvariantCulture),
            fixture.Teams.Home.Id.ToString(CultureInfo.InvariantCulture),
            fixture.Teams.Home.Name,
            fixture.Teams.Home.Logo,
            fixture.Teams.Away.Id.ToString(CultureInfo.InvariantCulture),
            fixture.Teams.Away.Name,
            fixture.Teams.Away.Logo,
            fixture.Fixture.Date.UtcDateTime,
            encerrada,
            fixture.Goals.Home,
            fixture.Goals.Away,
            ExtrairRodada(fixture.League.Round),
            fixture.League.Flag);
    }

    // Códigos de status curto da api-football: 1H/HT/2H/ET/BT/P/SUSP/INT/LIVE cobrem o jogo em
    // andamento (incluindo intervalo/prorrogação/pênaltis antes do apito final); FT/AET/PEN é
    // "encerrada" (mesmo critério de MapearFixture). Qualquer outro código (NS, PST, CANC, ABD...)
    // não é nem um nem outro — a sincronização ao vivo ignora esse fixture nesse ciclo.
    private static readonly HashSet<string> StatusEmAndamento = ["1H", "HT", "2H", "ET", "BT", "P", "SUSP", "INT", "LIVE"];
    private static readonly HashSet<string> StatusEncerrada = ["FT", "AET", "PEN"];

    private static FixtureAoVivoExternoDto MapearFixtureAoVivo(FixtureDto fixture) =>
        new(
            fixture.Fixture.Id.ToString(CultureInfo.InvariantCulture),
            StatusEmAndamento.Contains(fixture.Fixture.Status.Short),
            StatusEncerrada.Contains(fixture.Fixture.Status.Short),
            fixture.Goals.Home,
            fixture.Goals.Away);

    // league.round da api-football é um rótulo textual (ex.: "Regular Season - 4", "Relegation
    // Round - 3"), não um número isolado — extrai o inteiro final. Rótulos sem número no final
    // (grupos/eliminatórias com nome próprio) caem em 0; não é tratado como erro, só "sem rodada
    // numerada conhecida".
    private static int ExtrairRodada(string? round)
    {
        if (string.IsNullOrWhiteSpace(round))
        {
            return 0;
        }

        Match match = Regex.Match(round, @"\d+$", RegexOptions.None, TimeSpan.FromSeconds(1));

        return match.Success ? int.Parse(match.Value, CultureInfo.InvariantCulture) : 0;
    }

    private sealed record FixturesResponse(IReadOnlyCollection<FixtureDto>? Response);

    private sealed record FixtureDto(FixtureInfoDto Fixture, TeamsDto Teams, GoalsDto Goals, LeagueDto League);

    private sealed record FixtureInfoDto(long Id, DateTimeOffset Date, FixtureStatusDto Status);

    private sealed record FixtureStatusDto(string Short);

    private sealed record LeagueDto(string? Round, string? Flag);

    private sealed record TeamsDto(TeamDto Home, TeamDto Away);

    private sealed record TeamDto(long Id, string Name, string? Logo);

    private sealed record GoalsDto(int? Home, int? Away);
}
