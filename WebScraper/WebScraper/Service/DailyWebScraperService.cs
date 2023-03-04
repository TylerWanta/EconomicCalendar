using System;
using System.Diagnostics;
using log4net;
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

            ScrapingCalendar sc = new ScrapingCalendar();
            _scheduler.AddCalendar("scrapingCalendar", sc, false, false);

            ITrigger trigger = TriggerBuilder.Create()
                .WithIdentity("New Day", SchedulerConstants.DefaultGroup)
                .ForJob(job)
                .WithSchedule(CronScheduleBuilder.DailyAtHourAndMinute(0, 0))
                .ModifiedByCalendar("scrapingCalendar")
                .Build();

            _scheduler.ScheduleJob(job, trigger);
        }

        public void OnPause() => _scheduler.PauseAll();

        public void OnContinue() => _scheduler.ResumeAll();

        public void OnStop() => _scheduler.Shutdown();
    }
}
