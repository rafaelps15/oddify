using Oddify.Modules.Apostas.Domain.ApostasMultiplas;

namespace Oddify.Modules.Apostas.Application.Calculo;

// Shape intermediário compartilhado por GetDesempenhoPorCampeonatoQueryHandler e
// GetDesempenhoPorTimeQueryHandler — ambos precisam enriquecer a linha crua da aposta com
// dados de Fixtures (via IFixturesApi) antes de conseguir montar a chave de agrupamento, então a
// query não pode projetar direto na resposta final. Fica em Calculo/ (não na pasta de nenhum dos
// dois handlers) porque é o tipo de entrada de DesempenhoCalculator, não pertence a um handler
// específico.
internal sealed record ApostaComPartidaRow(
    ResultadoDaAposta Resultado,
    decimal LucroOuPerda,
    decimal Stake,
    int QtdPernas,
    Guid PartidaId);
