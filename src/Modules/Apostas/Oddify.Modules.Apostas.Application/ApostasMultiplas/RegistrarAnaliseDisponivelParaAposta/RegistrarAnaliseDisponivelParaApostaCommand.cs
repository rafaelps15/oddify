using Oddify.Common.Application.Messaging;

namespace Oddify.Modules.Apostas.Application.ApostasMultiplas.RegistrarAnaliseDisponivelParaAposta;

public sealed record RegistrarAnaliseDisponivelParaApostaCommand(
    Guid AnaliseId,
    Guid PartidaId,
    string Mercado,
    decimal OddDeMercado,
    decimal ProbabilidadeConfirmada,
    bool Reduzida) : ICommand;
