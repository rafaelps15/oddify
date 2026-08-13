using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Oddify.Common.Domain;
using Oddify.Common.Presentation.Endpoints;
using Oddify.Common.Presentation.Results;
using Oddify.Modules.Apostas.Application.ApostasMultiplas.EstornarLiquidacaoMultipla;

namespace Oddify.Modules.Apostas.Presentation.ApostasMultiplas;

internal sealed class EstornarLiquidacaoMultipla : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPut("apostas-multiplas/{id}/estornar", async (Guid id, ISender sender) =>
        {
            Result result = await sender.Send(new EstornarLiquidacaoMultiplaCommand(id));

            return result.Match(() => Results.Ok(), ApiResults.Problem);
        })
        .WithTags(Tags.ApostasMultiplas);
    }
}
