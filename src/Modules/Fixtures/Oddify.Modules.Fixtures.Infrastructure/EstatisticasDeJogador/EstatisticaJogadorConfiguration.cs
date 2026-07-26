using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Oddify.Modules.Fixtures.Domain.EstatisticasDeJogador;
using Oddify.Modules.Fixtures.Domain.Jogadores;
using Oddify.Modules.Fixtures.Domain.Partidas;

namespace Oddify.Modules.Fixtures.Infrastructure.EstatisticasDeJogador;

internal sealed class EstatisticaJogadorConfiguration : IEntityTypeConfiguration<EstatisticaJogador>
{
    public void Configure(EntityTypeBuilder<EstatisticaJogador> builder)
    {
        builder.HasOne<Partida>().WithMany().HasForeignKey(e => e.PartidaId);
        builder.HasOne<Jogador>().WithMany().HasForeignKey(e => e.JogadorId);
    }
}
