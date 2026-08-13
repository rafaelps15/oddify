using Oddify.Common.Domain;

namespace Oddify.Modules.Apostas.Domain.JornadasDeAlavancagem;

// O valor "atual" da jornada não é duplicado aqui — é sempre Banca.SaldoAtual da banca de
// alavancagem apontada por BancaId (fonte única de saldo, mesmo critério do resto do módulo).
// AvaliarPassoDaJornadaCommandHandler é quem cruza esse saldo com ValorObjetivo/TotalDePassos pra
// decidir se chama AvancarPasso, Concluir ou MarcarQuebrada — a entidade não conhece Banca.
public sealed class JornadaDeAlavancagem : Entity
{
    private JornadaDeAlavancagem()
    {
    }

    public Guid Id { get; private set; }

    public Guid UsuarioId { get; private set; }

    public Guid BancaId { get; private set; }

    public FaixaDeMeta FaixaMeta { get; private set; }

    public decimal ValorInicial { get; private set; }

    public decimal ValorObjetivo { get; private set; }

    public int NumeroDeFracoes { get; private set; }

    public int TotalDePassos { get; private set; }

    public int PassoAtual { get; private set; }

    public StatusDaJornada Status { get; private set; }

    public decimal ProbabilidadeDeConclusao { get; private set; }

    public DateTime CriadoEmUtc { get; private set; }

    public DateTime AtualizadoEmUtc { get; private set; }

    public static JornadaDeAlavancagem Create(
        Guid usuarioId,
        Guid bancaId,
        FaixaDeMeta faixaMeta,
        decimal valorInicial,
        decimal valorObjetivo,
        int numeroDeFracoes,
        int totalDePassos,
        decimal probabilidadeDeConclusao,
        DateTime criadoEmUtc)
    {
        var jornada = new JornadaDeAlavancagem
        {
            Id = Guid.NewGuid(),
            UsuarioId = usuarioId,
            BancaId = bancaId,
            FaixaMeta = faixaMeta,
            ValorInicial = valorInicial,
            ValorObjetivo = valorObjetivo,
            NumeroDeFracoes = numeroDeFracoes,
            TotalDePassos = totalDePassos,
            PassoAtual = 1,
            Status = StatusDaJornada.EmAndamento,
            ProbabilidadeDeConclusao = probabilidadeDeConclusao,
            CriadoEmUtc = criadoEmUtc,
            AtualizadoEmUtc = criadoEmUtc
        };

        jornada.Raise(new JornadaDeAlavancagemCriadaDomainEvent(jornada.Id));

        return jornada;
    }

    // Passo atual avançou (>=2 das N apostas ganharam) e ainda não é o último — segue pro próximo.
    public Result AvancarPasso(DateTime atualizadoEmUtc)
    {
        if (Status != StatusDaJornada.EmAndamento)
        {
            return Result.Failure(JornadaDeAlavancagemErrors.NaoEstaEmAndamento(Id));
        }

        PassoAtual += 1;
        AtualizadoEmUtc = atualizadoEmUtc;

        Raise(new JornadaDeAlavancagemPassoAvancadoDomainEvent(Id, PassoAtual));

        return Result.Success();
    }

    public Result Concluir(DateTime atualizadoEmUtc)
    {
        if (Status != StatusDaJornada.EmAndamento)
        {
            return Result.Failure(JornadaDeAlavancagemErrors.NaoEstaEmAndamento(Id));
        }

        Status = StatusDaJornada.Concluida;
        AtualizadoEmUtc = atualizadoEmUtc;

        Raise(new JornadaDeAlavancagemConcluidaDomainEvent(Id));

        return Result.Success();
    }

    // 0 das N apostas ganharam no passo, ou o valor recomposto ficou abaixo da banca mínima da
    // faixa — a jornada encerra aqui, sem chance de continuar.
    public Result MarcarQuebrada(DateTime atualizadoEmUtc)
    {
        if (Status != StatusDaJornada.EmAndamento)
        {
            return Result.Failure(JornadaDeAlavancagemErrors.NaoEstaEmAndamento(Id));
        }

        Status = StatusDaJornada.Quebrada;
        AtualizadoEmUtc = atualizadoEmUtc;

        Raise(new JornadaDeAlavancagemQuebradaDomainEvent(Id));

        return Result.Success();
    }
}
