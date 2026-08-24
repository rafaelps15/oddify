using Oddify.Common.Domain;

namespace Oddify.Modules.Analise.Domain.Analises;

public sealed class AnaliseDePartida : Entity
{
    private AnaliseDePartida()
    {

    }

    public Guid Id { get; private set; }

    public Guid PartidaId { get; private set; }

    public string Mercado { get; private set; }

    public decimal ProbPoissonPura { get; private set; }

    public decimal ProbDixonColes { get; private set; }

    public decimal ProbImplicitaDaOdd { get; private set; }

    public decimal Vantagem { get; private set; }

    public decimal OddDeMercado { get; private set; }

    public bool AprovadaNoFiltro { get; private set; }

    public string? MotivoDoDescarte { get; private set; }

    public DecisaoDoClaude DecisaoDoClaude { get; private set; }

    public string? JustificativaDoClaude { get; private set; }

    public string? RespostaLlmBruta { get; private set; }

    public string? VersaoDoPrompt { get; private set; }

    public DateTime CriadaEmUtc { get; private set; }

    public static AnaliseDePartida Create(
        Guid partidaId,
        string mercado,
        decimal probPoissonPura,
        decimal probDixonColes,
        decimal probImplicitaDaOdd,
        decimal vantagem,
        decimal oddDeMercado,
        bool aprovadaNoFiltro,
        string? motivoDoDescarte,
        DateTime criadaEmUtc)
    {
        var analise = new AnaliseDePartida
        { 
            Id = Guid.NewGuid(),
            PartidaId = partidaId,
            Mercado = mercado,
            ProbPoissonPura = probPoissonPura,
            ProbDixonColes = probDixonColes,
            ProbImplicitaDaOdd = probImplicitaDaOdd,
            Vantagem = vantagem,
            OddDeMercado = oddDeMercado,
            AprovadaNoFiltro = aprovadaNoFiltro,
            MotivoDoDescarte = motivoDoDescarte,
            CriadaEmUtc = criadaEmUtc
        };

        analise.Raise(new AnaliseCriadaDomainEvent(analise.Id));

        return analise;
    }

    public Result RegistrarDecisaoDoClaude(DecisaoDoClaude decisao, string justificativa, string respostaBruta, string versaoDoPrompt)
    {
        if (!AprovadaNoFiltro)
        {
            return Result.Failure(AnaliseDePartidaErrors.NaoAprovadaNoFiltro(Id));
        }

        DecisaoDoClaude = decisao;
        JustificativaDoClaude = justificativa;
        RespostaLlmBruta = respostaBruta;
        VersaoDoPrompt = versaoDoPrompt;

        Raise(new AnaliseAvaliadaPeloClaudeDomainEvent(Id, decisao));

        return Result.Success();
    }

    // Upsert: chamado quando já existe uma análise para a mesma Partida+Mercado e uma cotação nova
    // chegou. Reseta a avaliação do Claude porque os números mudaram, então a decisão anterior
    // (Confirma/Reduz/Veta) não é mais válida para o novo cálculo. Atualiza CriadaEmUtc porque esta
    // linha não é histórico — é o estado atual da análise — e GetAnalisesAprovadasQueryHandler
    // ordena por este campo para refletir o que mudou recentemente.
    public void AtualizarCalculo(
        decimal probPoissonPura,
        decimal probDixonColes,
        decimal probImplicitaDaOdd,
        decimal vantagem,
        decimal oddDeMercado,
        bool aprovadaNoFiltro,
        string? motivoDoDescarte,
        DateTime atualizadaEmUtc)
    {
        ProbPoissonPura = probPoissonPura;
        ProbDixonColes = probDixonColes;
        ProbImplicitaDaOdd = probImplicitaDaOdd;
        Vantagem = vantagem;
        OddDeMercado = oddDeMercado;
        AprovadaNoFiltro = aprovadaNoFiltro;
        MotivoDoDescarte = motivoDoDescarte;
        CriadaEmUtc = atualizadaEmUtc;

        DecisaoDoClaude = DecisaoDoClaude.NaoAvaliada;
        JustificativaDoClaude = null;
        RespostaLlmBruta = null;
        VersaoDoPrompt = null;
    }
}
