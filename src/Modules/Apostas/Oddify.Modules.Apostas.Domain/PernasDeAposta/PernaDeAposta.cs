using Oddify.Common.Domain;
using Oddify.Modules.Apostas.Domain.ApostasMultiplas;

namespace Oddify.Modules.Apostas.Domain.PernasDeAposta;

public sealed class PernaDeAposta : Entity
{
    private PernaDeAposta(Guid id, Guid apostaMultiplaId, Guid analiseId, Guid partidaId, string mercado, decimal odd)
    {
        Id = id;
        ApostaMultiplaId = apostaMultiplaId;
        AnaliseId = analiseId;
        PartidaId = partidaId;
        Mercado = mercado;
        Odd = odd;
        Resultado = ResultadoDaAposta.Pendente;
    }

    public Guid Id { get; private set; }

    public Guid ApostaMultiplaId { get; private set; }

    public Guid AnaliseId { get; private set; }

    public Guid PartidaId { get; private set; }

    public string Mercado { get; private set; }

    public decimal Odd { get; private set; }

    public ResultadoDaAposta Resultado { get; private set; }

    public static PernaDeAposta Create(Guid apostaMultiplaId, Guid analiseId, Guid partidaId, string mercado, decimal odd) =>
        new(Guid.NewGuid(), apostaMultiplaId, analiseId, partidaId, mercado, odd);

    public Result Resolver(bool ganhou)
    {
        if (Resultado != ResultadoDaAposta.Pendente)
        {
            return Result.Failure(PernaDeApostaErrors.JaResolvida(Id));
        }

        Resultado = ganhou ? ResultadoDaAposta.Ganha : ResultadoDaAposta.Perdida;

        return Result.Success();
    }
}
