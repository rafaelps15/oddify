namespace Oddify.Modules.Users.Application.Abstractions.PasswordReset;

public sealed class PasswordResetOptions
{
    // URL base do front-end — o link de redefinição vira "{ResetBaseUrl}?token={token}".
    public string ResetBaseUrl { get; init; } = "http://localhost:4200/reset-password";
}
