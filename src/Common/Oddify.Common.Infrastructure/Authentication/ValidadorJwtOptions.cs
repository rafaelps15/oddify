using Microsoft.Extensions.Options;

namespace Oddify.Common.Infrastructure.Authentication;

// Vazia de propósito — o gerador de código (Microsoft.Extensions.Options.SourceGeneration, ativo
// por padrão a partir do Microsoft.Extensions.Options 8+) lê os atributos de DataAnnotations em
// JwtOptions e emite a implementação de Validate(...) em Validators.g.cs, sem reflexão em
// runtime e compatível com AOT. Por isso ValidateDataAnnotations() não é usado no registro do
// OptionsBuilder — validaria as mesmas regras duas vezes, uma via reflexão.
[OptionsValidator]
internal sealed partial class ValidadorJwtOptions : IValidateOptions<JwtOptions>
{
}
