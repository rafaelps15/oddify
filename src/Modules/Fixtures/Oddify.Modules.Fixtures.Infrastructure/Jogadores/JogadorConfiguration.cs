using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Oddify.Modules.Fixtures.Domain.Equipes;
using Oddify.Modules.Fixtures.Domain.Jogadores;

namespace Oddify.Modules.Fixtures.Infrastructure.Jogadores;

internal sealed class JogadorConfiguration : IEntityTypeConfiguration<Jogador>
{
    public void Configure(EntityTypeBuilder<Jogador> builder)
    {
        builder.Property(j => j.IdExterno).HasMaxLength(100);
        builder.Property(j => j.Nome).HasMaxLength(200);
        builder.Property(j => j.Posicao).HasMaxLength(50);

        builder.HasOne<Equipe>().WithMany().HasForeignKey(j => j.EquipeId);
    }
}
