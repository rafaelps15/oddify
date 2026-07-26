using Oddify.Modules.Fixtures.Domain.Equipes;
using Oddify.Modules.Fixtures.Domain.Ligas;
using Oddify.Modules.Fixtures.Domain.Partidas;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Oddify.Modules.Fixtures.Infrastructure.Partidas;

internal sealed class PartidaConfiguration : IEntityTypeConfiguration<Partida>
{
    public void Configure(EntityTypeBuilder<Partida> builder)
    {
        builder.Property(p => p.IdExterno).HasMaxLength(100);

        builder.HasOne<LigaConfigurada>().WithMany().HasForeignKey(p => p.LigaId);

        builder.HasOne<Equipe>()
            .WithMany()
            .HasForeignKey(p => p.EquipeCasaId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Equipe>()
            .WithMany()
            .HasForeignKey(p => p.EquipeVisitanteId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
