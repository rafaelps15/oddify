using Microsoft.Extensions.Options;
using Oddify.Common.Application.Outbox;
using Quartz;

namespace Oddify.Common.Infrastructure.Processing
{
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
