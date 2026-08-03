using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Oddify.Modules.Users.Domain.Permissions;

namespace Oddify.Modules.Users.Infrastructure.Permissions;

internal sealed class PermissionConfiguration : IEntityTypeConfiguration<Permission>
{
    public void Configure(EntityTypeBuilder<Permission> builder)
    {
        builder.HasKey(p => p.Id);

        builder.Property(p => p.Name).HasMaxLength(100);

        builder.HasIndex(p => p.Name).IsUnique();

        builder.HasData(
            new { Id = WellKnownPermissions.UsersReadId, Name = WellKnownPermissions.UsersRead },
            new { Id = WellKnownPermissions.UsersUpdateId, Name = WellKnownPermissions.UsersUpdate },
            new { Id = WellKnownPermissions.UsersReadAllId, Name = WellKnownPermissions.UsersReadAll },
            new { Id = WellKnownPermissions.UsersManageRolesId, Name = WellKnownPermissions.UsersManageRoles });
    }
}
