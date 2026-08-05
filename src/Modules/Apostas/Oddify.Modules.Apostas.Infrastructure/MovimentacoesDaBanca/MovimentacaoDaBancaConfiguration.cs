using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Oddify.Modules.Apostas.Domain.Bancas;
using Oddify.Modules.Apostas.Domain.MovimentacoesDaBanca;

namespace Oddify.Modules.Apostas.Infrastructure.MovimentacoesDaBanca;

internal sealed class MovimentacaoDaBancaConfiguration : IEntityTypeConfiguration<MovimentacaoDaBanca>
{
    public void Configure(EntityTypeBuilder<MovimentacaoDaBanca> builder)
    {
        builder.HasOne<Banca>().WithMany().HasForeignKey(m => m.BancaId);

        builder.HasIndex(m => m.BancaId);
    }
}
