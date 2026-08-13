using Microsoft.EntityFrameworkCore;
using Oddify.Modules.Apostas.Domain.JornadasDeAlavancagem;
using Oddify.Modules.Apostas.Infrastructure.Database;

namespace Oddify.Modules.Apostas.Infrastructure.JornadasDeAlavancagem;

internal sealed class JornadaDeAlavancagemRepository(ApostasDbContext context) : IJornadaDeAlavancagemRepository
{
    public async Task<JornadaDeAlavancagem?> GetAsync(Guid id, Guid usuarioId, CancellationToken cancellationToken = default)
    {
        return await context.JornadasDeAlavancagem
            .SingleOrDefaultAsync(j => j.Id == id && j.UsuarioId == usuarioId, cancellationToken);
    }

    public async Task<JornadaDeAlavancagem?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await context.JornadasDeAlavancagem.SingleOrDefaultAsync(j => j.Id == id, cancellationToken);
    }

    public async Task<JornadaDeAlavancagem?> GetEmAndamentoAsync(Guid usuarioId, CancellationToken cancellationToken = default)
    {
        return await context.JornadasDeAlavancagem
            .SingleOrDefaultAsync(j => j.UsuarioId == usuarioId && j.Status == StatusDaJornada.EmAndamento, cancellationToken);
    }

    public void Insert(JornadaDeAlavancagem jornadaDeAlavancagem)
    {
        context.JornadasDeAlavancagem.Add(jornadaDeAlavancagem);
    }
}
