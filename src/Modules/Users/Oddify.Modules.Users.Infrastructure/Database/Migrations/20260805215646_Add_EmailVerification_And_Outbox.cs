using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Oddify.Modules.Users.Infrastructure.Database.Migrations;

/// <inheritdoc />
public partial class Add_EmailVerification_And_Outbox : Migration
{
    private static readonly string[] OutboxUnprocessedIndexColumns = ["occurred_on_utc", "processed_on_utc"];
    private static readonly string[] OutboxUnprocessedIndexIncludedColumns = ["id", "type", "content"];

    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<DateTime>(
            name: "email_verified_at_utc",
            schema: "users",
            table: "users",
            type: "timestamp with time zone",
            nullable: true);

        migrationBuilder.AddColumn<bool>(
            name: "is_email_verified",
            schema: "users",
            table: "users",
            type: "boolean",
            nullable: false,
            defaultValue: false);

        migrationBuilder.CreateTable(
            name: "email_verification_tokens",
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
                table.PrimaryKey("pk_email_verification_tokens", x => x.id);
            });

        migrationBuilder.CreateTable(
            name: "outbox_messages",
            schema: "users",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                type = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                content = table.Column<string>(type: "jsonb", nullable: false),
                occurred_on_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                processed_on_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                error = table.Column<string>(type: "text", nullable: true),
                retry_count = table.Column<int>(type: "integer", nullable: false),
                failed_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_outbox_messages", x => x.id);
            });

        migrationBuilder.CreateIndex(
            name: "ix_email_verification_tokens_token_hash",
            schema: "users",
            table: "email_verification_tokens",
            column: "token_hash",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "ix_email_verification_tokens_user_id",
            schema: "users",
            table: "email_verification_tokens",
            column: "user_id");

        migrationBuilder.CreateIndex(
                name: "idx_outbox_messages_unprocessed",
                schema: "users",
                table: "outbox_messages",
                columns: OutboxUnprocessedIndexColumns,
                filter: "processed_on_utc IS NULL")
            .Annotation("Npgsql:IndexInclude", OutboxUnprocessedIndexIncludedColumns);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "email_verification_tokens",
            schema: "users");

        migrationBuilder.DropTable(
            name: "outbox_messages",
            schema: "users");

        migrationBuilder.DropColumn(
            name: "email_verified_at_utc",
            schema: "users",
            table: "users");

        migrationBuilder.DropColumn(
            name: "is_email_verified",
            schema: "users",
            table: "users");
    }
}
