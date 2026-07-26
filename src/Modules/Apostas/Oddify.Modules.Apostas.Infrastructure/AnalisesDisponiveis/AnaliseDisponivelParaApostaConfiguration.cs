using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Oddify.Modules.Apostas.Domain.AnalisesDisponiveis;

namespace Oddify.Modules.Apostas.Infrastructure.AnalisesDisponiveis;

internal sealed class AnaliseDisponivelParaApostaConfiguration : IEntityTypeConfiguration<AnaliseDisponivelParaAposta>
{
    public void Configure(EntityTypeBuilder<AnaliseDisponivelParaAposta> builder)
    {
        builder.Property(a => a.Mercado).HasMaxLength(100);
    }
}
