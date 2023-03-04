using System;
using System.Diagnostics;
using System.Threading.Tasks;
using log4net;
using Quartz;

namespace WebScraper
{
    class ScrapeTodaysEventsJob : IJob
    {
        private ILog _log { get; }

        public ScrapeTodaysEventsJob(ILog log)
        {
            _log = log ?? throw new ArgumentNullException(nameof(log));
        } 

        public Task Execute(IJobExecutionContext context)
        {
            return Task.Run(() => _log.Info("in Job"));
        }
    }
}
