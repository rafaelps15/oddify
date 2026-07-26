using Oddify.Common.Domain;
using MediatR;

namespace Oddify.Common.Application.Messaging;

public interface IQuery<TResponse> : IRequest<Result<TResponse>>;
