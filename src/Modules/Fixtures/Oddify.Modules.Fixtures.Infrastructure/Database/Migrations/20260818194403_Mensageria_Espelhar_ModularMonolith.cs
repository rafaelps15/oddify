using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Oddify.Modules.Fixtures.Infrastructure.Database.Migrations;

/// <inheritdoc />
public partial class Mensageria_Espelhar_ModularMonolith : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "error",
            schema: "fixtures",
            table: "outbox_messages");

        migrationBuilder.DropColumn(
            name: "failed_at_utc",
            schema: "fixtures",
            table: "outbox_messages");

        migrationBuilder.DropColumn(
            name: "retry_count",
            schema: "fixtures",
            table: "outbox_messages");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "error",
            schema: "fixtures",
            table: "outbox_messages",
            type: "text",
            nullable: true);

        migrationBuilder.AddColumn<DateTime>(
            name: "failed_at_utc",
            schema: "fixtures",
            table: "outbox_messages",
            type: "timestamp with time zone",
            nullable: true);

        migrationBuilder.AddColumn<int>(
            name: "retry_count",
            schema: "fixtures",
            table: "outbox_messages",
            type: "integer",
            nullable: false,
            defaultValue: 0);
    }
}
