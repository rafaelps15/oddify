using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Oddify.Common.Domain;
using Oddify.Common.Presentation.Endpoints;
using Oddify.Common.Presentation.Results;
using Oddify.Modules.Apostas.Application.ApostasMultiplas.AnularApostaMultipla;

namespace Oddify.Modules.Apostas.Presentation.ApostasMultiplas;

internal sealed class AnularApostaMultipla : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPut("apostas-multiplas/{id}/anular", async (Guid id, ISender sender) =>
        {
            Result result = await sender.Send(new AnularApostaMultiplaCommand(id));

            return result.Match(() => Results.Ok(), ApiResults.Problem);
        })
        .WithTags(Tags.ApostasMultiplas);
    }
}
