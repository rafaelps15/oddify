using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1861 // Prefer 'static readonly' fields over constant array arguments

namespace Oddify.Modules.Apostas.Infrastructure.Database.Migrations;

/// <inheritdoc />
public partial class Add_InternalCommands : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "internal_commands",
            schema: "apostas",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                type = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                content = table.Column<string>(type: "jsonb", nullable: false),
                enqueued_on_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                processed_on_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_internal_commands", x => x.id);
            });

        migrationBuilder.CreateIndex(
            name: "idx_internal_commands_unprocessed",
            schema: "apostas",
            table: "internal_commands",
            columns: new[] { "enqueued_on_utc", "processed_on_utc" },
            filter: "processed_on_utc IS NULL")
            .Annotation("Npgsql:IndexInclude", new[] { "id", "type", "content" });
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "internal_commands",
            schema: "apostas");
    }
}
