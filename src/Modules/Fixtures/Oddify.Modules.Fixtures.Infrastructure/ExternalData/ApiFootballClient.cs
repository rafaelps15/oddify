using System.Globalization;
using System.Net.Http.Json;
using System.Text.Json;
using Oddify.Common.Domain;
using Oddify.Modules.Fixtures.Application.Abstractions.ExternalData;

namespace Oddify.Modules.Fixtures.Infrastructure.ExternalData;

internal sealed class ApiFootballClient(HttpClient httpClient) : IApiFootballClient
{
    private static readonly JsonSerializerOptions SerializerOptions = new() { PropertyNameCaseInsensitive = true };

    public async Task<Result<IReadOnlyCollection<FixtureExternoDto>>> GetFixturesAsync(
        string ligaIdExterno,
        int temporada,
        CancellationToken cancellationToken = default)
    {
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
            fixture.Teams.Away.Id.ToString(CultureInfo.InvariantCulture),
            fixture.Teams.Away.Name,
            fixture.Fixture.Date,
            encerrada,
            fixture.Goals.Home,
            fixture.Goals.Away);
    }

    private sealed record FixturesResponse(IReadOnlyCollection<FixtureDto>? Response);

    private sealed record FixtureDto(FixtureInfoDto Fixture, TeamsDto Teams, GoalsDto Goals);

    private sealed record FixtureInfoDto(long Id, DateTime Date, FixtureStatusDto Status);

    private sealed record FixtureStatusDto(string Short);

    private sealed record TeamsDto(TeamDto Home, TeamDto Away);

    private sealed record TeamDto(long Id, string Name);

    private sealed record GoalsDto(int? Home, int? Away);
}
