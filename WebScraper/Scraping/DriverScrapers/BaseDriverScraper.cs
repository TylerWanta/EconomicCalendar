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

        public event EventHandler<OnDriverFailEventArgs> OnFailHandler;

        public abstract List<EconomicEvent> Scrape(DateTime date);

        protected abstract DateTime ParseScrapedTime(string scrapedTime, DateTime date);

        private int _driverFailedCounter = 0;
        public int DriverFailedCounter { get { return _driverFailedCounter; } }

        private bool _driverFailed = false;
        public bool DriverFailed
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
                    (time, allDay) = ScrapeTime(date, eventElement);
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

            // we successfully scraped data, reset the counter
            if (!_driverFailed && _driverFailedCounter > 0)
            {
                _driverFailedCounter = 0;
            }

            return todaysEvents;
        }

        protected void OnDriverFailed(OnDriverFailEventArgs args = null)
        {
            _driverFailed = true;
            _driverFailedCounter += 1;

            OnFailHandler.Invoke(this, args);
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
                OnDriverFailed();
            }

            return value;
        }

        protected (DateTime?, bool?) ScrapeTime(DateTime date, IWebElement eventElement)
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
                // check if the time is up next since it isn't in the usual spot
                else
                {
                    // throws exception if we don't have an upnext element
                    var upNextElement = timeElement.FindElement(By.XPath("span[contains(@class, 'upnext')]"));
                    timeString = upNextElement.Text;
                }

                if (!string.IsNullOrEmpty(timeString) && !string.IsNullOrWhiteSpace(timeString))
                {
                    // Holidays have a time of 'All Day' and some events can have a time of 'Tentative'. We'll just set them to all day
                    if (timeString == "All Day" || timeString.Contains("Tentative"))
                    {
                        SetAllDay();
                    }
                    else
                    {
                        time = ParseScrapedTime(timeString, date);
                        allDay = false;
                    }
                }
                // will happen if we have a ' ' string in the usual time spot
                else
                {
                    SetAllDay();
                }
            }
            // will catch if we have don't have a time in the usual spot and the event isn't upnext 
            catch
            {
                SetAllDay();
            }

            // firestore requires UTC time to be specified
            time = DateTime.SpecifyKind(time.Value, DateTimeKind.Utc);
            return (time, allDay);

            void SetAllDay()
            {
                time = date;
                allDay = true;
            }
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
                OnDriverFailed();
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
