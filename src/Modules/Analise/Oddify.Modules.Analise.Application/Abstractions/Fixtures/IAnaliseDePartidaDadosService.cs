using Oddify.Modules.Analise.Application.Calculo;

namespace Oddify.Modules.Analise.Application.Abstractions.Fixtures;

internal interface IAnaliseDePartidaDadosService
{
    Task<AnaliseCalculada?> ObterAsync(Guid partidaId, string mercado, CancellationToken cancellationToken = default);
}
