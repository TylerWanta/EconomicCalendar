﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using log4net;
using Quartz;
using WebScraper.Firestore;
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
                List<EconomicEvent> todaysEvents = new List<EconomicEvent>();
                EconomicCalendarWebScraper webScraper = new EconomicCalendarWebScraper();

                if (!webScraper.ScrapeToday(out todaysEvents))
                {
                    // retry with fallback driver
                    if (!webScraper.ScrapeToday(out todaysEvents))
                    {
                        return;
                    }
                }

                EconomicEventsDB db = new EconomicEventsDB();

                if (todaysEvents.Any())
                {
                    await db.AddEvents(todaysEvents);
                }
            });
        }
    }
}
