using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Oddify.Modules.Analise.Domain.Fixtures;

namespace Oddify.Modules.Analise.Infrastructure.Fixtures;

internal sealed class PartidaConfiguration : IEntityTypeConfiguration<Partida>
{
    public void Configure(EntityTypeBuilder<Partida> builder)
    {
        builder.HasIndex(p => p.EquipeCasaId);
        builder.HasIndex(p => p.EquipeVisitanteId);
    }
}
