using MediatR;
using Microsoft.Extensions.Logging;
using Oddify.Common.Domain;
using Serilog.Context;

namespace Oddify.Common.Application.Behaviors;

internal sealed partial class RequestLoggingPipelineBehavior<TRequest, TResponse>(
    ILogger<RequestLoggingPipelineBehavior<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : class
    where TResponse : Result
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        string moduleName = GetModuleName(typeof(TRequest).FullName!);

        using (LogContext.PushProperty("Module", moduleName))
        {
            if (logger.IsEnabled(LogLevel.Information))
            {
                LogProcessingRequest(logger, typeof(TRequest).Name);
            }

            TResponse result = await next();

            if (result.IsSuccess)
            {
                if (logger.IsEnabled(LogLevel.Information))
                {
                    LogCompletedRequest(logger, typeof(TRequest).Name);
                }
            }
            else
            {
                using (LogContext.PushProperty("Error", result.Error, true))
                {
                    LogCompletedRequestWithError(logger, typeof(TRequest).Name);
                }
            }

            return result;
        }
    }

    private static string GetModuleName(string requestName) => requestName.Split('.')[2];

    [LoggerMessage(EventId = 1, Level = LogLevel.Information, Message = "Processing request {RequestName}")]
    private static partial void LogProcessingRequest(ILogger logger, string requestName);

    [LoggerMessage(EventId = 2, Level = LogLevel.Information, Message = "Completed request {RequestName}")]
    private static partial void LogCompletedRequest(ILogger logger, string requestName);

    [LoggerMessage(EventId = 3, Level = LogLevel.Error, Message = "Completed request {RequestName} with error")]
    private static partial void LogCompletedRequestWithError(ILogger logger, string requestName);
}
