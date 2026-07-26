using Oddify.Common.Domain;

namespace Oddify.Common.Application.Exceptions;

public sealed class OddifyException : Exception
{
    public OddifyException(string requestName, Error? error = default, Exception? innerException = default)
        : base("Application exception", innerException)
    {
        RequestName = requestName;
        Error = error;
    }

    public string RequestName { get; }

    public Error? Error { get; }
}
