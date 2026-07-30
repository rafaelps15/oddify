using Oddify.Modules.Analise.Application.Calculo;

namespace Oddify.Modules.Analise.Application.Abstractions.Analises;

public interface IAnaliseDePartidaDadosService
{
    Task<AnaliseCalculada?> ObterAsync(Guid partidaId, string mercado, CancellationToken cancellationToken);
}
