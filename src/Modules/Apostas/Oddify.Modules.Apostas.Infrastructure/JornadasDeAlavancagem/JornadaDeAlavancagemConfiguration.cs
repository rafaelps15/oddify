using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Oddify.Modules.Apostas.Domain.Bancas;
using Oddify.Modules.Apostas.Domain.JornadasDeAlavancagem;

namespace Oddify.Modules.Apostas.Infrastructure.JornadasDeAlavancagem;

internal sealed class JornadaDeAlavancagemConfiguration : IEntityTypeConfiguration<JornadaDeAlavancagem>
{
    public void Configure(EntityTypeBuilder<JornadaDeAlavancagem> builder)
    {
        builder.HasOne<Banca>().WithMany().HasForeignKey(j => j.BancaId);

        builder.HasIndex(j => j.UsuarioId);
    }
}
