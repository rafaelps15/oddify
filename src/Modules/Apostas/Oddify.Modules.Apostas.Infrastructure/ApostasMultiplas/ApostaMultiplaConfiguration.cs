using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Oddify.Modules.Apostas.Domain.ApostasMultiplas;
using Oddify.Modules.Apostas.Domain.Bancas;
using Oddify.Modules.Apostas.Domain.PassosDaJornada;

namespace Oddify.Modules.Apostas.Infrastructure.ApostasMultiplas;

internal sealed class ApostaMultiplaConfiguration : IEntityTypeConfiguration<ApostaMultipla>
{
    public void Configure(EntityTypeBuilder<ApostaMultipla> builder)
    {
        builder.HasOne<Banca>().WithMany().HasForeignKey(a => a.BancaId);

        // Só preenchido quando Origem=Alavancagem — ver comentário na entidade.
        builder.HasOne<PassoDaJornada>().WithMany().HasForeignKey(a => a.PassoDaJornadaId);

        builder.Property(a => a.Descricao).HasMaxLength(500);

        builder.HasIndex(a => a.UsuarioId);
    }
}
