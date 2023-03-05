using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using log4net;
using Quartz;
using WebScraper.Firestore;
using WebScraper.Scraping;

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
            return Task.Run(async () =>
            {
                EconomicEventsDB db = new EconomicEventsDB();
                List<EconomicEvent> todaysEvents = EconomicCalendarWebScraper.ScrapeToday();
                if (todaysEvents.Any())
                {
                    await db.AddEventsForDay(DateTime.Now, EconomicCalendarWebScraper.ScrapeToday());
                }
            });
        }
    }
}
