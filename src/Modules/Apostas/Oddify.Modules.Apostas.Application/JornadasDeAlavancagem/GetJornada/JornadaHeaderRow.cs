using Oddify.Modules.Apostas.Domain.JornadasDeAlavancagem;

namespace Oddify.Modules.Apostas.Application.JornadasDeAlavancagem.GetJornada;

// Ordem dos parâmetros precisa bater com a ordem das colunas do SELECT em
// GetJornadaQueryHandler — Dapper materializa record por posição do construtor, não por nome (ao
// contrário de classes com setter).
internal sealed record JornadaHeaderRow(
    Guid Id,
    FaixaDeMeta FaixaMeta,
    int PassoAtual,
    int TotalDePassos,
    int NumeroDeFracoes,
    decimal ValorInicial,
    decimal ValorObjetivo,
    StatusDaJornada Status,
    decimal ProbabilidadeDeConclusao,
    decimal ValorAtual);
