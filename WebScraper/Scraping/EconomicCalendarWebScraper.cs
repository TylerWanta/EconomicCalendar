using System;
using System.Collections.Generic;
using System.Globalization;
using OpenQA.Selenium.Firefox;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;

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
            // since they don't seem to like any redirects 
            using (FirefoxDriver driver = new FirefoxDriver())
            {
                driver.Navigate().GoToUrl(UrlForDate(date));

                WebDriverWait pageHasLoaded = new WebDriverWait(driver, TimeSpan.FromSeconds(10));
                pageHasLoaded.Until(d => d.FindElement(By.ClassName("calendar__table")));

                var todaysEventsElements = driver.FindElements(By.XPath("//tr[@data-touchable and @data-eventid]"));
                foreach (var eventElement in todaysEventsElements)
                {
                    DateTime time = new DateTime();
                    string title = "";
                    string symbol = "";
                    byte impact = 0;
                    double forecast = -1.0;
                    double previous = -1.0;

                    // time is formatted in 2 different ways based on if its up next or not
                    var timeElement = eventElement.FindElement(By.XPath("//td[contains(@class, 'time')]"));
                    if (timeElement != null)
                    {
                        string timeString = "";
                        if (!string.IsNullOrEmpty(timeElement.Text))
                        {
                            timeString = timeElement.Text;
                        }
                        // event must be up next since the time isn't in the usual spot
                        else
                        {
                            var upNextElement = timeElement.FindElement(By.XPath("//span[contains(@class, 'upnext')]"));
                            if (upNextElement != null)
                            {
                                timeString = upNextElement.Text;
                            }
                        }

                        time = DateTime.ParseExact(timeString, "hh:mmtt", CultureInfo.InvariantCulture);
                    }

                    var eventTitle = eventElement.FindElement(By.XPath("//td[contains(@class, 'event')]"));
                    if (eventTitle != null)
                    {
                        title = eventTitle.Text;
                    }

                    var symbolElement = eventElement.FindElement(By.XPath("//td[contains(@class, 'currency')]"));
                    if (symbolElement != null)
                    {
                        symbol = symbolElement.Text;
                    }

                    var forecastElement = eventElement.FindElement(By.XPath("//td[contains(@class, 'forecast')]"));
                    if (forecastElement != null)
                    {
                        if (Double.TryParse(forecastElement.Text, out double result))
                        {
                            forecast = result;
                        }
                    }

                    var previousElement = eventElement.FindElement(By.XPath("//td[contains(@class, 'previous')]"));
                    if (previousElement != null)
                    {
                        if (Double.TryParse(previousElement.Text, out double result))
                        {
                            previous = result;
                        }
                    }

                    var impactElement = eventElement.FindElement(By.XPath("//td[contains(@class, 'impact')]"));
                    if (impactElement != null)
                    {
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
                        }
                    }

                    todaysEvents.Add(new EconomicEvent(time, title, symbol, impact, forecast, previous));
                }
            }

            return todaysEvents;
        }
    }
}
