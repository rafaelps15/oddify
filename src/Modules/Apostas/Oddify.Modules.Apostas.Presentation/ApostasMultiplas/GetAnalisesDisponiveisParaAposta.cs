using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Oddify.Common.Domain;
using Oddify.Common.Presentation.Endpoints;
using Oddify.Common.Presentation.Results;
using Oddify.Modules.Apostas.Application.ApostasMultiplas.GetAnalisesDisponiveisParaAposta;

namespace Oddify.Modules.Apostas.Presentation.ApostasMultiplas;

internal sealed class GetAnalisesDisponiveisParaAposta : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("apostas-multiplas/analises-disponiveis", async (ISender sender) =>
        {
            Result<IReadOnlyCollection<AnaliseDisponivelParaApostaResponse>> result =
                await sender.Send(new GetAnalisesDisponiveisParaApostaQuery());

            return result.Match(Results.Ok, ApiResults.Problem);
        })
        .WithTags(Tags.ApostasMultiplas)
        .RequireAuthorization();
    }
}
