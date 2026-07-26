using Microsoft.AspNetCore.Http;
using Oddify.Common.Application.Authentication;

namespace Oddify.Common.Infrastructure.Authentication;

internal sealed class UserContext(IHttpContextAccessor httpContextAccessor) : IUserContext
{
    public Guid UserId =>
        httpContextAccessor.HttpContext?.User.GetUserId() ??
        throw new InvalidOperationException("User context is unavailable");
}
