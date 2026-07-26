using Oddify.Modules.Analise.Domain.Analises;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Oddify.Modules.Analise.Infrastructure.Analises;

internal sealed class AnaliseDePartidaConfiguration : IEntityTypeConfiguration<AnaliseDePartida>
{
    public void Configure(EntityTypeBuilder<AnaliseDePartida> builder)
    {
        builder.Property(a => a.Mercado).HasMaxLength(100);
        builder.Property(a => a.VersaoDoPrompt).HasMaxLength(100);
    }
}
