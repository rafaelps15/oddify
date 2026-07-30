using Oddify.Common.Domain;

namespace Oddify.Modules.Fixtures.Domain.EstatisticasDeEquipe;

public sealed class EstatisticaEquipe : Entity
{
    private EstatisticaEquipe()
    {
    }

    public Guid Id { get; private set; }

    public Guid PartidaId { get; private set; }

    public Guid EquipeId { get; private set; }

    public int Gols { get; private set; }

    public int Finalizacoes { get; private set; }

    public int Escanteios { get; private set; }

    public decimal Posse { get; private set; }

    public static EstatisticaEquipe Create(
        Guid partidaId,
        Guid equipeId,
        int gols,
        int finalizacoes,
        int escanteios,
        decimal posse)
    {
        var estatistica = new EstatisticaEquipe
        {
            Id = Guid.NewGuid(),
            PartidaId = partidaId,
            EquipeId = equipeId,
            Gols = gols,
            Finalizacoes = finalizacoes,
            Escanteios = escanteios,
            Posse = posse
        };

        estatistica.Raise(new EstatisticaEquipeRegistradaDomainEvent(estatistica.Id));

        return estatistica;
    }
}
