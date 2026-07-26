using Oddify.Common.Domain;

namespace Oddify.Modules.Fixtures.Domain.Equipes;

public sealed class Equipe : Entity
{
    private Equipe(Guid id, string idExterno, string nome, Guid ligaId)
    {
        Id = id;
        IdExterno = idExterno;
        Nome = nome;
        LigaId = ligaId;
    }

    public Guid Id { get; private set; }

    public string IdExterno { get; private set; }

    public string Nome { get; private set; }

    public Guid LigaId { get; private set; }

    public static Equipe Create(string idExterno, string nome, Guid ligaId)
    {
        var equipe = new Equipe(Guid.NewGuid(), idExterno, nome, ligaId);

        equipe.Raise(new EquipeCriadaDomainEvent(equipe.Id));

        return equipe;
    }

    public void Renomear(string nome)
    {
        if (Nome == nome)
        {
            return;
        }

        Nome = nome;

        Raise(new EquipeRenomeadaDomainEvent(Id, nome));
    }
}
