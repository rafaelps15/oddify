using Oddify.Modules.Apostas.Domain.JornadasDeAlavancagem;

namespace Oddify.Modules.Apostas.Application.JornadasDeAlavancagem.GetJornada;

public sealed record JornadaResponse(
    Guid Id,
    FaixaDeMeta FaixaMeta,
    int PassoAtual,
    int TotalDePassos,
    int NumeroDeFracoes,
    decimal ValorInicial,
    decimal ValorAtual,
    decimal ValorObjetivo,
    decimal BancaMinima,
    StatusDaJornada Status,
    decimal ProbabilidadeDeConclusao,
    IReadOnlyCollection<ApostaDoPassoAtualResponse> ApostasDoPassoAtual);
