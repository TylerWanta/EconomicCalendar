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
    // cloudflare. Very inefficent though. Also can't send cookies to format the dates, resulting in a lot more work in order to get them correct
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

        override protected (DateTime?, bool?) ScrapeTime(IWebElement eventElement, DateTime date)
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

                        // the string will only have one number before the ':' if its below 10, need to make sure our 'h' format matches it
                        string fullDateTimeFormat = DateFormat + new string('h', timeString.Split(':')[0].Length) + ":mmtt";

                        time = DateTime.ParseExact(fullDatetimeString, fullDateTimeFormat, CultureInfo.InvariantCulture);

                        // parsed time is defaulted to UTC -5 
                        time = time.Value.AddHours(5);
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

            // need to account for daylight savings time
            // account for being directly on the hour
            //if (TimeZoneInfo.Local.IsInvalidTime(time.Value))
            //{
            //    //  since we move clocks forward on the second sunday of March at 1:00, we need to subtract an extra hour since 2:00 central time doesn't exist
            //    if (time.Value.Month == 3 && NthDayOfMonth(time.Value, DayOfWeek.Sunday, 2) && time.Value.Hour == 2)
            //    {
            //        time = time.Value.AddHours(-1);
            //    }
            //    // since we move clocks backwards on the first sunday of November at 1:00, we need to add an extra hour since 2:00 central time doesn't exit
            //    else if (time.Value.Month == 11 && NthDayOfMonth(time.Value, DayOfWeek.Sunday, 1) && time.Value.Hour == 2)
            //    {
            //        time = time.Value.AddHours(1);
            //    }
            //}
            //// within daylight savings time, subtract another hour
            //else if (TimeZoneInfo.Local.IsDaylightSavingTime(time.Value))
            //{
            //    time = time.Value.AddHours(2);
            //}

            // need to specify utc time for firestore
            //time = DateTime.SpecifyKind(time.Value, DateTimeKind.Local);
            //time = TimeZoneInfo.ConvertTimeToUtc(time.Value, TimeZoneInfo.Local);
            time = DateTime.SpecifyKind(time.Value, DateTimeKind.Utc);

            return (time, allDay);

            void SetAllDay()
            {
                time = date;
                allDay = true;
            }
        }

        //static private bool NthDayOfMonth(DateTime date, DayOfWeek dow, int nth)
        //{
        //    int d = date.Day;
        //    return date.DayOfWeek == dow && (d - 1) / 7 == (nth - 1);
        //}
    }
}
