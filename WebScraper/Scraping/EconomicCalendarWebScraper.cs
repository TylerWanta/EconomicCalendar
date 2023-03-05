using System;
using System.Collections.Generic;
using System.Globalization;
using OpenQA.Selenium.Firefox;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;
using System.Linq;

namespace WebScraper.Scraping
{
    class EconomicCalendarWebScraper
    {
        static private string BaseUrl => "https://www.forexfactory.com/calendar?day=";

        static private string UrlForDate(DateTime date)
        {
            return BaseUrl + $"{date.ToString("MMM")}{date.Day}.{date.Year}";
        }

        static public List<EconomicEvent> ScrapeToday()
        {
            return Scrape(DateTime.Now);
        }

        static public List<EconomicEvent> ScrapeDate(DateTime date)
        {
            return Scrape(date); 
        }

        static private List<EconomicEvent> Scrape(DateTime date)
        {
            List<EconomicEvent> todaysEvents = new List<EconomicEvent>();

            // load up a new driver for each page. Yea it sucks but it seems to be the only way to get forexfactory to load
            // since they don't seem to like any redirects. Don't have to worry about overloading their servers though since 
            // it takes a few secons to spin up a webdriver instance
            using (FirefoxDriver driver = new FirefoxDriver())
            {
                driver.Navigate().GoToUrl(UrlForDate(date));

                WebDriverWait pageHasLoaded = new WebDriverWait(driver, TimeSpan.FromSeconds(10));
                pageHasLoaded.Until(d => d.FindElement(By.ClassName("calendar__table")));

                var todaysEventsElements = driver.FindElements(By.XPath("//tr[@data-touchable and @data-eventid]"));
                foreach (var eventElement in todaysEventsElements)
                {
                    (DateTime? time, bool? allDay) = ScrapeTime(eventElement, date);
                    string title = GetElementTextByXPath(eventElement, "td[contains(@class, 'event')]");
                    string symbol = GetElementTextByXPath(eventElement, "td[contains(@class, 'currency')]");
                    byte? impact = ScrapeImpact(eventElement);
                    double? forecast = GetElementDoubleByXPath(eventElement, "td[contains(@class, 'forecast')]");
                    double? previous = GetElementDoubleByXPath(eventElement, "td[contains(@class, 'previous')]"); ;

                    if (!time.HasValue && !allDay.HasValue && string.IsNullOrEmpty(title) && string.IsNullOrEmpty(symbol) && !impact.HasValue 
                        && !forecast.HasValue && !previous.HasValue)
                    {
                        continue;
                    }

                    todaysEvents.Add(new EconomicEvent(time.Value, allDay.Value, title, symbol, impact.Value, forecast, previous));
                }
            }

            return todaysEvents;
        }

        static private string GetElementTextByXPath(IWebElement parent, string xpath)
        {
            string value = "";
            try
            {
                var element = parent.FindElement(By.XPath(xpath));
                value = element.Text;
            }
            catch 
            { 
                // this is just here so that the program doesn't crash if the element isn't on screen 
            }

            return value;
        }

        static private double? GetElementDoubleByXPath(IWebElement parent, string xpath)
        {
            string value = GetElementTextByXPath(parent, xpath);
            if (Double.TryParse(value, out double result))
            {
                return result;
            }

            return null;
        }

        static private (DateTime?, bool?) ScrapeTime(IWebElement eventElement, DateTime date)
        {
            DateTime? time = null;
            bool? allDay = null;

            try
            {
                // time is formatted in 2 different ways based on if its up next or not
                var timeElement = eventElement.FindElement(By.XPath("td[contains(@class, 'time')]"));
                string timeString = "";
                if (!string.IsNullOrEmpty(timeElement.Text))
                {
                    timeString = timeElement.Text;
                }
                // check if the time is up next since the time isn't in the usual spot
                else
                {
                    var upNextElement = timeElement.FindElement(By.XPath("span[contains(@class, 'upnext')]"));
                    timeString = upNextElement.Text;
                }

                if (!string.IsNullOrEmpty(timeString))
                {
                    // Holidays are set to 'All Day'
                    if (timeString == "All Day")
                    {
                        time = new DateTime(date.Year, date.Month, date.Day);
                        allDay = true;
                    }
                    else
                    {
                        // times have a leading space, remove it 
                        timeString = new string(timeString.ToCharArray().Where(c => !Char.IsWhiteSpace(c)).ToArray());

                        // the string will only have one number before the ':' if its below 10, otherwise 2
                        string timeFormat = new string('h', timeString.Split(':')[0].Length) + ":mmtt";

                        time = DateTime.ParseExact(timeString, timeFormat, CultureInfo.InvariantCulture);
                        allDay = false;
                    }
                }
                // will happen if we don't have a time 
                else
                {
                    time = date;
                    allDay = true;
                }
            }
            // will catch if we don't have a time and the event is up next
            catch
            {
                time = date;
                allDay = true;
            }

            time = TimeZoneInfo.ConvertTimeToUtc(time.Value);
            time = DateTime.SpecifyKind(time.Value, DateTimeKind.Utc); // need to specify utc time for firestore

            return (time, allDay);
        }

        static private byte? ScrapeImpact(IWebElement eventElement)
        {
            byte? impact = null;
            try
            {
                var impactElement = eventElement.FindElement(By.XPath("td[contains(@class, 'impact')]"));

                // impact is only distinguishabl by the BEM class name
                string[] impactClasses = impactElement.GetAttribute("class").Split(' ');
                foreach (string impactClass in impactClasses)
                {
                    if (impactClass.Contains("low"))
                    {
                        impact = 1;
                    }
                    else if (impactClass.Contains("medium"))
                    {
                        impact = 2;
                    }
                    else if (impactClass.Contains("high"))
                    {
                        impact = 3;
                    }
                    else if (impactClass.Contains("holiday"))
                    {
                        impact = 4;
                    }
                }
                
            }
            catch
            {
                // this is just in case we can't find an element

            }

            return impact;
        }
    }
}
