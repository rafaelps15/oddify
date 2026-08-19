namespace Oddify.Common.Application.Messaging
{
    public interface ICommandsScheduler
    {
        Task EnqueueAsync(ICommand command);
    }
}
