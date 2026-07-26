namespace Oddify.Modules.Analise.Application.Analises.GetMedicao;

public sealed record MedicaoResponse(
    int AmostraTotal,
    decimal BrierPoissonPuro,
    decimal BrierDixonColes,
    int AmostraPosClaude,
    decimal? BrierPosClaude);
