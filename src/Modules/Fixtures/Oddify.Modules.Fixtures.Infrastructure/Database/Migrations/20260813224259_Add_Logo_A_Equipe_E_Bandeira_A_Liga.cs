using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Oddify.Modules.Fixtures.Infrastructure.Database.Migrations;

/// <inheritdoc />
public partial class Add_Logo_A_Equipe_E_Bandeira_A_Liga : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "bandeira",
            schema: "fixtures",
            table: "ligas",
            type: "character varying(500)",
            maxLength: 500,
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "logo",
            schema: "fixtures",
            table: "equipes",
            type: "character varying(500)",
            maxLength: 500,
            nullable: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "bandeira",
            schema: "fixtures",
            table: "ligas");

        migrationBuilder.DropColumn(
            name: "logo",
            schema: "fixtures",
            table: "equipes");
    }
}
