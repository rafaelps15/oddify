using System.Globalization;
using System.Net.Http.Json;
using System.Text.Json;
using Oddify.Common.Domain;
using Oddify.Modules.Fixtures.Application.Abstractions.ExternalData;

namespace Oddify.Modules.Fixtures.Infrastructure.ExternalData;

internal sealed class TheOddsApiClient(HttpClient httpClient) : ITheOddsApiClient
{
    private const string MercadoH2H = "h2h";
    private const string MercadoTotals = "totals";
    private const string MercadosRequisitados = $"{MercadoH2H},{MercadoTotals}";
    private const string ResultadoEmpate = "Draw";
    private const string OutcomeOver = "Over";
    private const string OutcomeUnder = "Under";

    private static readonly JsonSerializerOptions SerializerOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower };

    private readonly string _apiKey = Environment.GetEnvironmentVariable("THEODDSAPI_API_KEY") ?? string.Empty;

    public async Task<Result<IReadOnlyCollection<EventoDeOddsExternoDto>>> GetOddsAsync(
        string sportKey,
        CancellationToken cancellationToken = default)
    {
        try
        {
            IReadOnlyCollection<EventoDto>? resposta = await httpClient.GetFromJsonAsync<IReadOnlyCollection<EventoDto>>(
                $"sports/{sportKey}/odds?apiKey={_apiKey}&regions=eu&markets={MercadosRequisitados}&oddsFormat=decimal",
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
            foreach (MarketDto mercado in bookmaker.Markets ?? [])
            {
                foreach (OutcomeDto outcome in mercado.Outcomes ?? [])
                {
                    string? mercadoResolvido = mercado.Key switch
                    {
                        MercadoH2H => ResolverMercadoH2H(outcome.Name, evento.HomeTeam, evento.AwayTeam),
                        MercadoTotals => ResolverMercadoTotals(outcome.Name, outcome.Point),
                        _ => null
                    };

                    if (mercadoResolvido is not null)
                    {
                        outcomes.Add(new OutcomeDeOddsDto(bookmaker.Title, mercadoResolvido, outcome.Price));
                    }
                }
            }
        }

        return new EventoDeOddsExternoDto(evento.Id, evento.HomeTeam, evento.AwayTeam, evento.CommenceTime, outcomes);
    }

    private static string? ResolverMercadoH2H(string nomeOutcome, string nomeCasa, string nomeVisitante)
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

    /// <summary>Mapeia o outcome "Over"/"Under" + linha (ex.: 2.5) para o código de mercado do motor (ex.: "over_2_5").</summary>
    private static string? ResolverMercadoTotals(string nomeOutcome, decimal? linha)
    {
        if (linha is null)
        {
            return null;
        }

        string sufixo = FormatarLinha(linha.Value);

        if (string.Equals(nomeOutcome, OutcomeOver, StringComparison.OrdinalIgnoreCase))
        {
            return $"over_{sufixo}";
        }

        return string.Equals(nomeOutcome, OutcomeUnder, StringComparison.OrdinalIgnoreCase) ? $"under_{sufixo}" : null;
    }

    private static string FormatarLinha(decimal linha) =>
        linha.ToString("0.0##", CultureInfo.InvariantCulture).Replace('.', '_');

    private sealed record EventoDto(string Id, string HomeTeam, string AwayTeam, DateTime CommenceTime, IReadOnlyCollection<BookmakerDto>? Bookmakers);

    private sealed record BookmakerDto(string Title, IReadOnlyCollection<MarketDto>? Markets);

    private sealed record MarketDto(string Key, IReadOnlyCollection<OutcomeDto>? Outcomes);

    private sealed record OutcomeDto(string Name, decimal Price, decimal? Point = null);
}
