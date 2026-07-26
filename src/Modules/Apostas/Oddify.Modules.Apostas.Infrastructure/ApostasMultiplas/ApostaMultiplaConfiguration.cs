using Oddify.Modules.Apostas.Domain.ApostasMultiplas;
using Oddify.Modules.Apostas.Domain.Bancas;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Oddify.Modules.Apostas.Infrastructure.ApostasMultiplas;

internal sealed class ApostaMultiplaConfiguration : IEntityTypeConfiguration<ApostaMultipla>
{
    public void Configure(EntityTypeBuilder<ApostaMultipla> builder)
    {
        builder.HasOne<Banca>().WithMany().HasForeignKey(a => a.BancaId);
    }
}
