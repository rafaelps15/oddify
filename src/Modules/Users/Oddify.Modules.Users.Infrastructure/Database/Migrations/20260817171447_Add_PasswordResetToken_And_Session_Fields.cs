using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Oddify.Modules.Users.Infrastructure.Database.Migrations;

/// <inheritdoc />
public partial class Add_PasswordResetToken_And_Session_Fields : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<DateTime>(
            name: "created_at_utc",
            schema: "users",
            table: "refresh_tokens",
            type: "timestamp with time zone",
            nullable: false,
            defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

        migrationBuilder.AddColumn<DateTime>(
            name: "last_seen_at_utc",
            schema: "users",
            table: "refresh_tokens",
            type: "timestamp with time zone",
            nullable: false,
            defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

        migrationBuilder.AddColumn<string>(
            name: "user_agent",
            schema: "users",
            table: "refresh_tokens",
            type: "character varying(500)",
            maxLength: 500,
            nullable: true);

        migrationBuilder.CreateTable(
            name: "password_reset_tokens",
            schema: "users",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                user_id = table.Column<Guid>(type: "uuid", nullable: false),
                token_hash = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                expires_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                consumed_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_password_reset_tokens", x => x.id);
            });

        migrationBuilder.CreateIndex(
            name: "ix_password_reset_tokens_token_hash",
            schema: "users",
            table: "password_reset_tokens",
            column: "token_hash",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "ix_password_reset_tokens_user_id",
            schema: "users",
            table: "password_reset_tokens",
            column: "user_id");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "password_reset_tokens",
            schema: "users");

        migrationBuilder.DropColumn(
            name: "created_at_utc",
            schema: "users",
            table: "refresh_tokens");

        migrationBuilder.DropColumn(
            name: "last_seen_at_utc",
            schema: "users",
            table: "refresh_tokens");

        migrationBuilder.DropColumn(
            name: "user_agent",
            schema: "users",
            table: "refresh_tokens");
    }
}
