namespace Oddify.Modules.Apostas.Application.JornadasDeAlavancagem.GetOportunidadesParaAlavancagem;

public sealed record OportunidadeParaAlavancagemResponse(
    Guid Id,
    Guid PartidaId,
    string Mercado,
    decimal OddDeMercado,
    decimal ProbabilidadeConfirmada,
    decimal Vantagem);
