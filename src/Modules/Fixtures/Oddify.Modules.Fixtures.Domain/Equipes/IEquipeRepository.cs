namespace Oddify.Modules.Fixtures.Domain.Equipes;

public interface IEquipeRepository
{
    Task<Equipe?> GetAsync(Guid id, CancellationToken cancellationToken = default);

    void Insert(Equipe equipe);
}
