using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Oddify.Modules.Users.Domain.Permissions;
using Oddify.Modules.Users.Domain.Roles;

namespace Oddify.Modules.Users.Infrastructure.Roles;

internal sealed class RolePermissionConfiguration : IEntityTypeConfiguration<RolePermission>
{
    public void Configure(EntityTypeBuilder<RolePermission> builder)
    {
        builder.HasKey(rp => new { rp.RoleId, rp.PermissionId });

        builder.HasOne<Role>().WithMany().HasForeignKey(rp => rp.RoleId).OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<Permission>().WithMany().HasForeignKey(rp => rp.PermissionId).OnDelete(DeleteBehavior.Cascade);

        builder.HasData(
            new { RoleId = WellKnownRoles.RegisteredId, PermissionId = WellKnownPermissions.UsersReadId },
            new { RoleId = WellKnownRoles.RegisteredId, PermissionId = WellKnownPermissions.UsersUpdateId },
            new { RoleId = WellKnownRoles.OwnerId, PermissionId = WellKnownPermissions.UsersReadId },
            new { RoleId = WellKnownRoles.OwnerId, PermissionId = WellKnownPermissions.UsersUpdateId },
            new { RoleId = WellKnownRoles.OwnerId, PermissionId = WellKnownPermissions.UsersReadAllId },
            new { RoleId = WellKnownRoles.OwnerId, PermissionId = WellKnownPermissions.UsersManageRolesId });
    }
}
