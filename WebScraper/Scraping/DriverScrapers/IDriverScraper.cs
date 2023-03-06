using System;
using System.Collections.Generic;
using WebScraper.Types;

namespace WebScraper.Scraping
{
   interface IDriverScraper
    {
        event EventHandler OnFailHandler;

        List<EconomicEvent> Scrape(DateTime date);
    }
}
