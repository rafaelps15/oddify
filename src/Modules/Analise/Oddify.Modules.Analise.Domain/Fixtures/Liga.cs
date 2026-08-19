using Oddify.Common.Domain;

namespace Oddify.Modules.Analise.Domain.Fixtures;

// Espelho local de LigaConfigurada (módulo Fixtures), sincronizado via LigaAtualizadaIntegrationEvent
// — nunca criado/editado por um caso de uso deste módulo. Sem eventos de domínio próprios (§8 caso 1).
public sealed class Liga : Entity
{
    private Liga()
    {
    }

    public Guid Id { get; private set; }

    public string Nome { get; private set; }

    public decimal MediaDeGols { get; private set; }

    public decimal FatorCasa { get; private set; }

    public bool Calibrada { get; private set; }

    public static Liga Create(Guid id, string nome, decimal mediaDeGols, decimal fatorCasa, bool calibrada)
    {
        var liga = new Liga
        {
            Id = id,
            Nome = nome,
            MediaDeGols = mediaDeGols,
            FatorCasa = fatorCasa,
            Calibrada = calibrada
        };

        return liga;
    }

    public void Atualizar(string nome, decimal mediaDeGols, decimal fatorCasa, bool calibrada)
    {
        Nome = nome;
        MediaDeGols = mediaDeGols;
        FatorCasa = fatorCasa;
        Calibrada = calibrada;
    }
}
