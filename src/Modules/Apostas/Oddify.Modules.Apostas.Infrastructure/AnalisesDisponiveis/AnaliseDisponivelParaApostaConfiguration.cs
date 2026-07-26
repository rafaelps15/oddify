using Oddify.Modules.Apostas.Domain.AnalisesDisponiveis;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Oddify.Modules.Apostas.Infrastructure.AnalisesDisponiveis;

internal sealed class AnaliseDisponivelParaApostaConfiguration : IEntityTypeConfiguration<AnaliseDisponivelParaAposta>
{
    public void Configure(EntityTypeBuilder<AnaliseDisponivelParaAposta> builder)
    {
        builder.Property(a => a.Mercado).HasMaxLength(100);
    }
}
