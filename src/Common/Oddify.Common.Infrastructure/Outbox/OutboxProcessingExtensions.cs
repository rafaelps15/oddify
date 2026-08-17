using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Oddify.Common.Application.Outbox;
using Quartz;

namespace Oddify.Common.Infrastructure.Outbox;

internal static class OutboxProcessingExtensions
{
    // Um job Quartz por módulo em outboxModules (JobKey/JobDataMap próprios com Schema +
    // MessageAssembly), todos compartilhando o mesmo OutboxProcessorJob e o mesmo
    // OutboxProcessorOptions — os módulos que usam outbox hoje não precisam de intervalos/lotes
    // diferentes entre si; se algum precisar um dia, isso vira uma seção de configuração por
    // módulo em vez de compartilhada.
    public static void AddOutboxProcessing(this IServiceCollection services, IConfiguration configuration, OutboxModule[] outboxModules)
    {
        if (outboxModules.Length == 0)
        {
            return;
        }

        services.Configure<OutboxProcessorOptions>(configuration.GetSection("OutboxProcessor"));
        OutboxProcessorOptions options = configuration.GetSection("OutboxProcessor").Get<OutboxProcessorOptions>() ?? new OutboxProcessorOptions();

        services.AddSingleton(outboxModules);
        services.AddHostedService<OutboxCleanupBackgroundService>();

        if (!options.Enabled)
        {
            return;
        }

        services.AddQuartz(quartz =>
        {
            foreach (OutboxModule module in outboxModules)
            {
                var jobKey = new JobKey($"OutboxProcessor.{module.Schema}");

                quartz.AddJob<OutboxProcessorJob>(job => job
                    .WithIdentity(jobKey)
                    .UsingJobData("Schema", module.Schema)
                    .UsingJobData("MessageAssembly", module.MessageAssembly.FullName!));

                quartz.AddTrigger(trigger => trigger
                    .ForJob(jobKey)
                    .WithSimpleSchedule(schedule => schedule.WithInterval(options.Interval).RepeatForever()));
            }
        });

        services.AddQuartzHostedService(quartz => quartz.WaitForJobsToComplete = true);
    }
}
