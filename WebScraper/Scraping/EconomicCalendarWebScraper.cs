using System;
using System.Collections.Generic;
using System.Globalization;
using OpenQA.Selenium;
using System.Linq;
using System.Diagnostics;
using Selenium.WebDriver.UndetectedChromeDriver;

namespace WebScraper.Scraping
{
    class EconomicCalendarWebScraper
    {
        private IDriverScraper _driver;

        private bool _driverFailed = false;
        private bool DriverFailed
        {
            get
            {
                // reset the value once returned so that we do accidently return true twice for the same failure
                bool tempDriverFailed = _driverFailed;
                _driverFailed = false;
                return tempDriverFailed;
            }
            set
            {
                _driverFailed = value;
            }
        }

        public EconomicCalendarWebScraper()
        {
            _driver = new UndetectedChromeDriverScraper();
            _driver.OnFailHandler += OnMainDriverFailed;
        }

        // TODO: Create event args with boolean so that I can check if the driver failed to load the page or if a different exception occured
        private void OnMainDriverFailed(object sender, EventArgs e)
        {
            // send email or something
            // need to make sure we can pick up where we left off. Probably should just re try scraping the date somehow
            _driver = new FireFoxDriverScraper();
            DriverFailed = true;
        }

        // returns true if the scrape was successful and the driver didn't fail
        public bool ScrapeToday(out List<EconomicEvent> todaysEvents)
        {
            todaysEvents = _driver.Scrape(DateTime.Now);
            return !DriverFailed;
        }

        // returns true if the scrape was successful and the driver didn't fail
        public bool ScrapeDate(DateTime date, out List<EconomicEvent> datesEvents)
        {
            datesEvents = _driver.Scrape(date);
            return !DriverFailed;
        }
    }
}
