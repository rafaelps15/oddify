using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Oddify.Modules.Apostas.Domain.Bancas;

namespace Oddify.Modules.Apostas.Infrastructure.Bancas;

internal sealed class BancaConfiguration : IEntityTypeConfiguration<Banca>
{
    public void Configure(EntityTypeBuilder<Banca> builder)
    {
        builder.Property(b => b.Nome).HasMaxLength(100);

        builder.HasIndex(b => b.UsuarioId);

        // Token de concorrência otimista (xmin) — ver comentário em
        // AnaliseDisponivelParaApostaConfiguration. Aqui protege contra "lost update" de saldo:
        // AjustarSaldo() faz SaldoAtual += delta sem lock; duas movimentações concorrentes na mesma
        // banca (liquidação manual vs. em lote, ou dois depósitos) podiam fazer uma sobrescrever o
        // resultado da outra silenciosamente.
        builder.Property<uint>("Version").IsRowVersion();
    }
}
