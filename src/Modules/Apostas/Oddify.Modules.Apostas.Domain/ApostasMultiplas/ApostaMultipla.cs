using Oddify.Common.Domain;

namespace Oddify.Modules.Apostas.Domain.ApostasMultiplas;

public sealed class ApostaMultipla : Entity
{
    private ApostaMultipla()
    {
    }

    public Guid Id { get; private set; }

    public Guid BancaId { get; private set; }

    public decimal OddCombinada { get; private set; }

    public decimal Stake { get; private set; }

    public ResultadoDaAposta Resultado { get; private set; }

    public decimal? LucroOuPerda { get; private set; }

    public DateTime CriadaEmUtc { get; private set; }

    public static ApostaMultipla Create(Guid bancaId, decimal oddCombinada, decimal stake, DateTime criadaEmUtc)
    {
        var apostaMultipla = new ApostaMultipla
        {
            Id = Guid.NewGuid(),
            BancaId = bancaId,
            OddCombinada = oddCombinada,
            Stake = stake,
            Resultado = ResultadoDaAposta.Pendente,
            CriadaEmUtc = criadaEmUtc
        };

        apostaMultipla.Raise(new ApostaMultiplaCriadaDomainEvent(apostaMultipla.Id));

        return apostaMultipla;
    }

    public Result Liquidar(bool ganhou)
    {
        if (Resultado != ResultadoDaAposta.Pendente)
        {
            return Result.Failure(ApostaMultiplaErrors.JaLiquidada(Id));
        }

        Resultado = ganhou ? ResultadoDaAposta.Ganha : ResultadoDaAposta.Perdida;
        LucroOuPerda = ganhou ? Stake * (OddCombinada - 1) : -Stake;

        Raise(new ApostaMultiplaLiquidadaDomainEvent(Id, LucroOuPerda.Value));

        return Result.Success();
    }
}
