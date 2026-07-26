using Microsoft.EntityFrameworkCore;
using Oddify.Modules.Fixtures.Domain.Equipes;
using Oddify.Modules.Fixtures.Infrastructure.Database;

namespace Oddify.Modules.Fixtures.Infrastructure.Equipes;

internal sealed class EquipeRepository(FixturesDbContext context) : IEquipeRepository
{
    public async Task<Equipe?> GetAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await context.Equipes.SingleOrDefaultAsync(e => e.Id == id, cancellationToken);
    }

    public async Task<Equipe?> GetByIdExternoAsync(string idExterno, CancellationToken cancellationToken = default)
    {
        return await context.Equipes.SingleOrDefaultAsync(e => e.IdExterno == idExterno, cancellationToken);
    }

    public void Insert(Equipe equipe)
    {
        context.Equipes.Add(equipe);
    }
}
