using Oddify.Common.Domain;

namespace Oddify.Modules.Fixtures.Domain.Ligas;

public sealed class LigaConfigurada : Entity
{
    private LigaConfigurada(Guid id, string idExterno, string nome, decimal mediaDeGols, decimal fatorCasa)
    {
        Id = id;
        IdExterno = idExterno;
        Nome = nome;
        MediaDeGols = mediaDeGols;
        FatorCasa = fatorCasa;
        Calibrada = false;
    }

    public Guid Id { get; private set; }

    public string IdExterno { get; private set; }

    public string Nome { get; private set; }

    public decimal MediaDeGols { get; private set; }

    public decimal FatorCasa { get; private set; }

    public bool Calibrada { get; private set; }

    public static LigaConfigurada Create(string idExterno, string nome, decimal mediaDeGols, decimal fatorCasa)
    {
        var liga = new LigaConfigurada(Guid.NewGuid(), idExterno, nome, mediaDeGols, fatorCasa);

        liga.Raise(new LigaConfiguradaCriadaDomainEvent(liga.Id));

        return liga;
    }

    public void AtualizarMedias(decimal mediaDeGols, decimal fatorCasa)
    {
        if (MediaDeGols == mediaDeGols && FatorCasa == fatorCasa)
        {
            return;
        }

        MediaDeGols = mediaDeGols;
        FatorCasa = fatorCasa;

        Raise(new LigaMediasAtualizadasDomainEvent(Id, mediaDeGols, fatorCasa));
    }

    public void Calibrar()
    {
        if (Calibrada)
        {
            return;
        }

        Calibrada = true;

        Raise(new LigaCalibradaAlteradaDomainEvent(Id, Calibrada));
    }

    public void Descalibrar()
    {
        if (!Calibrada)
        {
            return;
        }

        Calibrada = false;

        Raise(new LigaCalibradaAlteradaDomainEvent(Id, Calibrada));
    }
}
