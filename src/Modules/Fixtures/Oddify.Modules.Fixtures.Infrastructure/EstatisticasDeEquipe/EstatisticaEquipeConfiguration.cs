using Oddify.Modules.Fixtures.Domain.Equipes;
using Oddify.Modules.Fixtures.Domain.EstatisticasDeEquipe;
using Oddify.Modules.Fixtures.Domain.Partidas;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Oddify.Modules.Fixtures.Infrastructure.EstatisticasDeEquipe;

internal sealed class EstatisticaEquipeConfiguration : IEntityTypeConfiguration<EstatisticaEquipe>
{
    public void Configure(EntityTypeBuilder<EstatisticaEquipe> builder)
    {
        builder.HasOne<Partida>().WithMany().HasForeignKey(e => e.PartidaId);
        builder.HasOne<Equipe>().WithMany().HasForeignKey(e => e.EquipeId);
    }
}
