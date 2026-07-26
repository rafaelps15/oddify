using System.Net.Http.Json;
using System.Text.Json;
using Oddify.Common.Domain;
using Oddify.Modules.Fixtures.Application.Abstractions.ExternalData;

namespace Oddify.Modules.Fixtures.Infrastructure.ExternalData;

internal sealed class TheOddsApiClient(HttpClient httpClient) : ITheOddsApiClient
{
    private const string MercadoH2H = "h2h";
    private const string ResultadoEmpate = "Draw";

    private static readonly JsonSerializerOptions SerializerOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower };

    private readonly string _apiKey = Environment.GetEnvironmentVariable("THEODDSAPI_API_KEY") ?? string.Empty;

    public async Task<Result<IReadOnlyCollection<EventoDeOddsExternoDto>>> GetOddsAsync(
        string sportKey,
        CancellationToken cancellationToken = default)
    {
        try
        {
            IReadOnlyCollection<EventoDto>? resposta = await httpClient.GetFromJsonAsync<IReadOnlyCollection<EventoDto>>(
                $"sports/{sportKey}/odds?apiKey={_apiKey}&regions=eu&markets={MercadoH2H}&oddsFormat=decimal",
                SerializerOptions,
                cancellationToken);

            IReadOnlyCollection<EventoDeOddsExternoDto> eventos = resposta is null
                ? []
                : resposta.Select(MapearEvento).ToList();

            return Result.Success(eventos);
        }
        catch (HttpRequestException ex)
        {
            return Result.Failure<IReadOnlyCollection<EventoDeOddsExternoDto>>(
                Error.Failure("Fixtures.TheOddsApiIndisponivel", ex.Message));
        }
    }

    public async Task<bool> VerificarStatusAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            using HttpResponseMessage resposta = await httpClient.GetAsync($"sports?apiKey={_apiKey}", cancellationToken);
            return resposta.IsSuccessStatusCode;
        }
        catch (HttpRequestException)
        {
            return false;
        }
    }

    private static EventoDeOddsExternoDto MapearEvento(EventoDto evento)
    {
        List<OutcomeDeOddsDto> outcomes = [];

        foreach (BookmakerDto bookmaker in evento.Bookmakers ?? [])
        {
            MarketDto? mercadoH2H = bookmaker.Markets?.FirstOrDefault(m => m.Key == MercadoH2H);

            if (mercadoH2H?.Outcomes is null)
            {
                continue;
            }

            foreach (OutcomeDto outcome in mercadoH2H.Outcomes)
            {
                string? mercado = ResolverMercado(outcome.Name, evento.HomeTeam, evento.AwayTeam);

                if (mercado is not null)
                {
                    outcomes.Add(new OutcomeDeOddsDto(bookmaker.Title, mercado, outcome.Price));
                }
            }
        }

        return new EventoDeOddsExternoDto(evento.Id, evento.HomeTeam, evento.AwayTeam, evento.CommenceTime, outcomes);
    }

    private static string? ResolverMercado(string nomeOutcome, string nomeCasa, string nomeVisitante)
    {
        if (string.Equals(nomeOutcome, nomeCasa, StringComparison.OrdinalIgnoreCase))
        {
            return "vitoria_casa";
        }

        if (string.Equals(nomeOutcome, nomeVisitante, StringComparison.OrdinalIgnoreCase))
        {
            return "vitoria_visitante";
        }

        return string.Equals(nomeOutcome, ResultadoEmpate, StringComparison.OrdinalIgnoreCase) ? "empate" : null;
    }

    private sealed record EventoDto(string Id, string HomeTeam, string AwayTeam, DateTime CommenceTime, IReadOnlyCollection<BookmakerDto>? Bookmakers);

    private sealed record BookmakerDto(string Title, IReadOnlyCollection<MarketDto>? Markets);

    private sealed record MarketDto(string Key, IReadOnlyCollection<OutcomeDto>? Outcomes);

    private sealed record OutcomeDto(string Name, decimal Price);
}
