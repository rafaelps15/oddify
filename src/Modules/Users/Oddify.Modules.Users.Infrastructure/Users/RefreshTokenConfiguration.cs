using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Oddify.Modules.Users.Domain.Users;

namespace Oddify.Modules.Users.Infrastructure.Users;

internal sealed class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        builder.HasKey(rt => rt.Id);

        builder.Property(rt => rt.Token).HasMaxLength(200);

        builder.HasIndex(rt => rt.Token).IsUnique();

        builder.HasIndex(rt => rt.UserId);
    }
}
