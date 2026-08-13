using Oddify.Common.Domain;

namespace Oddify.Modules.Apostas.Domain.Bancas;

public sealed class Banca : Entity
{
    private Banca()
    {
    }

    public Guid Id { get; private set; }

    public Guid UsuarioId { get; private set; }

    public string Nome { get; private set; }

    public decimal SaldoInicial { get; private set; }

    public decimal SaldoAtual { get; private set; }

    public decimal PercentualPorEntrada { get; private set; }

    public PerfilDeRisco PerfilDeRisco { get; private set; }

    public bool ModoPaperTrading { get; private set; }

    public bool Ativa { get; private set; }

    public FinalidadeDaBanca Finalidade { get; private set; }

    public DateTime CriadoEmUtc { get; private set; }

    public DateTime AtualizadoEmUtc { get; private set; }

    // Não persistido — derivado a cada leitura, nunca fica desatualizado em relação a SaldoAtual.
    public decimal ValorDaUnidade => SaldoAtual * PercentualPorEntrada;

    public static Banca Create(
        Guid usuarioId,
        string nome,
        decimal saldoInicial,
        decimal percentualPorEntrada,
        PerfilDeRisco perfilDeRisco,
        bool modoPaperTrading,
        FinalidadeDaBanca finalidade,
        DateTime criadoEmUtc)
    {
        return new Banca
        {
            Id = Guid.NewGuid(),
            UsuarioId = usuarioId,
            Nome = nome,
            SaldoInicial = saldoInicial,
            SaldoAtual = saldoInicial,
            PercentualPorEntrada = percentualPorEntrada,
            PerfilDeRisco = perfilDeRisco,
            ModoPaperTrading = modoPaperTrading,
            Ativa = true,
            Finalidade = finalidade,
            CriadoEmUtc = criadoEmUtc,
            AtualizadoEmUtc = criadoEmUtc
        };
    }

    public void AjustarSaldo(decimal delta, DateTime atualizadoEmUtc)
    {
        SaldoAtual += delta;
        AtualizadoEmUtc = atualizadoEmUtc;
    }
}
