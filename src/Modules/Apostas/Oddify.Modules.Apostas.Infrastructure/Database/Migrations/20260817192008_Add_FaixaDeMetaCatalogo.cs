using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional
#pragma warning disable CA1861 // Prefer 'static readonly' fields over constant array arguments

namespace Oddify.Modules.Apostas.Infrastructure.Database.Migrations;

/// <inheritdoc />
public partial class Add_FaixaDeMetaCatalogo : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "faixas_de_meta_catalogo",
            schema: "apostas",
            columns: table => new
            {
                faixa = table.Column<int>(type: "integer", nullable: false),
                multiplicador = table.Column<int>(type: "integer", nullable: false),
                numero_de_fracoes = table.Column<int>(type: "integer", nullable: false),
                total_de_passos = table.Column<int>(type: "integer", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_faixas_de_meta_catalogo", x => x.faixa);
            });

        migrationBuilder.InsertData(
            schema: "apostas",
            table: "faixas_de_meta_catalogo",
            columns: new[] { "faixa", "multiplicador", "numero_de_fracoes", "total_de_passos" },
            values: new object[,]
            {
                { 0, 2, 3, 3 },
                { 1, 3, 3, 5 },
                { 2, 5, 4, 8 }
            });
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "faixas_de_meta_catalogo",
            schema: "apostas");
    }
}
