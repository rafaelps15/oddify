using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Oddify.Modules.Apostas.Infrastructure.Database.Migrations;

/// <inheritdoc />
public partial class Add_MovimentacaoDaBanca : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<DateTime>(
            name: "atualizado_em_utc",
            schema: "apostas",
            table: "apostas_multiplas",
            type: "timestamp with time zone",
            nullable: false,
            defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

        migrationBuilder.AddColumn<string>(
            name: "descricao",
            schema: "apostas",
            table: "apostas_multiplas",
            type: "character varying(500)",
            maxLength: 500,
            nullable: true);

        migrationBuilder.AddColumn<int>(
            name: "origem",
            schema: "apostas",
            table: "apostas_multiplas",
            type: "integer",
            nullable: false,
            defaultValue: 0);

        migrationBuilder.AddColumn<decimal>(
            name: "retorno_potencial",
            schema: "apostas",
            table: "apostas_multiplas",
            type: "numeric",
            nullable: false,
            defaultValue: 0m);

        migrationBuilder.AddColumn<Guid>(
            name: "usuario_id",
            schema: "apostas",
            table: "apostas_multiplas",
            type: "uuid",
            nullable: false,
            defaultValue: Guid.Empty);

        migrationBuilder.CreateTable(
            name: "movimentacoes_da_banca",
            schema: "apostas",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                banca_id = table.Column<Guid>(type: "uuid", nullable: false),
                tipo = table.Column<int>(type: "integer", nullable: false),
                valor = table.Column<decimal>(type: "numeric", nullable: false),
                saldo_apos_movimentacao = table.Column<decimal>(type: "numeric", nullable: false),
                aposta_multipla_id = table.Column<Guid>(type: "uuid", nullable: true),
                criada_em_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_movimentacoes_da_banca", x => x.id);
                table.ForeignKey(
                    name: "fk_movimentacoes_da_banca_bancas_banca_id",
                    column: x => x.banca_id,
                    principalSchema: "apostas",
                    principalTable: "bancas",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "ix_apostas_multiplas_usuario_id",
            schema: "apostas",
            table: "apostas_multiplas",
            column: "usuario_id");

        migrationBuilder.CreateIndex(
            name: "ix_movimentacoes_da_banca_banca_id",
            schema: "apostas",
            table: "movimentacoes_da_banca",
            column: "banca_id");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "movimentacoes_da_banca",
            schema: "apostas");

        migrationBuilder.DropIndex(
            name: "ix_apostas_multiplas_usuario_id",
            schema: "apostas",
            table: "apostas_multiplas");

        migrationBuilder.DropColumn(
            name: "atualizado_em_utc",
            schema: "apostas",
            table: "apostas_multiplas");

        migrationBuilder.DropColumn(
            name: "descricao",
            schema: "apostas",
            table: "apostas_multiplas");

        migrationBuilder.DropColumn(
            name: "origem",
            schema: "apostas",
            table: "apostas_multiplas");

        migrationBuilder.DropColumn(
            name: "retorno_potencial",
            schema: "apostas",
            table: "apostas_multiplas");

        migrationBuilder.DropColumn(
            name: "usuario_id",
            schema: "apostas",
            table: "apostas_multiplas");
    }
}
