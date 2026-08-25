using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Oddify.Modules.Analise.Domain.Analises;

namespace Oddify.Modules.Analise.Infrastructure.Analises;

internal sealed class AnaliseDePartidaConfiguration : IEntityTypeConfiguration<AnaliseDePartida>
{
    public void Configure(EntityTypeBuilder<AnaliseDePartida> builder)
    {
        builder.Property(a => a.Mercado).HasMaxLength(100);
        builder.Property(a => a.VersaoDoPrompt).HasMaxLength(100);

        // O upsert de AnalisarPartidaCommandHandler é check-then-insert (GetPorPartidaEMercadoAsync
        // seguido de Insert), sem lock — mesma corrida corrigida em PartidaConfiguration/
        // EquipeConfiguration. Sem este índice, duas execuções concorrentes para a mesma
        // Partida+Mercado podiam duplicar a linha; SingleOrDefaultAsync então lançaria
        // InvalidOperationException não tratada na próxima leitura, reintroduzindo o poison-message
        // que o tratamento de mercado desconhecido eliminou no consumer.
        builder.HasIndex(a => new { a.PartidaId, a.Mercado }).IsUnique();
    }
}
