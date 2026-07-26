using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Oddify.Modules.Analise.Infrastructure.Database.Migrations;

/// <inheritdoc />
public partial class InitialCreate : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.EnsureSchema(
            name: "analise");

        migrationBuilder.CreateTable(
            name: "analises_de_partida",
            schema: "analise",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                partida_id = table.Column<Guid>(type: "uuid", nullable: false),
                mercado = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                prob_poisson_pura = table.Column<decimal>(type: "numeric", nullable: false),
                prob_dixon_coles = table.Column<decimal>(type: "numeric", nullable: false),
                prob_implicita_da_odd = table.Column<decimal>(type: "numeric", nullable: false),
                vantagem = table.Column<decimal>(type: "numeric", nullable: false),
                odd_de_mercado = table.Column<decimal>(type: "numeric", nullable: false),
                aprovada_no_filtro = table.Column<bool>(type: "boolean", nullable: false),
                motivo_do_descarte = table.Column<string>(type: "text", nullable: true),
                decisao_do_claude = table.Column<int>(type: "integer", nullable: false),
                justificativa_do_claude = table.Column<string>(type: "text", nullable: true),
                resposta_llm_bruta = table.Column<string>(type: "text", nullable: true),
                versao_do_prompt = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                criada_em_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_analises_de_partida", x => x.id);
            });
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "analises_de_partida",
            schema: "analise");
    }
}
