using System;
using System.Collections.Generic;

namespace WebScraper.Scraping
{
   interface IDriverScraper
    {
        event EventHandler OnFailHandler;

        List<EconomicEvent> Scrape(DateTime date);
    }
}
