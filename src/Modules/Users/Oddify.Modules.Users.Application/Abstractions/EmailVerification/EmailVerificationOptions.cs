namespace Oddify.Modules.Users.Application.Abstractions.EmailVerification;

public sealed class EmailVerificationOptions
{
    // URL base do front-end — o link de verificação vira "{VerificationBaseUrl}?token={token}".
    public string VerificationBaseUrl { get; init; } = "http://localhost:4200/verificar-email";
}
