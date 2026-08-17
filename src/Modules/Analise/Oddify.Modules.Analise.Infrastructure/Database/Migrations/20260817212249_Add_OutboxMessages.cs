using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1861 // Prefer 'static readonly' fields over constant array arguments

namespace Oddify.Modules.Analise.Infrastructure.Database.Migrations;

/// <inheritdoc />
public partial class Add_OutboxMessages : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "outbox_messages",
            schema: "analise",
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
            name: "idx_outbox_messages_unprocessed",
            schema: "analise",
            table: "outbox_messages",
            columns: new[] { "occurred_on_utc", "processed_on_utc" },
            filter: "processed_on_utc IS NULL")
            .Annotation("Npgsql:IndexInclude", new[] { "id", "type", "content" });
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "outbox_messages",
            schema: "analise");
    }
}
