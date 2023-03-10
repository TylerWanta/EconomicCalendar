using System;
using System.Collections.Generic;
using WebScraper.Types;

namespace WebScraper.Scraping
{
   interface IDriverScraper
    {
        bool DriverFailed { get; }

        int DriverFailedCounter { get; }

        event EventHandler<OnDriverFailEventArgs> OnFailHandler;

        List<EconomicEvent> Scrape(DateTime date);
    }
}
