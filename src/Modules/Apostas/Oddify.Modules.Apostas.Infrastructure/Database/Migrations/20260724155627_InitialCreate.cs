using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Oddify.Modules.Apostas.Infrastructure.Database.Migrations;

/// <inheritdoc />
public partial class InitialCreate : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.EnsureSchema(
            name: "apostas");

        migrationBuilder.CreateTable(
            name: "analises_disponiveis_para_aposta",
            schema: "apostas",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                partida_id = table.Column<Guid>(type: "uuid", nullable: false),
                mercado = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                odd_de_mercado = table.Column<decimal>(type: "numeric", nullable: false),
                probabilidade_confirmada = table.Column<decimal>(type: "numeric", nullable: false),
                reduzida = table.Column<bool>(type: "boolean", nullable: false),
                ja_utilizada = table.Column<bool>(type: "boolean", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_analises_disponiveis_para_aposta", x => x.id);
            });

        migrationBuilder.CreateTable(
            name: "bancas",
            schema: "apostas",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                saldo_atual = table.Column<decimal>(type: "numeric", nullable: false),
                modo_paper_trading = table.Column<bool>(type: "boolean", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_bancas", x => x.id);
            });

        migrationBuilder.CreateTable(
            name: "apostas_multiplas",
            schema: "apostas",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                banca_id = table.Column<Guid>(type: "uuid", nullable: false),
                odd_combinada = table.Column<decimal>(type: "numeric", nullable: false),
                stake = table.Column<decimal>(type: "numeric", nullable: false),
                resultado = table.Column<int>(type: "integer", nullable: false),
                lucro_ou_perda = table.Column<decimal>(type: "numeric", nullable: true),
                criada_em_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_apostas_multiplas", x => x.id);
                table.ForeignKey(
                    name: "fk_apostas_multiplas_bancas_banca_id",
                    column: x => x.banca_id,
                    principalSchema: "apostas",
                    principalTable: "bancas",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "pernas_de_aposta",
            schema: "apostas",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                aposta_multipla_id = table.Column<Guid>(type: "uuid", nullable: false),
                analise_id = table.Column<Guid>(type: "uuid", nullable: false),
                partida_id = table.Column<Guid>(type: "uuid", nullable: false),
                mercado = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                odd = table.Column<decimal>(type: "numeric", nullable: false),
                resultado = table.Column<int>(type: "integer", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_pernas_de_aposta", x => x.id);
                table.ForeignKey(
                    name: "fk_pernas_de_aposta_apostas_multiplas_aposta_multipla_id",
                    column: x => x.aposta_multipla_id,
                    principalSchema: "apostas",
                    principalTable: "apostas_multiplas",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "ix_apostas_multiplas_banca_id",
            schema: "apostas",
            table: "apostas_multiplas",
            column: "banca_id");

        migrationBuilder.CreateIndex(
            name: "ix_pernas_de_aposta_aposta_multipla_id",
            schema: "apostas",
            table: "pernas_de_aposta",
            column: "aposta_multipla_id");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "analises_disponiveis_para_aposta",
            schema: "apostas");

        migrationBuilder.DropTable(
            name: "pernas_de_aposta",
            schema: "apostas");

        migrationBuilder.DropTable(
            name: "apostas_multiplas",
            schema: "apostas");

        migrationBuilder.DropTable(
            name: "bancas",
            schema: "apostas");
    }
}
