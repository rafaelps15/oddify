namespace Oddify.Modules.Apostas.Domain.Bancas;

// Distingue a banca principal do usuário (gestão de banca normal) de uma banca de alavancagem
// (módulo separado, ver JornadaDeAlavancagem) — isoladas uma da outra: IniciarJornadaCommand cria
// sua própria Banca com Finalidade=Alavancagem em vez de reaproveitar a banca principal do
// usuário, então o saldo de uma jornada nunca se mistura com o saldo da gestão de banca normal.
public enum FinalidadeDaBanca
{
    Principal = 0,
    Alavancagem = 1
}
