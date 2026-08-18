using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Oddify.Modules.Fixtures.Domain.Escalacoes;
using Oddify.Modules.Fixtures.Domain.EscalacoesDeJogador;
using Oddify.Modules.Fixtures.Domain.Jogadores;

namespace Oddify.Modules.Fixtures.Infrastructure.EscalacoesDeJogador;

internal sealed class EscalacaoJogadorConfiguration : IEntityTypeConfiguration<EscalacaoJogador>
{
    public void Configure(EntityTypeBuilder<EscalacaoJogador> builder)
    {
        builder.HasOne<Escalacao>().WithMany().HasForeignKey(j => j.EscalacaoId);
        builder.HasOne<Jogador>().WithMany().HasForeignKey(j => j.JogadorId);

        builder.Property(j => j.Posicao).HasMaxLength(20);
    }
}
