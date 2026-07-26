using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Oddify.Modules.Fixtures.Domain.Cotacoes;
using Oddify.Modules.Fixtures.Domain.Partidas;

namespace Oddify.Modules.Fixtures.Infrastructure.Cotacoes;

internal sealed class CotacaoConfiguration : IEntityTypeConfiguration<Cotacao>
{
    public void Configure(EntityTypeBuilder<Cotacao> builder)
    {
        builder.Property(c => c.Mercado).HasMaxLength(100);
        builder.Property(c => c.Casa).HasMaxLength(100);

        builder.HasOne<Partida>().WithMany().HasForeignKey(c => c.PartidaId);
    }
}
