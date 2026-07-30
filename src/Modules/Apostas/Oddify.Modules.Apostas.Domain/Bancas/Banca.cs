using Oddify.Common.Domain;

namespace Oddify.Modules.Apostas.Domain.Bancas;

public sealed class Banca : Entity
{
    private Banca()
    {
    }

    public Guid Id { get; private set; }

    public decimal SaldoAtual { get; private set; }

    public bool ModoPaperTrading { get; private set; }

    public static Banca Create(decimal saldoInicial, bool modoPaperTrading)
    {
        return new Banca
        {
            Id = Guid.NewGuid(),
            SaldoAtual = saldoInicial,
            ModoPaperTrading = modoPaperTrading
        };
    }

    public void AjustarSaldo(decimal delta)
    {
        SaldoAtual += delta;
    }
}
