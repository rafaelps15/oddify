using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using Oddify.Common.Application.Authentication;

namespace Oddify.Common.Infrastructure.Authentication;

internal sealed class TokenProvider(IOptions<JwtOptions> options) : ITokenProvider
{
    private JwtOptions Options => options.Value;

    public string Create(Guid userId, string email)
    {
        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(Options.Secret));
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(
            [
                new Claim(JwtRegisteredClaimNames.Sub, userId.ToString()),
                new Claim(JwtRegisteredClaimNames.Email, email)
            ]),
            Expires = DateTime.UtcNow.AddMinutes(Options.ExpirationInMinutes),
            SigningCredentials = credentials,
            Issuer = Options.Issuer,
            Audience = Options.Audience
        };

        var handler = new JsonWebTokenHandler();

        return handler.CreateToken(tokenDescriptor);
    }

    public string GenerateRefreshToken()
    {
        byte[] randomBytes = RandomNumberGenerator.GetBytes(32);

        return Convert.ToBase64String(randomBytes);
    }
}
