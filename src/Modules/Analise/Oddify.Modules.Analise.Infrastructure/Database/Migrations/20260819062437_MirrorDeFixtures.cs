using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1861 // Prefer 'static readonly' fields over constant array arguments

namespace Oddify.Modules.Analise.Infrastructure.Database.Migrations;

/// <inheritdoc />
public partial class MirrorDeFixtures : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "cotacoes",
            schema: "analise",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                partida_id = table.Column<Guid>(type: "uuid", nullable: false),
                mercado = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                odd = table.Column<decimal>(type: "numeric", nullable: false),
                casa = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                coletada_em_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_cotacoes", x => x.id);
            });

        migrationBuilder.CreateTable(
            name: "inbox_messages",
            schema: "analise",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                type = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                content = table.Column<string>(type: "jsonb", nullable: false),
                occurred_on_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                processed_on_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_inbox_messages", x => x.id);
            });

        migrationBuilder.CreateTable(
            name: "ligas",
            schema: "analise",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                nome = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                media_de_gols = table.Column<decimal>(type: "numeric", nullable: false),
                fator_casa = table.Column<decimal>(type: "numeric", nullable: false),
                calibrada = table.Column<bool>(type: "boolean", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_ligas", x => x.id);
            });

        migrationBuilder.CreateTable(
            name: "partidas",
            schema: "analise",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                liga_id = table.Column<Guid>(type: "uuid", nullable: false),
                equipe_casa_id = table.Column<Guid>(type: "uuid", nullable: false),
                equipe_visitante_id = table.Column<Guid>(type: "uuid", nullable: false),
                data_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                gols_casa = table.Column<int>(type: "integer", nullable: true),
                gols_visitante = table.Column<int>(type: "integer", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_partidas", x => x.id);
            });

        migrationBuilder.CreateIndex(
            name: "ix_cotacoes_partida_id_mercado",
            schema: "analise",
            table: "cotacoes",
            columns: new[] { "partida_id", "mercado" });

        migrationBuilder.CreateIndex(
            name: "idx_inbox_messages_unprocessed",
            schema: "analise",
            table: "inbox_messages",
            columns: new[] { "occurred_on_utc", "processed_on_utc" },
            filter: "processed_on_utc IS NULL")
            .Annotation("Npgsql:IndexInclude", new[] { "id", "type", "content" });

        migrationBuilder.CreateIndex(
            name: "ix_partidas_equipe_casa_id",
            schema: "analise",
            table: "partidas",
            column: "equipe_casa_id");

        migrationBuilder.CreateIndex(
            name: "ix_partidas_equipe_visitante_id",
            schema: "analise",
            table: "partidas",
            column: "equipe_visitante_id");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "cotacoes",
            schema: "analise");

        migrationBuilder.DropTable(
            name: "inbox_messages",
            schema: "analise");

        migrationBuilder.DropTable(
            name: "ligas",
            schema: "analise");

        migrationBuilder.DropTable(
            name: "partidas",
            schema: "analise");
    }
}
