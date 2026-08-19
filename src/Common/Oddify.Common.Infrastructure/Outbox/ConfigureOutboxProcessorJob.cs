using Microsoft.Extensions.Options;
using Oddify.Common.Application.Outbox;
using Quartz;

namespace Oddify.Common.Infrastructure.Outbox
{
    internal class ConfigureOutboxProcessorJob : IConfigureOptions<QuartzOptions>
    {
        private readonly string _schema;
        private readonly IOptions<OutboxProcessorOptions> _options;

        public ConfigureOutboxProcessorJob(string schema, IOptions<OutboxProcessorOptions> options)
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

            var jobKey = new JobKey($"OutboxProcessor.{_schema}");

            quartzOptions
                .AddJob<OutboxProcessorJob>(job => job
                    .WithIdentity(jobKey)
                    .UsingJobData("Schema", _schema))
                .AddTrigger(trigger => trigger
                    .ForJob(jobKey)
                    .WithSimpleSchedule(schedule => schedule.WithInterval(_options.Value.Interval).RepeatForever()));
        }
    }
}
