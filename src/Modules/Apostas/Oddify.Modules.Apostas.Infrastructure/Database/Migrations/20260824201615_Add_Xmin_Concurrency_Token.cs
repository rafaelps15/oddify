using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Oddify.Modules.Apostas.Infrastructure.Database.Migrations;

/// <inheritdoc />
public partial class Add_Xmin_Concurrency_Token : Migration
{
    // xmin já existe fisicamente como coluna de sistema em toda tabela Postgres — não há schema
    // real para alterar aqui. Esta migration só atualiza o modelo do EF Core (via o snapshot) para
    // que Banca/ApostaMultipla/AnaliseDisponivelParaAposta passem a usar a shadow property "Version"
    // (uint, IsRowVersion) mapeada para xmin como token de concorrência otimista — ver
    // BancaConfiguration/ApostaMultiplaConfiguration/AnaliseDisponivelParaApostaConfiguration.
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // Nada a fazer — xmin já existe como coluna de sistema, ver comentário da classe.
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // Nada a fazer — xmin já existe como coluna de sistema, ver comentário da classe.
    }
}
