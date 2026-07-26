using Oddify.Common.Domain;
using Oddify.Common.Presentation.Endpoints;
using Oddify.Common.Presentation.Results;
using Oddify.Modules.Apostas.Application.Bancas.CriarBanca;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Oddify.Modules.Apostas.Presentation.Bancas;

internal sealed class CriarBanca : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("bancas", async (Request request, ISender sender) =>
        {
            Result<Guid> result = await sender.Send(new CriarBancaCommand(request.SaldoInicial, request.ModoPaperTrading));

            return result.Match(Results.Ok, ApiResults.Problem);
        })
        .WithTags(Tags.Bancas);
    }

    internal sealed class Request
    {
        public decimal SaldoInicial { get; init; }

        public bool ModoPaperTrading { get; init; }
    }
}
