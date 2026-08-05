using Oddify.Common.Application.Messaging;

namespace Oddify.Modules.Users.Application.Users.ResendVerification;

// Sem parâmetros de propósito — usa IUserContext.UserId no handler, nunca um id vindo do
// cliente, pra ninguém reenviar verificação de outra conta.
public sealed record ResendVerificationCommand : ICommand;
