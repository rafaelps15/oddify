using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Oddify.Modules.Apostas.Infrastructure.Database.Migrations;

/// <inheritdoc />
public partial class Add_Banca_Multitenancy_E_ValorDaUnidade : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<bool>(
            name: "ativa",
            schema: "apostas",
            table: "bancas",
            type: "boolean",
            nullable: false,
            defaultValue: false);

        migrationBuilder.AddColumn<DateTime>(
            name: "atualizado_em_utc",
            schema: "apostas",
            table: "bancas",
            type: "timestamp with time zone",
            nullable: false,
            defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

        migrationBuilder.AddColumn<DateTime>(
            name: "criado_em_utc",
            schema: "apostas",
            table: "bancas",
            type: "timestamp with time zone",
            nullable: false,
            defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

        migrationBuilder.AddColumn<string>(
            name: "nome",
            schema: "apostas",
            table: "bancas",
            type: "character varying(100)",
            maxLength: 100,
            nullable: false,
            defaultValue: "");

        migrationBuilder.AddColumn<decimal>(
            name: "percentual_por_entrada",
            schema: "apostas",
            table: "bancas",
            type: "numeric",
            nullable: false,
            defaultValue: 0m);

        migrationBuilder.AddColumn<decimal>(
            name: "saldo_inicial",
            schema: "apostas",
            table: "bancas",
            type: "numeric",
            nullable: false,
            defaultValue: 0m);

        migrationBuilder.AddColumn<Guid>(
            name: "usuario_id",
            schema: "apostas",
            table: "bancas",
            type: "uuid",
            nullable: false,
            defaultValue: Guid.Empty);

        migrationBuilder.CreateIndex(
            name: "ix_bancas_usuario_id",
            schema: "apostas",
            table: "bancas",
            column: "usuario_id");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "ix_bancas_usuario_id",
            schema: "apostas",
            table: "bancas");

        migrationBuilder.DropColumn(
            name: "ativa",
            schema: "apostas",
            table: "bancas");

        migrationBuilder.DropColumn(
            name: "atualizado_em_utc",
            schema: "apostas",
            table: "bancas");

        migrationBuilder.DropColumn(
            name: "criado_em_utc",
            schema: "apostas",
            table: "bancas");

        migrationBuilder.DropColumn(
            name: "nome",
            schema: "apostas",
            table: "bancas");

        migrationBuilder.DropColumn(
            name: "percentual_por_entrada",
            schema: "apostas",
            table: "bancas");

        migrationBuilder.DropColumn(
            name: "saldo_inicial",
            schema: "apostas",
            table: "bancas");

        migrationBuilder.DropColumn(
            name: "usuario_id",
            schema: "apostas",
            table: "bancas");
    }
}
