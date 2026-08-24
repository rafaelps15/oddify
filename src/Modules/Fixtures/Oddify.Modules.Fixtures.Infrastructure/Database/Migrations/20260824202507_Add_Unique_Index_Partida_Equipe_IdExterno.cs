using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Oddify.Modules.Fixtures.Infrastructure.Database.Migrations;

/// <inheritdoc />
public partial class Add_Unique_Index_Partida_Equipe_IdExterno : Migration
{
    private static readonly string[] IndiceEquipeIdExternoLigaIdColumns = ["id_externo", "liga_id"];

    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateIndex(
            name: "ix_partidas_id_externo",
            schema: "fixtures",
            table: "partidas",
            column: "id_externo",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "ix_equipes_id_externo_liga_id",
            schema: "fixtures",
            table: "equipes",
            columns: IndiceEquipeIdExternoLigaIdColumns,
            unique: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "ix_partidas_id_externo",
            schema: "fixtures",
            table: "partidas");

        migrationBuilder.DropIndex(
            name: "ix_equipes_id_externo_liga_id",
            schema: "fixtures",
            table: "equipes");
    }
}
