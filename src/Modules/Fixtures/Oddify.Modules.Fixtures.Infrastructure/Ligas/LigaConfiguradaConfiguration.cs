using Oddify.Modules.Fixtures.Domain.Ligas;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Oddify.Modules.Fixtures.Infrastructure.Ligas;

internal sealed class LigaConfiguradaConfiguration : IEntityTypeConfiguration<LigaConfigurada>
{
    public void Configure(EntityTypeBuilder<LigaConfigurada> builder)
    {
        builder.Property(l => l.IdExterno).HasMaxLength(100);
        builder.Property(l => l.Nome).HasMaxLength(200);

        builder.HasIndex(l => l.IdExterno).IsUnique();
    }
}
