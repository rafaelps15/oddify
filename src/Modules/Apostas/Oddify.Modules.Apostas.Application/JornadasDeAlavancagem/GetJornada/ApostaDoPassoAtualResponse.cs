using Oddify.Modules.Apostas.Domain.ApostasMultiplas;

namespace Oddify.Modules.Apostas.Application.JornadasDeAlavancagem.GetJornada;

public sealed record ApostaDoPassoAtualResponse(
    Guid Id,
    string? Descricao,
    decimal OddCombinada,
    decimal Stake,
    ResultadoDaAposta Resultado,
    decimal? LucroOuPerda);
