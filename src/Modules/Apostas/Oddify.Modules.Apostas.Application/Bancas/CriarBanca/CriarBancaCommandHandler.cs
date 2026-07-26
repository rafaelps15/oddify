using Oddify.Common.Application.Messaging;
using Oddify.Common.Domain;
using Oddify.Modules.Apostas.Application.Abstractions.Data;
using Oddify.Modules.Apostas.Domain.Bancas;

namespace Oddify.Modules.Apostas.Application.Bancas.CriarBanca;

internal sealed class CriarBancaCommandHandler(IBancaRepository bancaRepository, IUnitOfWork unitOfWork)
    : ICommandHandler<CriarBancaCommand, Guid>
{
    public async Task<Result<Guid>> Handle(CriarBancaCommand request, CancellationToken cancellationToken)
    {
        var banca = Banca.Create(request.SaldoInicial, request.ModoPaperTrading);

        bancaRepository.Insert(banca);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return banca.Id;
    }
}
