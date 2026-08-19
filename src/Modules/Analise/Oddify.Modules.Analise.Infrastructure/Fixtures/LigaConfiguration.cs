using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Oddify.Modules.Analise.Domain.Fixtures;

namespace Oddify.Modules.Analise.Infrastructure.Fixtures;

internal sealed class LigaConfiguration : IEntityTypeConfiguration<Liga>
{
    public void Configure(EntityTypeBuilder<Liga> builder)
    {
        builder.Property(l => l.Nome).HasMaxLength(200);
    }
}
