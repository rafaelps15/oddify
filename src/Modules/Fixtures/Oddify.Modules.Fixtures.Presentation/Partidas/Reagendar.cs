using Oddify.Common.Domain;
using Oddify.Common.Presentation.Endpoints;
using Oddify.Common.Presentation.Results;
using Oddify.Modules.Fixtures.Application.Partidas.Reagendar;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Oddify.Modules.Fixtures.Presentation.Partidas;

internal sealed class Reagendar : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPut("partidas/{id}/reagendar", async (Guid id, Request request, ISender sender) =>
        {
            Result result = await sender.Send(new ReagendarCommand(id, request.NovaDataUtc));

            return result.Match(() => Results.Ok(), ApiResults.Problem);
        })
        .WithTags(Tags.Partidas);
    }

    internal sealed class Request
    {
        public DateTime NovaDataUtc { get; init; }
    }
}
