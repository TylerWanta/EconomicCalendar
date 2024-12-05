using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using log4net;
using Quartz;
using WebScraper.Calendars.Excel;
using WebScraper.Calendars.Firestore;
using WebScraper.Scraping;
using WebScraper.Types;

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
                List<EconomicEvent> tomorrowsEvents = new List<EconomicEvent>();
                EconomicCalendarWebScraper webScraper = new EconomicCalendarWebScraper();

                // keep trying if we fail. There is internal tracking to throw an exception so that this doesn't become an infinite loop
                // this will run at 3pm Central Time, which we need to do since some strategies start trading at 5 pm. 
                // We'll just have to scrape the next day instead of scraping our current day
                while (!webScraper.ScrapeTomorrow(out tomorrowsEvents))
                {
                    continue;
                }

                if (tomorrowsEvents.Any())
                {
                    FirestoreEconomicCalendar firestoreDB = new FirestoreEconomicCalendar();

                    await firestoreDB.AddEvents(tomorrowsEvents);

                    DateTime today = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, DateTime.UtcNow.Day, 0, 0, 0, DateTimeKind.Utc);
                    ExcelCalendars.AddEventsToCalendars(today.AddDays(1), tomorrowsEvents);
                }
            });
        }
    }
}
