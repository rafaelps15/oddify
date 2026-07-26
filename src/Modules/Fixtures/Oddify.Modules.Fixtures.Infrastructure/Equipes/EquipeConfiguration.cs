using Oddify.Modules.Fixtures.Domain.Equipes;
using Oddify.Modules.Fixtures.Domain.Ligas;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Oddify.Modules.Fixtures.Infrastructure.Equipes;

internal sealed class EquipeConfiguration : IEntityTypeConfiguration<Equipe>
{
    public void Configure(EntityTypeBuilder<Equipe> builder)
    {
        builder.Property(e => e.IdExterno).HasMaxLength(100);
        builder.Property(e => e.Nome).HasMaxLength(200);

        builder.HasOne<LigaConfigurada>().WithMany().HasForeignKey(e => e.LigaId);
    }
}
