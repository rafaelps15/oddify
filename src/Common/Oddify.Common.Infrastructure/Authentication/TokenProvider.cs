using System.Security.Claims;
using System.Security.Cryptography;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using Oddify.Common.Application.Authentication;

namespace Oddify.Common.Infrastructure.Authentication;

internal sealed class TokenProvider(IOptionsMonitor<JwtOptions> optionsMonitor) : ITokenProvider
{
    public string Create(Guid userId, string email)
    {
        JwtOptions options = optionsMonitor.CurrentValue;

        var credentials = new SigningCredentials(options.GetSecurityKey(), SecurityAlgorithms.HmacSha256);

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(
            [
                new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
                new Claim(JwtRegisteredClaimNames.Email, email)
            ]),
            Expires = DateTime.UtcNow.AddMinutes(options.ExpirationInMinutes),
            SigningCredentials = credentials,
            Issuer = options.Issuer,
            Audience = options.Audience
        };

        var handler = new JsonWebTokenHandler();

        string token = handler.CreateToken(tokenDescriptor);

        return token;
    }

    public string GenerateRefreshToken()
    {
        byte[] randomBytes = RandomNumberGenerator.GetBytes(32);

        return Convert.ToBase64String(randomBytes);
    }
}
