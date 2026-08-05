using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Oddify.Common.Domain;
using Oddify.Common.Presentation.Endpoints;
using Oddify.Common.Presentation.Results;
using Oddify.Modules.Apostas.Application.Bancas.GetMovimentacoesDaBanca;

namespace Oddify.Modules.Apostas.Presentation.Bancas;

internal sealed class GetMovimentacoesDaBanca : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("bancas/{id}/movimentacoes", async (Guid id, ISender sender, int page = 1, int pageSize = 15) =>
        {
            Result<GetMovimentacoesDaBancaResponse> result =
                await sender.Send(new GetMovimentacoesDaBancaQuery(id, page, pageSize));

            return result.Match(Results.Ok, ApiResults.Problem);
        })
        .WithTags(Tags.Bancas);
    }
}
