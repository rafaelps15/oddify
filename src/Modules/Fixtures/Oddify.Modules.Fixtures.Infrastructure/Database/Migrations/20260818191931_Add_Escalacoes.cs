using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Oddify.Modules.Fixtures.Infrastructure.Database.Migrations;

/// <inheritdoc />
public partial class Add_Escalacoes : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "escalacoes",
            schema: "fixtures",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                partida_id = table.Column<Guid>(type: "uuid", nullable: false),
                equipe_id = table.Column<Guid>(type: "uuid", nullable: false),
                formacao = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                tecnico = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_escalacoes", x => x.id);
                table.ForeignKey(
                    name: "fk_escalacoes_equipes_equipe_id",
                    column: x => x.equipe_id,
                    principalSchema: "fixtures",
                    principalTable: "equipes",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "fk_escalacoes_partidas_partida_id",
                    column: x => x.partida_id,
                    principalSchema: "fixtures",
                    principalTable: "partidas",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "escalacoes_de_jogador",
            schema: "fixtures",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                escalacao_id = table.Column<Guid>(type: "uuid", nullable: false),
                jogador_id = table.Column<Guid>(type: "uuid", nullable: false),
                titular = table.Column<bool>(type: "boolean", nullable: false),
                posicao = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                numero = table.Column<int>(type: "integer", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_escalacoes_de_jogador", x => x.id);
                table.ForeignKey(
                    name: "fk_escalacoes_de_jogador_escalacoes_escalacao_id",
                    column: x => x.escalacao_id,
                    principalSchema: "fixtures",
                    principalTable: "escalacoes",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "fk_escalacoes_de_jogador_jogadores_jogador_id",
                    column: x => x.jogador_id,
                    principalSchema: "fixtures",
                    principalTable: "jogadores",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "ix_escalacoes_equipe_id",
            schema: "fixtures",
            table: "escalacoes",
            column: "equipe_id");

        migrationBuilder.CreateIndex(
            name: "ix_escalacoes_partida_id",
            schema: "fixtures",
            table: "escalacoes",
            column: "partida_id");

        migrationBuilder.CreateIndex(
            name: "ix_escalacoes_de_jogador_escalacao_id",
            schema: "fixtures",
            table: "escalacoes_de_jogador",
            column: "escalacao_id");

        migrationBuilder.CreateIndex(
            name: "ix_escalacoes_de_jogador_jogador_id",
            schema: "fixtures",
            table: "escalacoes_de_jogador",
            column: "jogador_id");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "escalacoes_de_jogador",
            schema: "fixtures");

        migrationBuilder.DropTable(
            name: "escalacoes",
            schema: "fixtures");
    }
}
