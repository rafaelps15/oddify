using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Oddify.Modules.Fixtures.Infrastructure.Database.Migrations;

/// <inheritdoc />
public partial class AddRodadaETemporadaAPartida : Migration
{
    private static readonly string[] IndiceLigaTemporadaRodadaColumns = ["liga_id", "temporada", "rodada"];

    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "ix_partidas_liga_id",
            schema: "fixtures",
            table: "partidas");

        migrationBuilder.AddColumn<int>(
            name: "rodada",
            schema: "fixtures",
            table: "partidas",
            type: "integer",
            nullable: false,
            defaultValue: 0);

        migrationBuilder.AddColumn<int>(
            name: "temporada",
            schema: "fixtures",
            table: "partidas",
            type: "integer",
            nullable: false,
            defaultValue: 0);

        migrationBuilder.CreateIndex(
            name: "ix_partidas_liga_id_temporada_rodada",
            schema: "fixtures",
            table: "partidas",
            columns: IndiceLigaTemporadaRodadaColumns);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "ix_partidas_liga_id_temporada_rodada",
            schema: "fixtures",
            table: "partidas");

        migrationBuilder.DropColumn(
            name: "rodada",
            schema: "fixtures",
            table: "partidas");

        migrationBuilder.DropColumn(
            name: "temporada",
            schema: "fixtures",
            table: "partidas");

        migrationBuilder.CreateIndex(
            name: "ix_partidas_liga_id",
            schema: "fixtures",
            table: "partidas",
            column: "liga_id");
    }
}
