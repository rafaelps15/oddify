using Oddify.Common.Application.Messaging;

namespace Oddify.Modules.Apostas.Application.ApostasMultiplas.MontarMultipla;

public sealed record MontarMultiplaCommand(Guid BancaId, IReadOnlyCollection<Guid> AnaliseIds) : ICommand<Guid>;
