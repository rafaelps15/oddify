using Oddify.Common.Application.Messaging;
using Oddify.Common.Domain;
using Oddify.Modules.Apostas.Application.Abstractions.Data;
using Oddify.Modules.Apostas.Domain.AnalisesDisponiveis;

namespace Oddify.Modules.Apostas.Application.ApostasMultiplas.RegistrarAnaliseDisponivelParaAposta;

internal sealed class RegistrarAnaliseDisponivelParaApostaCommandHandler(
    IAnaliseDisponivelParaApostaRepository repository,
    IUnitOfWork unitOfWork)
    : ICommandHandler<RegistrarAnaliseDisponivelParaApostaCommand>
{
    public async Task<Result> Handle(
        RegistrarAnaliseDisponivelParaApostaCommand request,
        CancellationToken cancellationToken)
    {
        var disponivel = AnaliseDisponivelParaAposta.Create(
            request.AnaliseId,
            request.PartidaId,
            request.Mercado,
            request.OddDeMercado,
            request.ProbabilidadeConfirmada,
            request.Reduzida);

        repository.Insert(disponivel);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
