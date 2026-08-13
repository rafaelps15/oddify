using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Oddify.Modules.Apostas.Infrastructure.Database.Migrations;

/// <inheritdoc />
public partial class Add_JornadaDeAlavancagem_E_Finalidade_Da_Banca : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<int>(
            name: "finalidade",
            schema: "apostas",
            table: "bancas",
            type: "integer",
            nullable: false,
            defaultValue: 0);

        migrationBuilder.AddColumn<Guid>(
            name: "passo_da_jornada_id",
            schema: "apostas",
            table: "apostas_multiplas",
            type: "uuid",
            nullable: true);

        migrationBuilder.CreateTable(
            name: "jornadas_de_alavancagem",
            schema: "apostas",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                usuario_id = table.Column<Guid>(type: "uuid", nullable: false),
                banca_id = table.Column<Guid>(type: "uuid", nullable: false),
                faixa_meta = table.Column<int>(type: "integer", nullable: false),
                valor_inicial = table.Column<decimal>(type: "numeric", nullable: false),
                valor_objetivo = table.Column<decimal>(type: "numeric", nullable: false),
                numero_de_fracoes = table.Column<int>(type: "integer", nullable: false),
                total_de_passos = table.Column<int>(type: "integer", nullable: false),
                passo_atual = table.Column<int>(type: "integer", nullable: false),
                status = table.Column<int>(type: "integer", nullable: false),
                probabilidade_de_conclusao = table.Column<decimal>(type: "numeric", nullable: false),
                criado_em_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                atualizado_em_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_jornadas_de_alavancagem", x => x.id);
                table.ForeignKey(
                    name: "fk_jornadas_de_alavancagem_bancas_banca_id",
                    column: x => x.banca_id,
                    principalSchema: "apostas",
                    principalTable: "bancas",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "passos_da_jornada",
            schema: "apostas",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                jornada_id = table.Column<Guid>(type: "uuid", nullable: false),
                numero = table.Column<int>(type: "integer", nullable: false),
                valor_do_passo = table.Column<decimal>(type: "numeric", nullable: false),
                numero_de_apostas = table.Column<int>(type: "integer", nullable: false),
                status = table.Column<int>(type: "integer", nullable: false),
                valor_resultante = table.Column<decimal>(type: "numeric", nullable: true),
                criado_em_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_passos_da_jornada", x => x.id);
                table.ForeignKey(
                    name: "fk_passos_da_jornada_jornadas_de_alavancagem_jornada_id",
                    column: x => x.jornada_id,
                    principalSchema: "apostas",
                    principalTable: "jornadas_de_alavancagem",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "ix_apostas_multiplas_passo_da_jornada_id",
            schema: "apostas",
            table: "apostas_multiplas",
            column: "passo_da_jornada_id");

        migrationBuilder.CreateIndex(
            name: "ix_jornadas_de_alavancagem_banca_id",
            schema: "apostas",
            table: "jornadas_de_alavancagem",
            column: "banca_id");

        migrationBuilder.CreateIndex(
            name: "ix_jornadas_de_alavancagem_usuario_id",
            schema: "apostas",
            table: "jornadas_de_alavancagem",
            column: "usuario_id");

        migrationBuilder.CreateIndex(
            name: "ix_passos_da_jornada_jornada_id",
            schema: "apostas",
            table: "passos_da_jornada",
            column: "jornada_id");

        migrationBuilder.AddForeignKey(
            name: "fk_apostas_multiplas_passos_da_jornada_passo_da_jornada_id",
            schema: "apostas",
            table: "apostas_multiplas",
            column: "passo_da_jornada_id",
            principalSchema: "apostas",
            principalTable: "passos_da_jornada",
            principalColumn: "id");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey(
            name: "fk_apostas_multiplas_passos_da_jornada_passo_da_jornada_id",
            schema: "apostas",
            table: "apostas_multiplas");

        migrationBuilder.DropTable(
            name: "passos_da_jornada",
            schema: "apostas");

        migrationBuilder.DropTable(
            name: "jornadas_de_alavancagem",
            schema: "apostas");

        migrationBuilder.DropIndex(
            name: "ix_apostas_multiplas_passo_da_jornada_id",
            schema: "apostas",
            table: "apostas_multiplas");

        migrationBuilder.DropColumn(
            name: "finalidade",
            schema: "apostas",
            table: "bancas");

        migrationBuilder.DropColumn(
            name: "passo_da_jornada_id",
            schema: "apostas",
            table: "apostas_multiplas");
    }
}
