using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Oddify.Modules.Users.Infrastructure.Database.Migrations;

/// <inheritdoc />
public partial class Create_Database : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.EnsureSchema(
            name: "users");

        migrationBuilder.CreateTable(
            name: "refresh_tokens",
            schema: "users",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                user_id = table.Column<Guid>(type: "uuid", nullable: false),
                token = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                expires_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_refresh_tokens", x => x.id);
            });

        migrationBuilder.CreateTable(
            name: "users",
            schema: "users",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                email = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                password_hash = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                first_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                last_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_users", x => x.id);
            });

        migrationBuilder.CreateIndex(
            name: "ix_refresh_tokens_token",
            schema: "users",
            table: "refresh_tokens",
            column: "token",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "ix_refresh_tokens_user_id",
            schema: "users",
            table: "refresh_tokens",
            column: "user_id");

        migrationBuilder.CreateIndex(
            name: "ix_users_email",
            schema: "users",
            table: "users",
            column: "email",
            unique: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "refresh_tokens",
            schema: "users");

        migrationBuilder.DropTable(
            name: "users",
            schema: "users");
    }
}
