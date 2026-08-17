using Oddify.Modules.Apostas.Domain.JornadasDeAlavancagem;

namespace Oddify.Modules.Apostas.Application.JornadasDeAlavancagem.GetFaixasDeMeta;

public sealed record FaixaDeMetaResponse(FaixaDeMeta Faixa, int Multiplicador, int NumeroDeFracoes, int TotalDePassos)
{
    // Fora do construtor posicional — não vem do SELECT, é derivada por RegrasDeAlavancagem depois
    // do fetch (mesma convenção de Enriquecer em query-slice.md §B5).
    public decimal ProbabilidadeDeConclusao { get; private set; }

    public void DefinirProbabilidadeDeConclusao(decimal probabilidadeDeConclusao) =>
        ProbabilidadeDeConclusao = probabilidadeDeConclusao;
}
