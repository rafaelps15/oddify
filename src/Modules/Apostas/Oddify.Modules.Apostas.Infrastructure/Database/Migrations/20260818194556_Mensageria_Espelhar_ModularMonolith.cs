using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Oddify.Modules.Apostas.Infrastructure.Database.Migrations;

/// <inheritdoc />
public partial class Mensageria_Espelhar_ModularMonolith : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "inbox_message_consumers",
            schema: "apostas");

        migrationBuilder.DropColumn(
            name: "error",
            schema: "apostas",
            table: "outbox_messages");

        migrationBuilder.DropColumn(
            name: "failed_at_utc",
            schema: "apostas",
            table: "outbox_messages");

        migrationBuilder.DropColumn(
            name: "retry_count",
            schema: "apostas",
            table: "outbox_messages");

        migrationBuilder.DropColumn(
            name: "error",
            schema: "apostas",
            table: "inbox_messages");

        migrationBuilder.DropColumn(
            name: "failed_at_utc",
            schema: "apostas",
            table: "inbox_messages");

        migrationBuilder.DropColumn(
            name: "retry_count",
            schema: "apostas",
            table: "inbox_messages");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "error",
            schema: "apostas",
            table: "outbox_messages",
            type: "text",
            nullable: true);

        migrationBuilder.AddColumn<DateTime>(
            name: "failed_at_utc",
            schema: "apostas",
            table: "outbox_messages",
            type: "timestamp with time zone",
            nullable: true);

        migrationBuilder.AddColumn<int>(
            name: "retry_count",
            schema: "apostas",
            table: "outbox_messages",
            type: "integer",
            nullable: false,
            defaultValue: 0);

        migrationBuilder.AddColumn<string>(
            name: "error",
            schema: "apostas",
            table: "inbox_messages",
            type: "text",
            nullable: true);

        migrationBuilder.AddColumn<DateTime>(
            name: "failed_at_utc",
            schema: "apostas",
            table: "inbox_messages",
            type: "timestamp with time zone",
            nullable: true);

        migrationBuilder.AddColumn<int>(
            name: "retry_count",
            schema: "apostas",
            table: "inbox_messages",
            type: "integer",
            nullable: false,
            defaultValue: 0);

        migrationBuilder.CreateTable(
            name: "inbox_message_consumers",
            schema: "apostas",
            columns: table => new
            {
                inbox_message_id = table.Column<Guid>(type: "uuid", nullable: false),
                name = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_inbox_message_consumers", x => new { x.inbox_message_id, x.name });
            });
    }
}
