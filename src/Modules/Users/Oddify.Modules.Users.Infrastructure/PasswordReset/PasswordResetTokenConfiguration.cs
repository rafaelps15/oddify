using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Oddify.Modules.Users.Domain.PasswordReset;

namespace Oddify.Modules.Users.Infrastructure.PasswordReset;

internal sealed class PasswordResetTokenConfiguration : IEntityTypeConfiguration<PasswordResetToken>
{
    public void Configure(EntityTypeBuilder<PasswordResetToken> builder)
    {
        builder.HasKey(t => t.Id);

        builder.Property(t => t.TokenHash).HasMaxLength(200);

        builder.HasIndex(t => t.TokenHash).IsUnique();

        builder.HasIndex(t => t.UserId);
    }
}
