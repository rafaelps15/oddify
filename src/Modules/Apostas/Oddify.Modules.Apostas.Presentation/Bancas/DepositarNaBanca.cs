using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Oddify.Common.Domain;
using Oddify.Common.Presentation.Endpoints;
using Oddify.Common.Presentation.Results;
using Oddify.Modules.Apostas.Application.Bancas.DepositarNaBanca;

namespace Oddify.Modules.Apostas.Presentation.Bancas;

internal sealed class DepositarNaBanca : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("bancas/{id}/depositos", async (Guid id, Request request, ISender sender) =>
        {
            Result result = await sender.Send(new DepositarNaBancaCommand(id, request.Valor));

            return result.Match(Results.NoContent, ApiResults.Problem);
        })
        .WithTags(Tags.Bancas);
    }

    internal sealed class Request
    {
        public decimal Valor { get; init; }
    }
}
