using Oddify.Common.Domain;

namespace Oddify.Modules.Fixtures.Domain.Jogadores;

public sealed class Jogador : Entity
{
    private Jogador(Guid id, string idExterno, Guid equipeId, string nome, string posicao)
    {
        Id = id;
        IdExterno = idExterno;
        EquipeId = equipeId;
        Nome = nome;
        Posicao = posicao;
    }

    public Guid Id { get; private set; }

    public string IdExterno { get; private set; }

    public Guid EquipeId { get; private set; }

    public string Nome { get; private set; }

    public string Posicao { get; private set; }

    public static Jogador Create(string idExterno, Guid equipeId, string nome, string posicao)
    {
        var jogador = new Jogador(Guid.NewGuid(), idExterno, equipeId, nome, posicao);

        jogador.Raise(new JogadorCriadoDomainEvent(jogador.Id));

        return jogador;
    }

    public Result TransferirParaEquipe(Guid novaEquipeId)
    {
        if (EquipeId == novaEquipeId)
        {
            return Result.Failure(JogadorErrors.JaNaEquipe);
        }

        EquipeId = novaEquipeId;

        Raise(new JogadorTransferidoDomainEvent(Id, novaEquipeId));

        return Result.Success();
    }
}
