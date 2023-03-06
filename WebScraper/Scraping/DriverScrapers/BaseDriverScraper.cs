using System;
using System.Collections.Generic;
using System.Diagnostics;
using OpenQA.Selenium;
using WebScraper.Types;

namespace WebScraper.Scraping.DriverScrapers
{
    abstract class BaseDriverScraper : IDriverScraper
    {
        protected string BaseUrl => "https://www.forexfactory.com/calendar?day=";

        protected string DateFormat => "MM/dd/yyyy";

        public event EventHandler OnFailHandler;

        public abstract List<EconomicEvent> Scrape(DateTime date);

        protected abstract (DateTime?, bool?) ScrapeTime(IWebElement eventElement, DateTime date); 

        protected List<EconomicEvent> BaseScrape(IWebDriver driver, DateTime date)
        {
            List<EconomicEvent> todaysEvents = new List<EconomicEvent>();
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

            return todaysEvents;
        }

        protected void FailedToLoad()
        {
            OnFailHandler.Invoke(this, null);
        }

        protected string UrlForDate(DateTime date)
        {
            return BaseUrl + $"{date.ToString("MMM")}{date.Day}.{date.Year}";
        }

        protected string GetElementTextByXPath(IWebElement parent, string xpath)
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

        protected byte? ScrapeImpact(IWebElement eventElement)
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

        protected string ScrapePrevious(IWebElement eventElement)
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
