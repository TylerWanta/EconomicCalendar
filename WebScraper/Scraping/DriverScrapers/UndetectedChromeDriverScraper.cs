using System;
using System.Collections.Generic;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.Extensions;
using Selenium.Extensions;
using Selenium.WebDriver.UndetectedChromeDriver;
using WebScraper.Scraping.DriverScrapers;

namespace WebScraper.Scraping
{
    class UndetectedChromeDriverScraper : BaseDriverScraper
    {
        SlDriver _driver;
        private bool _firstLoad;

        public UndetectedChromeDriverScraper() 
        {
            _driver = UndetectedChromeDriver.Instance();
            _firstLoad = true;
        }

        public override List<EconomicEvent> Scrape(DateTime date)
        {
            _driver.Navigate().GoToUrl(UrlForDate(date));
            CheckPageLoad();

            return BaseScrape(_driver, date);
        }

        private void CheckPageLoad()
        {
            if (_firstLoad)
            {
                SetCookies();
                _firstLoad = false;

                _driver.Navigate().Refresh();
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

                FailedToLoad();
            }
        }

        private void SetCookies()
        {
            // update time zone to UTC so I don't have to convert it
            _driver.ExecuteScript("document.cookie = \"fftimezone=America/Chicago; Max-Age=-1;\"");
            _driver.ExecuteScript("document.cookie = \"fftimezone=Etc%2FUTC\"");

            // update time format to 24 hour time so I don't have to do specific parsing
            _driver.ExecuteScript("document.cookie =\"fftimeformat=0; Max-Age=-1;\"");
            _driver.ExecuteScript("document.cookie = \"fftimeformat=1\"");
        }

        protected override (DateTime?, bool?) ScrapeTime(IWebElement eventElement, DateTime date)
        {
            throw new NotImplementedException();
        }
    }
}
