using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using OpenQA.Selenium;
using Selenium.Extensions;
using Selenium.WebDriver.UndetectedChromeDriver;
using WebScraper.Scraping.DriverScrapers;
using WebScraper.Types;

namespace WebScraper.Scraping
{
    // wrapper around UndectedChromeDriver. Used as main driver as it allows for more effecient scraping but could stop working
    // at any given time if cloudfare finds a way to block it; hence why we have a fallback driver, FireFoxDriverScraper.cs.
    // Also sends cookies to format the dates in 24 hour UTC format, making it easy to scrape and store them since firestore requires utc times
    class UndetectedChromeDriverScraper : BaseDriverScraper
    {
        private SlDriver _driver;

        private bool _firstLoad;

        private int _runningPageLoads;


        public UndetectedChromeDriverScraper() 
        {
            _driver = UndetectedChromeDriver.Instance();
            _firstLoad = true;
        }

        public override List<EconomicEvent> Scrape(DateTime date)
        {
            CheckRunningPageLoads();

            _driver.Navigate().GoToUrl(UrlForDate(date));

            CheckPageLoaded(date);

            return BaseScrape(_driver, date);
        }

        private void CheckRunningPageLoads()
        {
            if (_runningPageLoads + 1 < 50)
            {
                _runningPageLoads += 1;
                return;
            }

            // sleep every 50 page loads so that we don't overload their servers
            Thread.Sleep(5000);
            _runningPageLoads = 0;
        }

        private void CheckPageLoaded(DateTime date)
        {
            if (_firstLoad)
            {
                SetCookies();
                _firstLoad = false;

                _driver.Navigate().GoToUrl(UrlForDate(date));
            }

            try
            {
                var calendarTable = _driver.FindElement(By.XPath("//table[contains(@class, 'calendar__table'"));
            }
            catch
            {
                // this should only happen if cloudflare starts blocking this driver due to redirecting after our inital load
                // this will cuase EconomicCalendarWebScraper.cs to switch to a FireFoxDriverScraper which always works but isn't as 
                // effecient

                OnDriverFailed(new OnDriverFailEventArgs(true));
            }
        }

        private void SetCookies()
        {
            //update time zone to UTC so I don't have to convert it
            _driver.ExecuteScript("document.cookie = \"fftimezone=America/Chicago; Max-Age=-1;\"");
            _driver.ExecuteScript("document.cookie = \"fftimezone=Etc%2FUTC\"");

            // update time format to 24 hour time so I don't have to do specific parsing
            _driver.ExecuteScript("document.cookie =\"fftimeformat=0; Max-Age=-1;\"");
            _driver.ExecuteScript("document.cookie = \"fftimeformat=1\"");
        }

        // parses the scraped time. Time should be in the format "hh:mm" UTC
        protected override DateTime ParseScrapedTime(string scrapedTime, DateTime date)
        {
            string fullDateTimeFormat = DateFormat + "hh:mm";
            string fullDatetimeString = date.ToString(DateFormat) + scrapedTime;

            return DateTime.ParseExact(fullDatetimeString, fullDateTimeFormat, CultureInfo.InvariantCulture);
        }
    }
}
