using Microsoft.EntityFrameworkCore;
using Oddify.Modules.Users.Application.Abstractions.Data;
using Oddify.Modules.Users.Domain.EmailVerification;
using Oddify.Modules.Users.Domain.PasswordReset;
using Oddify.Modules.Users.Domain.Permissions;
using Oddify.Modules.Users.Domain.Roles;
using Oddify.Modules.Users.Domain.Users;
using Oddify.Modules.Users.Infrastructure.EmailVerification;
using Oddify.Modules.Users.Infrastructure.Outbox;
using Oddify.Modules.Users.Infrastructure.PasswordReset;
using Oddify.Modules.Users.Infrastructure.Permissions;
using Oddify.Modules.Users.Infrastructure.Roles;
using Oddify.Modules.Users.Infrastructure.Users;

namespace Oddify.Modules.Users.Infrastructure.Database;

public sealed class UsersDbContext(DbContextOptions<UsersDbContext> options) : DbContext(options), IUnitOfWork
{
    internal DbSet<User> Users { get; set; }

    internal DbSet<RefreshToken> RefreshTokens { get; set; }

    internal DbSet<Role> Roles { get; set; }

    internal DbSet<Permission> Permissions { get; set; }

    internal DbSet<RolePermission> RolePermissions { get; set; }

    internal DbSet<UserRole> UserRoles { get; set; }

    internal DbSet<EmailVerificationToken> EmailVerificationTokens { get; set; }

    internal DbSet<PasswordResetToken> PasswordResetTokens { get; set; }

    internal DbSet<OutboxMessage> OutboxMessages { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(Schemas.Users);

        modelBuilder.ApplyConfiguration(new UserConfiguration());
        modelBuilder.ApplyConfiguration(new RefreshTokenConfiguration());
        modelBuilder.ApplyConfiguration(new RoleConfiguration());
        modelBuilder.ApplyConfiguration(new PermissionConfiguration());
        modelBuilder.ApplyConfiguration(new RolePermissionConfiguration());
        modelBuilder.ApplyConfiguration(new UserRoleConfiguration());
        modelBuilder.ApplyConfiguration(new EmailVerificationTokenConfiguration());
        modelBuilder.ApplyConfiguration(new PasswordResetTokenConfiguration());
        modelBuilder.ApplyConfiguration(new OutboxMessageConfiguration());
    }
}
