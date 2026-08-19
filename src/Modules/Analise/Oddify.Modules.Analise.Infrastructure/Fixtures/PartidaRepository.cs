using Microsoft.EntityFrameworkCore;
using Oddify.Modules.Analise.Domain.Fixtures;
using Oddify.Modules.Analise.Infrastructure.Database;

namespace Oddify.Modules.Analise.Infrastructure.Fixtures;

internal sealed class PartidaRepository(AnaliseDbContext context) : IPartidaRepository
{
    public async Task<Partida?> GetAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await context.Partidas.SingleOrDefaultAsync(p => p.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyCollection<Partida>> GetRecentesPorEquipeAsync(
        Guid equipeId,
        int quantidade,
        CancellationToken cancellationToken = default)
    {
        return await context.Partidas
            .Where(p => p.GolsCasa != null && p.GolsVisitante != null)
            .Where(p => p.EquipeCasaId == equipeId || p.EquipeVisitanteId == equipeId)
            .OrderByDescending(p => p.DataUtc)
            .Take(quantidade)
            .ToListAsync(cancellationToken);
    }

    public void Insert(Partida partida)
    {
        context.Partidas.Add(partida);
    }
}
