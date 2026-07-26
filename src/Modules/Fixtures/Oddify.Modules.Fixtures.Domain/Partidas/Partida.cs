using Oddify.Common.Domain;

namespace Oddify.Modules.Fixtures.Domain.Partidas;

public sealed class Partida : Entity
{
    private Partida(
        Guid id,
        string idExterno,
        Guid ligaId,
        Guid equipeCasaId,
        Guid equipeVisitanteId,
        DateTime dataUtc)
    {
        Id = id;
        IdExterno = idExterno;
        LigaId = ligaId;
        EquipeCasaId = equipeCasaId;
        EquipeVisitanteId = equipeVisitanteId;
        DataUtc = dataUtc;
        Situacao = SituacaoDaPartida.Agendada;
    }

    public Guid Id { get; private set; }

    public string IdExterno { get; private set; }

    public Guid LigaId { get; private set; }

    public Guid EquipeCasaId { get; private set; }

    public Guid EquipeVisitanteId { get; private set; }

    public DateTime DataUtc { get; private set; }

    public SituacaoDaPartida Situacao { get; private set; }

    public int? GolsCasa { get; private set; }

    public int? GolsVisitante { get; private set; }

    public static Partida Create(string idExterno, Guid ligaId, Guid equipeCasaId, Guid equipeVisitanteId, DateTime dataUtc)
    {
        var partida = new Partida(Guid.NewGuid(), idExterno, ligaId, equipeCasaId, equipeVisitanteId, dataUtc);

        partida.Raise(new PartidaAgendadaDomainEvent(partida.Id));

        return partida;
    }

    public Result RegistrarResultado(int golsCasa, int golsVisitante)
    {
        if (Situacao != SituacaoDaPartida.Agendada)
        {
            return Result.Failure(PartidaErrors.JaEncerrada(Id));
        }

        GolsCasa = golsCasa;
        GolsVisitante = golsVisitante;
        Situacao = SituacaoDaPartida.Encerrada;

        Raise(new PartidaEncerradaDomainEvent(Id, golsCasa, golsVisitante));

        return Result.Success();
    }

    public Result Reagendar(DateTime novaDataUtc)
    {
        if (Situacao != SituacaoDaPartida.Agendada)
        {
            return Result.Failure(PartidaErrors.JaEncerrada(Id));
        }

        if (DataUtc == novaDataUtc)
        {
            return Result.Success();
        }

        DataUtc = novaDataUtc;

        Raise(new PartidaReagendadaDomainEvent(Id, novaDataUtc));

        return Result.Success();
    }

    public Result MarcarComoLiquidada()
    {
        if (Situacao != SituacaoDaPartida.Encerrada)
        {
            return Result.Failure(PartidaErrors.AindaNaoEncerrada(Id));
        }

        Situacao = SituacaoDaPartida.Liquidada;

        Raise(new PartidaLiquidadaDomainEvent(Id));

        return Result.Success();
    }
}
