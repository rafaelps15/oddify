using MediatR;
using Oddify.Common.Domain;

namespace Oddify.Common.Application.Messaging;

public interface IQuery<TResponse> : IRequest<Result<TResponse>>;
