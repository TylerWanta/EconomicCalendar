using System;
using System.Collections.Generic;
using WebScraper.Types;

namespace WebScraper.Scraping
{
    class EconomicCalendarWebScraper
    {
        private IDriverScraper _driver;

        public EconomicCalendarWebScraper()
        {
            // looks like UndetectedChromeDriver no longer works. Defaulting to the fallback for now
            //_driver = new UndetectedChromeDriverScraper();
            _driver = new FireFoxDriverScraper();
            _driver.OnFailHandler += OnMainDriverFailed;
        }

        private void OnMainDriverFailed(object sender, OnDriverFailEventArgs e)
        {
            if (_driver.DriverFailedCounter >= 5)
            {
                throw new Exception("Max failed loads reached");
            }

            // send email or something?
            if (e != null && e.UseFallbackDriver)
            {
                _driver = new FireFoxDriverScraper();
            }
        }

        // returns true if the scrape was successful and the driver didn't fail
        public bool ScrapeToday(out List<EconomicEvent> todaysEvents)
        {
            todaysEvents = _driver.Scrape(DateTime.Now);
            return !_driver.DriverFailed;
        }

        // returns true if the scrape was successful and the driver didn't fail
        public bool ScrapeTomorrow(out List<EconomicEvent> tomorrowsEvens)
        {
            tomorrowsEvens = _driver.Scrape(DateTime.Now.AddDays(1));
            return !_driver.DriverFailed;
        }

        // returns true if the scrape was successful and the driver didn't fail
        public bool ScrapeDate(DateTime date, out List<EconomicEvent> datesEvents)
        {
            datesEvents = _driver.Scrape(date);
            return !_driver.DriverFailed;
        }
    }
}
