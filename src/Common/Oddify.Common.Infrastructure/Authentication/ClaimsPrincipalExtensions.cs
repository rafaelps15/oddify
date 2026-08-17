using System.Security.Claims;
using Microsoft.IdentityModel.JsonWebTokens;

namespace Oddify.Common.Infrastructure.Authentication;

internal static class ClaimsPrincipalExtensions
{
    public static Guid GetUserId(this ClaimsPrincipal? principal)
    {
        // MapInboundClaims=false (JwtBearerOptionsPostConfigure) faz o ClaimsPrincipal carregar o
        // claim exatamente como veio no token ("sub", não o ClaimTypes.NameIdentifier de longa
        // duração) — precisa bater com o nome que TokenProvider.Create usa pra emitir.
        string? userId = principal?.FindFirstValue(JwtRegisteredClaimNames.Sub);

        return Guid.TryParse(userId, out Guid parsedUserId) ?
            parsedUserId :
            throw new ApplicationException("User id is unavailable");
    }
}
