using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Oddify.Modules.Apostas.Domain.AnalisesDisponiveis;

namespace Oddify.Modules.Apostas.Infrastructure.AnalisesDisponiveis;

internal sealed class AnaliseDisponivelParaApostaConfiguration : IEntityTypeConfiguration<AnaliseDisponivelParaAposta>
{
    public void Configure(EntityTypeBuilder<AnaliseDisponivelParaAposta> builder)
    {
        builder.Property(a => a.Mercado).HasMaxLength(100);

        // Token de concorrência otimista nativo do Postgres (coluna de sistema xmin, sem coluna
        // nova no schema). MarcarComoUtilizada()/LiberarUso() só protegem o estado em memória: duas
        // transações concorrentes lendo a mesma linha com JaUtilizada=false podiam ambas persistir
        // com sucesso (double-spend da oportunidade entre múltiplas/jornada de alavancagem).
        builder.Property<uint>("Version").IsRowVersion();
    }
}
