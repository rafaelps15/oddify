using Oddify.Common.Domain;

namespace Oddify.Modules.Analise.Domain.Fixtures;

// Espelho local de Partida (módulo Fixtures), sincronizado via PartidaAgendadaIntegrationEvent
// (criação) e PartidaEncerradaIntegrationEvent (resultado) — nunca criado/editado por um caso de
// uso deste módulo. Sem eventos de domínio próprios (§8 caso 1).
public sealed class Partida : Entity
{
    private Partida()
    {
    }

    public Guid Id { get; private set; }

    public Guid LigaId { get; private set; }

    public Guid EquipeCasaId { get; private set; }

    public Guid EquipeVisitanteId { get; private set; }

    public DateTime DataUtc { get; private set; }

    public int? GolsCasa { get; private set; }

    public int? GolsVisitante { get; private set; }

    public static Partida Create(Guid id, Guid ligaId, Guid equipeCasaId, Guid equipeVisitanteId, DateTime dataUtc)
    {
        var partida = new Partida
        {
            Id = id,
            LigaId = ligaId,
            EquipeCasaId = equipeCasaId,
            EquipeVisitanteId = equipeVisitanteId,
            DataUtc = dataUtc
        };

        return partida;
    }

    public void RegistrarResultado(int golsCasa, int golsVisitante)
    {
        GolsCasa = golsCasa;
        GolsVisitante = golsVisitante;
    }
}
