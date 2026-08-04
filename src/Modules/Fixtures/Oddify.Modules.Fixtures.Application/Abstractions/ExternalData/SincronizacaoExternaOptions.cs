namespace Oddify.Modules.Fixtures.Application.Abstractions.ExternalData;

public sealed class SincronizacaoExternaOptions
{
    public bool Habilitada { get; init; } = true;

    public TimeSpan Intervalo { get; init; } = TimeSpan.FromHours(24);

    public Dictionary<string, string> TheOddsApiSportKeys { get; init; } = [];

    // Chave = LigaConfigurada.IdExterno; liga sem entrada aqui usa o ano corrente (UTC) como padrão.
    public Dictionary<string, int> ApiFootballTemporadas { get; init; } = [];
}
