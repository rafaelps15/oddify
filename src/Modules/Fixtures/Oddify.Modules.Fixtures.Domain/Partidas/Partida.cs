using Oddify.Common.Domain;

namespace Oddify.Modules.Fixtures.Domain.Partidas;

public sealed class Partida : Entity
{
    private Partida()
    {
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

    public int Rodada { get; private set; }

    public int Temporada { get; private set; }

    public static Partida Create(
        string idExterno,
        Guid ligaId,
        Guid equipeCasaId,
        Guid equipeVisitanteId,
        DateTime dataUtc,
        int rodada,
        int temporada)
    {
        var partida = new Partida
        {
            Id = Guid.NewGuid(),
            IdExterno = idExterno,
            LigaId = ligaId,
            EquipeCasaId = equipeCasaId,
            EquipeVisitanteId = equipeVisitanteId,
            DataUtc = dataUtc,
            Situacao = SituacaoDaPartida.Agendada,
            Rodada = rodada,
            Temporada = temporada
        };

        partida.Raise(new PartidaAgendadaDomainEvent(partida.Id));

        return partida;
    }

    public Result RegistrarResultado(int golsCasa, int golsVisitante)
    {
        if (Situacao != SituacaoDaPartida.Agendada && Situacao != SituacaoDaPartida.EmAndamento)
        {
            return Result.Failure(PartidaErrors.JaEncerrada(Id));
        }

        GolsCasa = golsCasa;
        GolsVisitante = golsVisitante;
        Situacao = SituacaoDaPartida.Encerrada;

        Raise(new PartidaEncerradaDomainEvent(Id, golsCasa, golsVisitante));

        return Result.Success();
    }

    // Chamado pela sincronização ao vivo (fixtures?live=... da API-Football) enquanto a partida
    // está em campo — não é a fonte de verdade do resultado final (isso continua sendo
    // RegistrarResultado, inclusive re-sincronizado depois pelo job de temporada normal); só
    // mantém placar/situação visíveis em tempo real. Idempotente turno a turno: repetir o mesmo
    // placar na mesma situação não deveria re-notificar nada, por isso só levanta o evento na
    // primeira transição Agendada → EmAndamento (o "apito inicial"), não a cada atualização de
    // placar.
    public Result AtualizarAoVivo(int golsCasa, int golsVisitante)
    {
        if (Situacao != SituacaoDaPartida.Agendada && Situacao != SituacaoDaPartida.EmAndamento)
        {
            return Result.Failure(PartidaErrors.JaEncerrada(Id));
        }

        bool primeiraVezEmAndamento = Situacao == SituacaoDaPartida.Agendada;

        GolsCasa = golsCasa;
        GolsVisitante = golsVisitante;
        Situacao = SituacaoDaPartida.EmAndamento;

        if (primeiraVezEmAndamento)
        {
            Raise(new PartidaEmAndamentoDomainEvent(Id));
        }

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
