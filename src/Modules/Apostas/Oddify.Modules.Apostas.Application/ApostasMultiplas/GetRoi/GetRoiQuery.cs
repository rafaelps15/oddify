using Oddify.Common.Application.Messaging;

namespace Oddify.Modules.Apostas.Application.ApostasMultiplas.GetRoi;

public sealed record GetRoiQuery(Guid? BancaId) : IQuery<RoiResponse>;
