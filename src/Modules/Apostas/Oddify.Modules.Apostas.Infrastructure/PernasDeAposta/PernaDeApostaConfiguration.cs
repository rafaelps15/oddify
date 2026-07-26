using Oddify.Modules.Apostas.Domain.ApostasMultiplas;
using Oddify.Modules.Apostas.Domain.PernasDeAposta;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Oddify.Modules.Apostas.Infrastructure.PernasDeAposta;

internal sealed class PernaDeApostaConfiguration : IEntityTypeConfiguration<PernaDeAposta>
{
    public void Configure(EntityTypeBuilder<PernaDeAposta> builder)
    {
        builder.Property(p => p.Mercado).HasMaxLength(100);

        builder.HasOne<ApostaMultipla>().WithMany().HasForeignKey(p => p.ApostaMultiplaId);
    }
}
