using Oddify.Modules.Apostas.Domain.ApostasMultiplas;

namespace Oddify.Modules.Apostas.Application.Bancas.GetDesempenhoPorMercado;

// Shape intermediário compartilhado por GetDesempenhoPorCampeonatoQueryHandler e
// GetDesempenhoPorTimeQueryHandler — ambos precisam enriquecer a linha crua da aposta com
// dados de Fixtures (via IFixturesApi) antes de conseguir montar a chave de agrupamento, então a
// query não pode projetar direto na resposta final. Fonte única em vez de redeclarar em cada
// handler.
internal sealed record ApostaComPartidaRow(
    ResultadoDaAposta Resultado,
    decimal LucroOuPerda,
    decimal Stake,
    int QtdPernas,
    Guid PartidaId);
