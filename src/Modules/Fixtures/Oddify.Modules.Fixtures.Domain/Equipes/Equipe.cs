using Oddify.Common.Domain;

namespace Oddify.Modules.Fixtures.Domain.Equipes;

public sealed class Equipe : Entity
{
    private Equipe()
    {
    }

    public Guid Id { get; private set; }

    public string IdExterno { get; private set; }

    public string Nome { get; private set; }

    public Guid LigaId { get; private set; }

    public string? Logo { get; private set; }

    public static Equipe Create(string idExterno, string nome, Guid ligaId, string? logo)
    {
        var equipe = new Equipe
        {
            Id = Guid.NewGuid(),
            IdExterno = idExterno,
            Nome = nome,
            LigaId = ligaId,
            Logo = logo
        };

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

    public void AtualizarLogo(string? logo)
    {
        if (Logo == logo)
        {
            return;
        }

        Logo = logo;

        Raise(new EquipeLogoAtualizadoDomainEvent(Id, logo));
    }
}
