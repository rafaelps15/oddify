using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Oddify.Common.Domain;
using Oddify.Common.Presentation.Endpoints;
using Oddify.Common.Presentation.Results;
using Oddify.Modules.Apostas.Application.JornadasDeAlavancagem.GetOportunidadesParaAlavancagem;

namespace Oddify.Modules.Apostas.Presentation.JornadasDeAlavancagem;

internal sealed class GetOportunidadesParaAlavancagem : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("jornadas-de-alavancagem/oportunidades", async (ISender sender, int quantidade) =>
        {
            Result<IReadOnlyCollection<OportunidadeParaAlavancagemResponse>> result =
                await sender.Send(new GetOportunidadesParaAlavancagemQuery(quantidade));

            return result.Match(Results.Ok, ApiResults.Problem);
        })
        .WithTags(Tags.JornadasDeAlavancagem);
    }
}
