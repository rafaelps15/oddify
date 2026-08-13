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

    // Só preenchido quando Origem=Alavancagem — liga esta aposta ao passo da jornada que a criou
    // (ver JornadaDeAlavancagem/PassoDaJornada). Guid bruto, nunca navegação, mesmo padrão de
    // MovimentacaoDaBanca.ApostaMultiplaId.
    public Guid? PassoDaJornadaId { get; private set; }

    public DateTime CriadaEmUtc { get; private set; }

    public DateTime AtualizadoEmUtc { get; private set; }

    public static ApostaMultipla Create(
        Guid usuarioId,
        Guid bancaId,
        decimal oddCombinada,
        decimal stake,
        OrigemDaAposta origem,
        string? descricao,
        Guid? passoDaJornadaId,
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
            PassoDaJornadaId = passoDaJornadaId,
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

    // Devolve a aposta pra Pendente e entrega o lucro/perda que ela tinha antes do estorno — quem
    // chama precisa desse valor pra reverter o saldo da banca (mesma separação de responsabilidade
    // de Liquidar: a entidade não conhece Banca, só relata o delta pro handler aplicar).
    public Result<decimal> Estornar(DateTime atualizadoEmUtc)
    {
        if (Resultado == ResultadoDaAposta.Pendente)
        {
            return Result.Failure<decimal>(ApostaMultiplaErrors.AindaNaoLiquidada(Id));
        }

        decimal lucroOuPerdaAnterior = LucroOuPerda!.Value;

        Resultado = ResultadoDaAposta.Pendente;
        LucroOuPerda = null;
        AtualizadoEmUtc = atualizadoEmUtc;

        Raise(new ApostaMultiplaEstornadaDomainEvent(Id));

        return lucroOuPerdaAnterior;
    }
}
