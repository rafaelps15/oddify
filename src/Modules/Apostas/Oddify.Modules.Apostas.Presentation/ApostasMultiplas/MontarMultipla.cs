using Oddify.Common.Domain;
using Oddify.Common.Presentation.Endpoints;
using Oddify.Common.Presentation.Results;
using Oddify.Modules.Apostas.Application.ApostasMultiplas.MontarMultipla;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Oddify.Modules.Apostas.Presentation.ApostasMultiplas;

internal sealed class MontarMultipla : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("apostas-multiplas/montar", async (Request request, ISender sender) =>
        {
            Result<Guid> result = await sender.Send(new MontarMultiplaCommand(request.BancaId, request.AnaliseIds));

            return result.Match(Results.Ok, ApiResults.Problem);
        })
        .WithTags(Tags.ApostasMultiplas);
    }

    internal sealed class Request
    {
        public Guid BancaId { get; init; }

        public IReadOnlyCollection<Guid> AnaliseIds { get; init; }
    }
}
