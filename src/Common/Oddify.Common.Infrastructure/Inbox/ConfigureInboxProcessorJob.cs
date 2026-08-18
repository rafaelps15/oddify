using System.Reflection;
using Microsoft.Extensions.Options;
using Oddify.Common.Application.Outbox;
using Quartz;

namespace Oddify.Common.Infrastructure.Inbox;

// Espelho de ConfigureOutboxProcessorJob — um por módulo que consome integration event de outro
// módulo (ver AddInboxProcessor).
internal sealed class ConfigureInboxProcessorJob(string schema, Assembly presentationAssembly, IOptions<OutboxProcessorOptions> options)
    : IConfigureOptions<QuartzOptions>
{
    public void Configure(QuartzOptions quartzOptions)
    {
        if (!options.Value.Enabled)
        {
            return;
        }

        var jobKey = new JobKey($"InboxProcessor.{schema}");

        quartzOptions
            .AddJob<ProcessInboxJob>(job => job
                .WithIdentity(jobKey)
                .UsingJobData("Schema", schema)
                .UsingJobData("PresentationAssembly", presentationAssembly.FullName!))
            .AddTrigger(trigger => trigger
                .ForJob(jobKey)
                .WithSimpleSchedule(schedule => schedule.WithInterval(options.Value.Interval).RepeatForever()));
    }
}
