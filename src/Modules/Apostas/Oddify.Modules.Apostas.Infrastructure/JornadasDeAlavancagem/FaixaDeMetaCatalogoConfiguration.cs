using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Oddify.Modules.Apostas.Domain.JornadasDeAlavancagem;

namespace Oddify.Modules.Apostas.Infrastructure.JornadasDeAlavancagem;

internal sealed class FaixaDeMetaCatalogoConfiguration : IEntityTypeConfiguration<FaixaDeMetaCatalogo>
{
    public void Configure(EntityTypeBuilder<FaixaDeMetaCatalogo> builder)
    {
        builder.HasKey(f => f.Faixa);

        builder.HasData(
            new { Faixa = FaixaDeMeta.Dobrar, Multiplicador = 2, NumeroDeFracoes = 3, TotalDePassos = 3 },
            new { Faixa = FaixaDeMeta.Triplicar, Multiplicador = 3, NumeroDeFracoes = 3, TotalDePassos = 5 },
            new { Faixa = FaixaDeMeta.CincoVezes, Multiplicador = 5, NumeroDeFracoes = 4, TotalDePassos = 8 });
    }
}
