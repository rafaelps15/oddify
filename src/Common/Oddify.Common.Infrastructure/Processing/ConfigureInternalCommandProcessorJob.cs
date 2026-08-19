using Microsoft.Extensions.Options;
using Oddify.Common.Application.Outbox;
using Quartz;

namespace Oddify.Common.Infrastructure.Processing
{
    // Um IConfigureOptions<QuartzOptions> por módulo (ver AddCommandsProcessor) — cada instância
    // registra só o job+trigger do próprio schema. Reaproveita OutboxProcessorOptions (Enabled/Interval)
    // em vez de uma options própria — mesmo Enabled/Interval que já configura o polling da outbox
    // também controla o polling da fila de comandos internos; InboxProcessingExtensions já faz o mesmo
    // reaproveitamento pro Inbox.
    internal class ConfigureInternalCommandProcessorJob : IConfigureOptions<QuartzOptions>
    {
        private readonly string _schema;
        private readonly IOptions<OutboxProcessorOptions> _options;

        public ConfigureInternalCommandProcessorJob(string schema, IOptions<OutboxProcessorOptions> options)
        {
            _schema = schema;
            _options = options;
        }

        public void Configure(QuartzOptions quartzOptions)
        {
            if (!_options.Value.Enabled)
            {
                return;
            }

            var jobKey = new JobKey($"InternalCommandProcessor.{_schema}");

            quartzOptions
                .AddJob<InternalCommandProcessorJob>(job => job
                    .WithIdentity(jobKey)
                    .UsingJobData("Schema", _schema))
                .AddTrigger(trigger => trigger
                    .ForJob(jobKey)
                    .WithSimpleSchedule(schedule => schedule.WithInterval(_options.Value.Interval).RepeatForever()));
        }
    }
}
