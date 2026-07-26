using Oddify.Common.Domain;

namespace Oddify.Modules.Apostas.Domain.Bancas;

public sealed class Banca : Entity
{
    private Banca(Guid id, decimal saldoAtual, bool modoPaperTrading)
    {
        Id = id;
        SaldoAtual = saldoAtual;
        ModoPaperTrading = modoPaperTrading;
    }

    public Guid Id { get; private set; }

    public decimal SaldoAtual { get; private set; }

    public bool ModoPaperTrading { get; private set; }

    public static Banca Create(decimal saldoInicial, bool modoPaperTrading) =>
        new(Guid.NewGuid(), saldoInicial, modoPaperTrading);

    public void AjustarSaldo(decimal delta)
    {
        SaldoAtual += delta;
    }
}
