using Microsoft.Extensions.Options;
using Oddify.Common.Application.Outbox;
using Quartz;

namespace Oddify.Common.Infrastructure.Outbox;

// Um IConfigureOptions<QuartzOptions> por módulo (ver AddOutboxProcessor) — cada instância
// registra só o job+trigger do próprio schema. O Quartz consome cada IConfigureOptions<QuartzOptions>
// registrado ao montar QuartzOptions, então isso funciona registrado em qualquer ordem, de dentro
// do composition root de cada módulo, sem um array central coordenando quem existe.
internal sealed class ConfigureOutboxProcessorJob(string schema, IOptions<OutboxProcessorOptions> options)
    : IConfigureOptions<QuartzOptions>
{
    public void Configure(QuartzOptions quartzOptions)
    {
        if (!options.Value.Enabled)
        {
            return;
        }

        var jobKey = new JobKey($"OutboxProcessor.{schema}");

        quartzOptions
            .AddJob<OutboxProcessorJob>(job => job
                .WithIdentity(jobKey)
                .UsingJobData("Schema", schema))
            .AddTrigger(trigger => trigger
                .ForJob(jobKey)
                .WithSimpleSchedule(schedule => schedule.WithInterval(options.Value.Interval).RepeatForever()));
    }
}
