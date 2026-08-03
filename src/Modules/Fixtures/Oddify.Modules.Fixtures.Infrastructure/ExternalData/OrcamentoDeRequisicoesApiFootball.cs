using StackExchange.Redis;

namespace Oddify.Modules.Fixtures.Infrastructure.ExternalData;

/// <summary>
/// Controla o orçamento diário de requisições contra a API-Football (100 req/dia no tier gratuito —
/// uma restrição de primeira classe que molda os jobs de ingestão). Usa um contador atômico no Redis
/// com expiração à meia-noite UTC, para que o job de sincronização nunca estoure a cota e derrube o
/// acesso ao provedor pelo resto do dia.
/// </summary>
internal sealed class OrcamentoDeRequisicoesApiFootball(IConnectionMultiplexer connectionMultiplexer)
{
    private const int LimiteDiario = 100;
    private const string PrefixoDaChave = "orcamento:api-football:";

    /// <summary>Registra o consumo de uma requisição e informa se ela ainda cabe dentro da cota diária.</summary>
    public async Task<bool> TentarConsumirAsync()
    {
        IDatabase database = connectionMultiplexer.GetDatabase();
        string chave = ChaveDoDia();

        long usoAtual = await database.StringIncrementAsync(chave);

        if (usoAtual == 1)
        {
            await database.KeyExpireAsync(chave, ProximaMeiaNoiteUtc() - DateTime.UtcNow);
        }

        return usoAtual <= LimiteDiario;
    }

    private static string ChaveDoDia() => $"{PrefixoDaChave}{DateTime.UtcNow:yyyy-MM-dd}";

    private static DateTime ProximaMeiaNoiteUtc() => DateTime.UtcNow.Date.AddDays(1);
}
