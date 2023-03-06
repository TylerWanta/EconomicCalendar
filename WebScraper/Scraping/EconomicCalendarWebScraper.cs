using System;
using System.Collections.Generic;
using System.Globalization;
using OpenQA.Selenium.Firefox;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;
using System.Linq;
using System.Threading;
using static Google.Cloud.Firestore.V1.StructuredAggregationQuery.Types.Aggregation.Types;
using System.Diagnostics;

namespace WebScraper.Scraping
{
    class EconomicCalendarWebScraper
    {
        static private string BaseUrl => "https://www.forexfactory.com/calendar?day=";

        static private string DateFormat => "MM/dd/yyyy";

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

                DateTime? previousTimeValue = null;
                bool? previousAllDayValue = null;

                foreach (var eventElement in todaysEventsElements)
                {
                    DateTime? time = null;
                    bool? allDay = null;

                    // will happen if more than 1 event occurs at the same time. The proceeding events don't have a time specified in their row,
                    // only on the first events row
                    bool sharesWithPreviousEvent = eventElement.GetAttribute("class").Contains("nogrid");
                    if (sharesWithPreviousEvent)
                    {
                        time = previousTimeValue;
                        allDay = previousAllDayValue;
                    }
                    else
                    {
                        (time, allDay) = ScrapeTime(eventElement, date);
                    }

                    string title = GetElementTextByXPath(eventElement, "td[contains(@class, 'event')]");
                    string symbol = GetElementTextByXPath(eventElement, "td[contains(@class, 'currency')]");
                    byte? impact = ScrapeImpact(eventElement);
                    string forecast = GetElementTextByXPath(eventElement, "td[contains(@class, 'forecast')]");
                    string previous = ScrapePrevious(eventElement);

                    if (!time.HasValue && !allDay.HasValue && string.IsNullOrEmpty(title) && string.IsNullOrEmpty(symbol) && !impact.HasValue 
                        && string.IsNullOrEmpty(forecast) && string.IsNullOrEmpty(previous))
                    {
                        continue;
                    }

                    previousTimeValue = time;
                    previousAllDayValue = allDay;

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
                // should never happen with the elements that call this function
                #if DEBUG
                Debugger.Break();
                #endif
            }

            return value;
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
                    // Holidays are set to 'All Day', some events can have a time of 'Tentative'. We'll just set them to all day
                    if (timeString == "All Day" || timeString.Contains("Tentative"))
                    {
                        SetAllDay();
                    }
                    else
                    {
                        // times have a leading space, remove it 
                        timeString = new string(timeString.ToCharArray().Where(c => !Char.IsWhiteSpace(c)).ToArray());
                        string fullDatetimeString = date.ToString(DateFormat) + timeString;

                        // the string will only have one number before the ':' if its below 10, otherwise 2
                        string fullDateTimeFormat = DateFormat + new string('h', timeString.Split(':')[0].Length) + ":mmtt";

                        time = DateTime.ParseExact(fullDatetimeString, fullDateTimeFormat, CultureInfo.InvariantCulture);

                        // pased time is one hour ahead of central time 
                        time = time.Value.AddHours(-1); 
                        allDay = false;
                    }
                }
                // will happen if we don't have a time 
                else
                {
                    SetAllDay();
                }
            }
            // will catch if we don't have a time and the event is up next
            catch
            {
                SetAllDay();
            }

            time = TimeZoneInfo.ConvertTimeToUtc(time.Value);
            time = DateTime.SpecifyKind(time.Value, DateTimeKind.Utc); // need to specify utc time for firestore

            return (time, allDay);

            void SetAllDay()
            {
                time = date;
                allDay = true;
            }
        }

        static private byte? ScrapeImpact(IWebElement eventElement)
        {
            byte? impact = null;
            try
            {
                var impactElement = eventElement.FindElement(By.XPath("td[contains(@class, 'impact')]"));

                // impact is only distinguishabl by the BEM class name
                string impactClasses = impactElement.GetAttribute("class");
                if (impactClasses.Contains("--low"))
                {
                    impact = 1;
                }
                else if (impactClasses.Contains("--medium"))
                {
                    impact = 2;
                }
                else if (impactClasses.Contains("--high"))
                {
                    impact = 3;
                }
                else if (impactClasses.Contains("--holiday"))
                {
                    impact = 4;
                }
            }
            catch
            {
                // should never happen
                #if DEBUG
                Debugger.Break();
                #endif
            }

            return impact;
        }

        static private string ScrapePrevious(IWebElement eventElement)
        {
            string previous = null;
            try
            {
                var previousElement = eventElement.FindElement(By.XPath("td[contains(@class, 'previous')]"));
                previous = previousElement.Text;

                // if not found then it may be in a nested tag for 'revised' values
                if (string.IsNullOrEmpty(previous))
                {
                    var revisedValueElement = previousElement.FindElement(By.XPath("span[contains(@class, 'revised'"));
                    previous = revisedValueElement.Text;
                }
            }
            catch
            {
                // this is fine as not all events have data for the previous value
            }

            return previous;
        }
    }
}
