using System;
using Quartz;

namespace WebScraper
{
    class DailyWebScraperService
    {
        private IScheduler _scheduler { get; set; }

        public DailyWebScraperService(IScheduler scheduler)
        {
            _scheduler = scheduler ?? throw new ArgumentNullException(nameof(scheduler));
        }

        public void OnStart()
        {
            IJobDetail job = JobBuilder.Create<ScrapeTodaysEventsJob>()
                .WithIdentity(typeof(ScrapeTodaysEventsJob).Name, SchedulerConstants.DefaultGroup)
                .Build();

            ITrigger trigger = TriggerBuilder.Create()
                .WithIdentity("New day", SchedulerConstants.DefaultGroup)
                .ForJob(job)
                // load events in before midnight UTC +2 since that is what MQL4 uses 
                .WithSchedule(CronScheduleBuilder.DailyAtHourAndMinute(15, 0)) 
                .Build();

            _scheduler.ScheduleJob(job, trigger);
        }

        public void OnPause() => _scheduler.PauseAll();

        public void OnContinue() => _scheduler.ResumeAll();

        public void OnStop() => _scheduler.Shutdown();
    }
}
