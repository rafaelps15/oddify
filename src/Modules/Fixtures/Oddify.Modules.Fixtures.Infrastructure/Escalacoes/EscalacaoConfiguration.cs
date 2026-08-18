using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Oddify.Modules.Fixtures.Domain.Equipes;
using Oddify.Modules.Fixtures.Domain.Escalacoes;
using Oddify.Modules.Fixtures.Domain.Partidas;

namespace Oddify.Modules.Fixtures.Infrastructure.Escalacoes;

internal sealed class EscalacaoConfiguration : IEntityTypeConfiguration<Escalacao>
{
    public void Configure(EntityTypeBuilder<Escalacao> builder)
    {
        builder.HasOne<Partida>().WithMany().HasForeignKey(e => e.PartidaId);
        builder.HasOne<Equipe>().WithMany().HasForeignKey(e => e.EquipeId);

        builder.Property(e => e.Formacao).HasMaxLength(20);
        builder.Property(e => e.Tecnico).HasMaxLength(200);
    }
}
