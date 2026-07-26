using Oddify.Common.Domain;

namespace Oddify.Modules.Fixtures.Domain.EstatisticasDeJogador;

public sealed class EstatisticaJogador : Entity
{
    private EstatisticaJogador(
        Guid id,
        Guid partidaId,
        Guid jogadorId,
        int gols,
        int assistencias,
        int minutos,
        bool titular,
        decimal nota)
    {
        Id = id;
        PartidaId = partidaId;
        JogadorId = jogadorId;
        Gols = gols;
        Assistencias = assistencias;
        Minutos = minutos;
        Titular = titular;
        Nota = nota;
    }

    public Guid Id { get; private set; }

    public Guid PartidaId { get; private set; }

    public Guid JogadorId { get; private set; }

    public int Gols { get; private set; }

    public int Assistencias { get; private set; }

    public int Minutos { get; private set; }

    public bool Titular { get; private set; }

    public decimal Nota { get; private set; }

    public static EstatisticaJogador Create(
        Guid partidaId,
        Guid jogadorId,
        int gols,
        int assistencias,
        int minutos,
        bool titular,
        decimal nota)
    {
        var estatistica = new EstatisticaJogador(
            Guid.NewGuid(),
            partidaId,
            jogadorId,
            gols,
            assistencias,
            minutos,
            titular,
            nota);

        estatistica.Raise(new EstatisticaJogadorRegistradaDomainEvent(estatistica.Id));

        return estatistica;
    }
}
