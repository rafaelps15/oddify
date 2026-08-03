using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional
#pragma warning disable CA1861 // Prefer 'static readonly' fields over constant array arguments

namespace Oddify.Modules.Users.Infrastructure.Database.Migrations;

/// <inheritdoc />
public partial class Add_Roles_And_Permissions : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "permissions",
            schema: "users",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_permissions", x => x.id);
            });

        migrationBuilder.CreateTable(
            name: "roles",
            schema: "users",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_roles", x => x.id);
            });

        migrationBuilder.CreateTable(
            name: "role_permissions",
            schema: "users",
            columns: table => new
            {
                role_id = table.Column<Guid>(type: "uuid", nullable: false),
                permission_id = table.Column<Guid>(type: "uuid", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_role_permissions", x => new { x.role_id, x.permission_id });
                table.ForeignKey(
                    name: "fk_role_permissions_permissions_permission_id",
                    column: x => x.permission_id,
                    principalSchema: "users",
                    principalTable: "permissions",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "fk_role_permissions_roles_role_id",
                    column: x => x.role_id,
                    principalSchema: "users",
                    principalTable: "roles",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "user_roles",
            schema: "users",
            columns: table => new
            {
                user_id = table.Column<Guid>(type: "uuid", nullable: false),
                role_id = table.Column<Guid>(type: "uuid", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_user_roles", x => new { x.user_id, x.role_id });
                table.ForeignKey(
                    name: "fk_user_roles_roles_role_id",
                    column: x => x.role_id,
                    principalSchema: "users",
                    principalTable: "roles",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "fk_user_roles_users_user_id",
                    column: x => x.user_id,
                    principalSchema: "users",
                    principalTable: "users",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.InsertData(
            schema: "users",
            table: "permissions",
            columns: new[] { "id", "name" },
            values: new object[,]
            {
                { new Guid("9a6f9b2a-2e2a-4b8b-9f0a-1a2b3c4d5f01"), "users:read" },
                { new Guid("9a6f9b2a-2e2a-4b8b-9f0a-1a2b3c4d5f02"), "users:update" },
                { new Guid("9a6f9b2a-2e2a-4b8b-9f0a-1a2b3c4d5f03"), "users:read-all" },
                { new Guid("9a6f9b2a-2e2a-4b8b-9f0a-1a2b3c4d5f04"), "users:manage-roles" }
            });

        migrationBuilder.InsertData(
            schema: "users",
            table: "roles",
            columns: new[] { "id", "name" },
            values: new object[,]
            {
                { new Guid("9a6f9b2a-1e1a-4b8b-9f0a-1a2b3c4d5e01"), "Registered" },
                { new Guid("9a6f9b2a-1e1a-4b8b-9f0a-1a2b3c4d5e02"), "Owner" }
            });

        migrationBuilder.InsertData(
            schema: "users",
            table: "role_permissions",
            columns: new[] { "permission_id", "role_id" },
            values: new object[,]
            {
                { new Guid("9a6f9b2a-2e2a-4b8b-9f0a-1a2b3c4d5f01"), new Guid("9a6f9b2a-1e1a-4b8b-9f0a-1a2b3c4d5e01") },
                { new Guid("9a6f9b2a-2e2a-4b8b-9f0a-1a2b3c4d5f02"), new Guid("9a6f9b2a-1e1a-4b8b-9f0a-1a2b3c4d5e01") },
                { new Guid("9a6f9b2a-2e2a-4b8b-9f0a-1a2b3c4d5f01"), new Guid("9a6f9b2a-1e1a-4b8b-9f0a-1a2b3c4d5e02") },
                { new Guid("9a6f9b2a-2e2a-4b8b-9f0a-1a2b3c4d5f02"), new Guid("9a6f9b2a-1e1a-4b8b-9f0a-1a2b3c4d5e02") },
                { new Guid("9a6f9b2a-2e2a-4b8b-9f0a-1a2b3c4d5f03"), new Guid("9a6f9b2a-1e1a-4b8b-9f0a-1a2b3c4d5e02") },
                { new Guid("9a6f9b2a-2e2a-4b8b-9f0a-1a2b3c4d5f04"), new Guid("9a6f9b2a-1e1a-4b8b-9f0a-1a2b3c4d5e02") }
            });

        migrationBuilder.CreateIndex(
            name: "ix_permissions_name",
            schema: "users",
            table: "permissions",
            column: "name",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "ix_role_permissions_permission_id",
            schema: "users",
            table: "role_permissions",
            column: "permission_id");

        migrationBuilder.CreateIndex(
            name: "ix_roles_name",
            schema: "users",
            table: "roles",
            column: "name",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "ix_user_roles_role_id",
            schema: "users",
            table: "user_roles",
            column: "role_id");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "role_permissions",
            schema: "users");

        migrationBuilder.DropTable(
            name: "user_roles",
            schema: "users");

        migrationBuilder.DropTable(
            name: "permissions",
            schema: "users");

        migrationBuilder.DropTable(
            name: "roles",
            schema: "users");
    }
}
