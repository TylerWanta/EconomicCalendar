using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using OpenQA.Selenium;
using OpenQA.Selenium.Firefox;
using WebScraper.Scraping.DriverScrapers;
using WebScraper.Types;

namespace WebScraper.Scraping
{
    // Wrapper around FireFoxDriver. Used as a fallback driver since we open a new driver instance for each request, which can't be blocked by
    // cloudflare. Very inefficent though. 
    class FireFoxDriverScraper : BaseDriverScraper
    {
        public FireFoxDriverScraper()
        {
        }

        public override List<EconomicEvent> Scrape(DateTime date)
        {
            // this will spin up a new driver for each day that we want to scrape. This way cloudfare can't block any requests.
            // super inefficent and is why we only use it as a backup
            using (FirefoxDriver driver = new FirefoxDriver())
            {
                driver.Navigate().GoToUrl(UrlForDate(date));
                return BaseScrape(driver, date);
            }
        }

        // parses the scraped time. Time should be in the foramt " hh?:mmtt" UTC-5
        override protected DateTime ParseScrapedTime(string scrapedTime, DateTime date)
        {
            // times have a leading space, remove it 
            scrapedTime = new string(scrapedTime.ToCharArray().Where(c => !Char.IsWhiteSpace(c)).ToArray());
            string fullDatetimeString = date.ToString(DateFormat) + scrapedTime;

            // the string will only have one number before the ':' if its below 10, need to make sure our 'h' format matches it
            string fullDateTimeFormat = DateFormat + new string('h', scrapedTime.Split(':')[0].Length) + ":mmtt";

            DateTime parsedDateTime = DateTime.ParseExact(fullDatetimeString, fullDateTimeFormat, CultureInfo.InvariantCulture);

            // parsed time is defaulted to UTC-5, convert to UTC
            parsedDateTime = parsedDateTime.AddHours(5);

            return parsedDateTime;
        }
    }
}
