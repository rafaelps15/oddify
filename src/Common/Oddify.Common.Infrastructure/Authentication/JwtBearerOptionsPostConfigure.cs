using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Oddify.Common.Infrastructure.Authentication;

// IPostConfigureOptions<T> roda depois de qualquer Configure<T>/named options já aplicado — é o
// jeito do próprio sistema de Options injetar dependência (aqui, o JwtOptions já validado) na
// configuração de um options type de uma biblioteca de terceiros (JwtBearerOptions) sem precisar
// capturar IServiceProvider dentro do lambda de AddJwtBearer(...), nem reler configuração crua.
internal sealed class JwtBearerOptionsPostConfigure(IOptionsMonitor<JwtOptions> jwtOptionsMonitor)
    : IPostConfigureOptions<JwtBearerOptions>
{
    public void PostConfigure(string? name, JwtBearerOptions options)
    {
        JwtOptions jwtOptions = jwtOptionsMonitor.CurrentValue;

        options.MapInboundClaims = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            IssuerSigningKey = jwtOptions.GetSecurityKey(),
            ValidIssuer = jwtOptions.Issuer,
            ValidAudience = jwtOptions.Audience,
            ClockSkew = TimeSpan.Zero
        };
    }
}
