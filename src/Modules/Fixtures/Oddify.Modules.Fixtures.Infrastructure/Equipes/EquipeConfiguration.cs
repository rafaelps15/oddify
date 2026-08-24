using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Oddify.Modules.Fixtures.Domain.Equipes;
using Oddify.Modules.Fixtures.Domain.Ligas;

namespace Oddify.Modules.Fixtures.Infrastructure.Equipes;

internal sealed class EquipeConfiguration : IEntityTypeConfiguration<Equipe>
{
    public void Configure(EntityTypeBuilder<Equipe> builder)
    {
        builder.Property(e => e.IdExterno).HasMaxLength(100);
        builder.Property(e => e.Nome).HasMaxLength(200);
        builder.Property(e => e.Logo).HasMaxLength(500);

        // GetByIdExternoAsync é escopado por (IdExterno, LigaId) — mesma justificativa do índice
        // único em PartidaConfiguration.IdExterno (upsert sem lock em SincronizarFixturesDaLigaCommandHandler).
        builder.HasIndex(e => new { e.IdExterno, e.LigaId }).IsUnique();

        builder.HasOne<LigaConfigurada>().WithMany().HasForeignKey(e => e.LigaId);
    }
}
