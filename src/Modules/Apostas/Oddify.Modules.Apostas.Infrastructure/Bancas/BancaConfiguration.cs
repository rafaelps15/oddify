using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Oddify.Modules.Apostas.Domain.Bancas;

namespace Oddify.Modules.Apostas.Infrastructure.Bancas;

internal sealed class BancaConfiguration : IEntityTypeConfiguration<Banca>
{
    public void Configure(EntityTypeBuilder<Banca> builder)
    {
        builder.Property(b => b.Nome).HasMaxLength(100);

        builder.HasIndex(b => b.UsuarioId);
    }
}
