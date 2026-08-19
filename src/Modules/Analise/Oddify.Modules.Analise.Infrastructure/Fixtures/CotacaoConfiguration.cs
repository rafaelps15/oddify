using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Oddify.Modules.Analise.Domain.Fixtures;

namespace Oddify.Modules.Analise.Infrastructure.Fixtures;

internal sealed class CotacaoConfiguration : IEntityTypeConfiguration<Cotacao>
{
    public void Configure(EntityTypeBuilder<Cotacao> builder)
    {
        builder.Property(c => c.Mercado).HasMaxLength(100);
        builder.Property(c => c.Casa).HasMaxLength(100);
        builder.HasIndex(c => new { c.PartidaId, c.Mercado });
    }
}
