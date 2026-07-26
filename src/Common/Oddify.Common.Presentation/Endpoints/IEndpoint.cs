using Microsoft.AspNetCore.Routing;

namespace Oddify.Common.Presentation.Endpoints;

public interface IEndpoint
{
    void MapEndpoint(IEndpointRouteBuilder app);
}
