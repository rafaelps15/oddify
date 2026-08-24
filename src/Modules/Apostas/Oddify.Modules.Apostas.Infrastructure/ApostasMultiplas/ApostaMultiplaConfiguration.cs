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

        // Token de concorrência otimista (xmin) — ver comentário em
        // AnaliseDisponivelParaApostaConfiguration. Aqui protege contra dupla liquidação: Liquidar()
        // só verifica Resultado != Pendente em memória, então duas liquidações concorrentes da
        // mesma aposta (endpoint manual vs. job em lote de PartidaEncerrada) podiam ambas persistir.
        builder.Property<uint>("Version").IsRowVersion();
    }
}
