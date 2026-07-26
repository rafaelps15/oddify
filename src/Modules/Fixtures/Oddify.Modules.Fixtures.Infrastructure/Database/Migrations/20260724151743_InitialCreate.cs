using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Oddify.Modules.Fixtures.Infrastructure.Database.Migrations;
/// <inheritdoc />
public partial class InitialCreate : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.EnsureSchema(
            name: "fixtures");

        migrationBuilder.CreateTable(
            name: "ligas",
            schema: "fixtures",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                id_externo = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                nome = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                media_de_gols = table.Column<decimal>(type: "numeric", nullable: false),
                fator_casa = table.Column<decimal>(type: "numeric", nullable: false),
                calibrada = table.Column<bool>(type: "boolean", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_ligas", x => x.id);
            });

        migrationBuilder.CreateTable(
            name: "equipes",
            schema: "fixtures",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                id_externo = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                nome = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                liga_id = table.Column<Guid>(type: "uuid", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_equipes", x => x.id);
                table.ForeignKey(
                    name: "fk_equipes_ligas_liga_id",
                    column: x => x.liga_id,
                    principalSchema: "fixtures",
                    principalTable: "ligas",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "jogadores",
            schema: "fixtures",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                id_externo = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                equipe_id = table.Column<Guid>(type: "uuid", nullable: false),
                nome = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                posicao = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_jogadores", x => x.id);
                table.ForeignKey(
                    name: "fk_jogadores_equipes_equipe_id",
                    column: x => x.equipe_id,
                    principalSchema: "fixtures",
                    principalTable: "equipes",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "partidas",
            schema: "fixtures",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                id_externo = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                liga_id = table.Column<Guid>(type: "uuid", nullable: false),
                equipe_casa_id = table.Column<Guid>(type: "uuid", nullable: false),
                equipe_visitante_id = table.Column<Guid>(type: "uuid", nullable: false),
                data_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                situacao = table.Column<int>(type: "integer", nullable: false),
                gols_casa = table.Column<int>(type: "integer", nullable: true),
                gols_visitante = table.Column<int>(type: "integer", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_partidas", x => x.id);
                table.ForeignKey(
                    name: "fk_partidas_equipes_equipe_casa_id",
                    column: x => x.equipe_casa_id,
                    principalSchema: "fixtures",
                    principalTable: "equipes",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "fk_partidas_equipes_equipe_visitante_id",
                    column: x => x.equipe_visitante_id,
                    principalSchema: "fixtures",
                    principalTable: "equipes",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "fk_partidas_ligas_liga_id",
                    column: x => x.liga_id,
                    principalSchema: "fixtures",
                    principalTable: "ligas",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "cotacoes",
            schema: "fixtures",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                partida_id = table.Column<Guid>(type: "uuid", nullable: false),
                mercado = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                odd = table.Column<decimal>(type: "numeric", nullable: false),
                casa = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                coletada_em_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_cotacoes", x => x.id);
                table.ForeignKey(
                    name: "fk_cotacoes_partidas_partida_id",
                    column: x => x.partida_id,
                    principalSchema: "fixtures",
                    principalTable: "partidas",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "estatisticas_de_equipe",
            schema: "fixtures",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                partida_id = table.Column<Guid>(type: "uuid", nullable: false),
                equipe_id = table.Column<Guid>(type: "uuid", nullable: false),
                gols = table.Column<int>(type: "integer", nullable: false),
                finalizacoes = table.Column<int>(type: "integer", nullable: false),
                escanteios = table.Column<int>(type: "integer", nullable: false),
                posse = table.Column<decimal>(type: "numeric", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_estatisticas_de_equipe", x => x.id);
                table.ForeignKey(
                    name: "fk_estatisticas_de_equipe_equipes_equipe_id",
                    column: x => x.equipe_id,
                    principalSchema: "fixtures",
                    principalTable: "equipes",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "fk_estatisticas_de_equipe_partidas_partida_id",
                    column: x => x.partida_id,
                    principalSchema: "fixtures",
                    principalTable: "partidas",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "estatisticas_de_jogador",
            schema: "fixtures",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                partida_id = table.Column<Guid>(type: "uuid", nullable: false),
                jogador_id = table.Column<Guid>(type: "uuid", nullable: false),
                gols = table.Column<int>(type: "integer", nullable: false),
                assistencias = table.Column<int>(type: "integer", nullable: false),
                minutos = table.Column<int>(type: "integer", nullable: false),
                titular = table.Column<bool>(type: "boolean", nullable: false),
                nota = table.Column<decimal>(type: "numeric", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_estatisticas_de_jogador", x => x.id);
                table.ForeignKey(
                    name: "fk_estatisticas_de_jogador_jogadores_jogador_id",
                    column: x => x.jogador_id,
                    principalSchema: "fixtures",
                    principalTable: "jogadores",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "fk_estatisticas_de_jogador_partidas_partida_id",
                    column: x => x.partida_id,
                    principalSchema: "fixtures",
                    principalTable: "partidas",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "ix_cotacoes_partida_id",
            schema: "fixtures",
            table: "cotacoes",
            column: "partida_id");

        migrationBuilder.CreateIndex(
            name: "ix_equipes_liga_id",
            schema: "fixtures",
            table: "equipes",
            column: "liga_id");

        migrationBuilder.CreateIndex(
            name: "ix_estatisticas_de_equipe_equipe_id",
            schema: "fixtures",
            table: "estatisticas_de_equipe",
            column: "equipe_id");

        migrationBuilder.CreateIndex(
            name: "ix_estatisticas_de_equipe_partida_id",
            schema: "fixtures",
            table: "estatisticas_de_equipe",
            column: "partida_id");

        migrationBuilder.CreateIndex(
            name: "ix_estatisticas_de_jogador_jogador_id",
            schema: "fixtures",
            table: "estatisticas_de_jogador",
            column: "jogador_id");

        migrationBuilder.CreateIndex(
            name: "ix_estatisticas_de_jogador_partida_id",
            schema: "fixtures",
            table: "estatisticas_de_jogador",
            column: "partida_id");

        migrationBuilder.CreateIndex(
            name: "ix_jogadores_equipe_id",
            schema: "fixtures",
            table: "jogadores",
            column: "equipe_id");

        migrationBuilder.CreateIndex(
            name: "ix_ligas_id_externo",
            schema: "fixtures",
            table: "ligas",
            column: "id_externo",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "ix_partidas_equipe_casa_id",
            schema: "fixtures",
            table: "partidas",
            column: "equipe_casa_id");

        migrationBuilder.CreateIndex(
            name: "ix_partidas_equipe_visitante_id",
            schema: "fixtures",
            table: "partidas",
            column: "equipe_visitante_id");

        migrationBuilder.CreateIndex(
            name: "ix_partidas_liga_id",
            schema: "fixtures",
            table: "partidas",
            column: "liga_id");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "cotacoes",
            schema: "fixtures");

        migrationBuilder.DropTable(
            name: "estatisticas_de_equipe",
            schema: "fixtures");

        migrationBuilder.DropTable(
            name: "estatisticas_de_jogador",
            schema: "fixtures");

        migrationBuilder.DropTable(
            name: "jogadores",
            schema: "fixtures");

        migrationBuilder.DropTable(
            name: "partidas",
            schema: "fixtures");

        migrationBuilder.DropTable(
            name: "equipes",
            schema: "fixtures");

        migrationBuilder.DropTable(
            name: "ligas",
            schema: "fixtures");
    }
}
