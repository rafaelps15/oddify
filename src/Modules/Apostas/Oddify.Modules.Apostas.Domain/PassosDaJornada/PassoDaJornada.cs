using Oddify.Common.Domain;

namespace Oddify.Modules.Apostas.Domain.PassosDaJornada;

// Uma tentativa de um passo da jornada (ver JornadaDeAlavancagem). Numero não é único por
// jornada: quando um passo não avança mas o valor recomposto ainda cobre a banca mínima da faixa
// (ver AvaliarPassoDaJornadaCommandHandler), a jornada tenta de novo com um NOVO PassoDaJornada no
// mesmo Numero, em vez de reescrever este — histórico completo de tentativas fica preservado.
public sealed class PassoDaJornada : Entity
{
    private PassoDaJornada()
    {
    }

    public Guid Id { get; private set; }

    public Guid JornadaId { get; private set; }

    public int Numero { get; private set; }

    public decimal ValorDoPasso { get; private set; }

    public int NumeroDeApostas { get; private set; }

    public StatusDoPasso Status { get; private set; }

    public decimal? ValorResultante { get; private set; }

    public DateTime CriadoEmUtc { get; private set; }

    public static PassoDaJornada Create(Guid jornadaId, int numero, decimal valorDoPasso, int numeroDeApostas, DateTime criadoEmUtc)
    {
        var passo = new PassoDaJornada
        {
            Id = Guid.NewGuid(),
            JornadaId = jornadaId,
            Numero = numero,
            ValorDoPasso = valorDoPasso,
            NumeroDeApostas = numeroDeApostas,
            Status = StatusDoPasso.Montado,
            CriadoEmUtc = criadoEmUtc
        };

        passo.Raise(new PassoDaJornadaCriadoDomainEvent(passo.Id, jornadaId));

        return passo;
    }

    // As N apostas do passo já foram criadas e vinculadas (ver MontarPassoDaJornadaCommandHandler)
    // — passo passa a aguardar a liquidação delas.
    public Result MarcarEmAberto()
    {
        if (Status != StatusDoPasso.Montado)
        {
            return Result.Failure(PassoDaJornadaErrors.NaoEstaMontado(Id));
        }

        Status = StatusDoPasso.EmAberto;

        return Result.Success();
    }

    public Result MarcarAvancou(decimal valorResultante)
    {
        if (Status != StatusDoPasso.EmAberto)
        {
            return Result.Failure(PassoDaJornadaErrors.NaoEstaEmAberto(Id));
        }

        Status = StatusDoPasso.Avancou;
        ValorResultante = valorResultante;

        return Result.Success();
    }

    public Result MarcarQuebrou(decimal valorResultante)
    {
        if (Status != StatusDoPasso.EmAberto)
        {
            return Result.Failure(PassoDaJornadaErrors.NaoEstaEmAberto(Id));
        }

        Status = StatusDoPasso.Quebrou;
        ValorResultante = valorResultante;

        return Result.Success();
    }
}
