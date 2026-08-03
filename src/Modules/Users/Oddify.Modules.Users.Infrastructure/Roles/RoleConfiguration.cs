using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Oddify.Modules.Users.Domain.Roles;

namespace Oddify.Modules.Users.Infrastructure.Roles;

internal sealed class RoleConfiguration : IEntityTypeConfiguration<Role>
{
    public void Configure(EntityTypeBuilder<Role> builder)
    {
        builder.HasKey(r => r.Id);

        builder.Property(r => r.Name).HasMaxLength(100);

        builder.HasIndex(r => r.Name).IsUnique();

        builder.HasData(
            new { Id = WellKnownRoles.RegisteredId, Name = WellKnownRoles.Registered },
            new { Id = WellKnownRoles.OwnerId, Name = WellKnownRoles.Owner });
    }
}
