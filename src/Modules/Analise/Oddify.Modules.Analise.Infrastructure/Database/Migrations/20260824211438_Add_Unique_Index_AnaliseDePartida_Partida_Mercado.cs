using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Oddify.Modules.Analise.Infrastructure.Database.Migrations;

/// <inheritdoc />
public partial class Add_Unique_Index_AnaliseDePartida_Partida_Mercado : Migration
{
    private static readonly string[] IndiceAnaliseDePartidaPartidaIdMercadoColumns = ["partida_id", "mercado"];

    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateIndex(
            name: "ix_analises_de_partida_partida_id_mercado",
            schema: "analise",
            table: "analises_de_partida",
            columns: IndiceAnaliseDePartidaPartidaIdMercadoColumns,
            unique: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "ix_analises_de_partida_partida_id_mercado",
            schema: "analise",
            table: "analises_de_partida");
    }
}
