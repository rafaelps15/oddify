using System.Reflection;
using Microsoft.Extensions.Options;
using Oddify.Common.Application.Outbox;
using Quartz;

namespace Oddify.Common.Infrastructure.Inbox
{
    internal class ConfigureInboxProcessorJob : IConfigureOptions<QuartzOptions>
    {
        private readonly string _schema;
        private readonly Assembly _presentationAssembly;
        private readonly IOptions<OutboxProcessorOptions> _options;

        public ConfigureInboxProcessorJob(string schema, Assembly presentationAssembly, IOptions<OutboxProcessorOptions> options)
        {
            _schema = schema;
            _presentationAssembly = presentationAssembly;
            _options = options;
        }

        public void Configure(QuartzOptions quartzOptions)
        {
            if (!_options.Value.Enabled)
            {
                return;
            }

            var jobKey = new JobKey($"InboxProcessor.{_schema}");

            quartzOptions
                .AddJob<ProcessInboxJob>(job => job
                    .WithIdentity(jobKey)
                    .UsingJobData("Schema", _schema)
                    .UsingJobData("PresentationAssembly", _presentationAssembly.FullName!))
                .AddTrigger(trigger => trigger
                    .ForJob(jobKey)
                    .WithSimpleSchedule(schedule => schedule.WithInterval(_options.Value.Interval).RepeatForever()));
        }
    }
}
