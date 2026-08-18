using Oddify.Common.Domain;

namespace Oddify.Modules.Fixtures.Domain.Escalacoes;

public sealed class Escalacao : Entity
{
    private Escalacao()
    {
    }

    public Guid Id { get; private set; }

    public Guid PartidaId { get; private set; }

    public Guid EquipeId { get; private set; }

    public string Formacao { get; private set; }

    public string Tecnico { get; private set; }

    public static Escalacao Create(Guid partidaId, Guid equipeId, string formacao, string tecnico)
    {
        var escalacao = new Escalacao
        {
            Id = Guid.NewGuid(),
            PartidaId = partidaId,
            EquipeId = equipeId,
            Formacao = formacao,
            Tecnico = tecnico
        };

        escalacao.Raise(new EscalacaoRegistradaDomainEvent(escalacao.Id));

        return escalacao;
    }
}
