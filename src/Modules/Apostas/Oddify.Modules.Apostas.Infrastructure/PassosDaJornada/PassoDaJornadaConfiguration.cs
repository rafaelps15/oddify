using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Oddify.Modules.Apostas.Domain.JornadasDeAlavancagem;
using Oddify.Modules.Apostas.Domain.PassosDaJornada;

namespace Oddify.Modules.Apostas.Infrastructure.PassosDaJornada;

internal sealed class PassoDaJornadaConfiguration : IEntityTypeConfiguration<PassoDaJornada>
{
    public void Configure(EntityTypeBuilder<PassoDaJornada> builder)
    {
        builder.HasOne<JornadaDeAlavancagem>().WithMany().HasForeignKey(p => p.JornadaId);

        builder.HasIndex(p => p.JornadaId);
    }
}
