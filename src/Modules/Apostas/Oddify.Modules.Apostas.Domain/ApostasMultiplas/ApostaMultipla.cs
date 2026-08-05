using Oddify.Common.Domain;

namespace Oddify.Modules.Apostas.Domain.ApostasMultiplas;

public sealed class ApostaMultipla : Entity
{
    private ApostaMultipla()
    {
    }

    public Guid Id { get; private set; }

    public Guid UsuarioId { get; private set; }

    public Guid BancaId { get; private set; }

    public string? Descricao { get; private set; }

    public decimal OddCombinada { get; private set; }

    public decimal Stake { get; private set; }

    public decimal RetornoPotencial { get; private set; }

    public OrigemDaAposta Origem { get; private set; }

    public ResultadoDaAposta Resultado { get; private set; }

    public decimal? LucroOuPerda { get; private set; }

    public DateTime CriadaEmUtc { get; private set; }

    public DateTime AtualizadoEmUtc { get; private set; }

    public static ApostaMultipla Create(
        Guid usuarioId,
        Guid bancaId,
        decimal oddCombinada,
        decimal stake,
        OrigemDaAposta origem,
        string? descricao,
        DateTime criadaEmUtc)
    {
        var apostaMultipla = new ApostaMultipla
        {
            Id = Guid.NewGuid(),
            UsuarioId = usuarioId,
            BancaId = bancaId,
            Descricao = descricao,
            OddCombinada = oddCombinada,
            Stake = stake,
            RetornoPotencial = stake * oddCombinada,
            Origem = origem,
            Resultado = ResultadoDaAposta.Pendente,
            CriadaEmUtc = criadaEmUtc,
            AtualizadoEmUtc = criadaEmUtc
        };

        apostaMultipla.Raise(new ApostaMultiplaCriadaDomainEvent(apostaMultipla.Id));

        return apostaMultipla;
    }

    public Result Liquidar(bool ganhou, DateTime atualizadoEmUtc)
    {
        if (Resultado != ResultadoDaAposta.Pendente)
        {
            return Result.Failure(ApostaMultiplaErrors.JaLiquidada(Id));
        }

        Resultado = ganhou ? ResultadoDaAposta.Ganha : ResultadoDaAposta.Perdida;
        LucroOuPerda = ganhou ? Stake * (OddCombinada - 1) : -Stake;
        AtualizadoEmUtc = atualizadoEmUtc;

        Raise(new ApostaMultiplaLiquidadaDomainEvent(Id, LucroOuPerda.Value));

        return Result.Success();
    }
}
