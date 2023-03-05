using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net.Http;
using System.Threading.Tasks;
using HtmlAgilityPack;

namespace WebScraper.Scraping
{
    class EconomicCalendarWebScraper
    {
        static private string BaseUrl => "https://www.forexfactory.com/calendar?day=";

        static public List<EconomicEvent> ScrapeToday()
        {
            using (HttpClient client = new HttpClient())
            {
                return Scrape(client, DateTime.Now).Result;
            }
        }

        static public List<EconomicEvent> ScrapeDate(DateTime date)
        {
            using (HttpClient client = new HttpClient())
            {
                return Scrape(client, date).Result;
            }
        }

        static async private Task<List<EconomicEvent>> Scrape(HttpClient client, DateTime date)
        {
            var html = await client.GetStringAsync(UrlForDate(date));

            HtmlDocument htmlDocument = new HtmlDocument();
            htmlDocument.LoadHtml(html);

            List<EconomicEvent> events = new List<EconomicEvent>();

            foreach (HtmlNode row in htmlDocument.DocumentNode.SelectNodes("//tr[@data-touchable and @data-eventid]"))
            {
                DateTime time = new DateTime();
                string title = "";
                string symbol = "";
                byte impact = 0;
                double forecast = -1.0;
                double previous = -1.0;

                foreach (HtmlNode column in row.Descendants("td"))
                {
                    if (column.HasClass("time"))
                    {
                        time = DateTime.ParseExact(column.InnerText, "hh:mmtt", CultureInfo.InvariantCulture);
                    }
                    else if (column.HasClass("event"))
                    {
                        title = column.SelectSingleNode("//span[contains(@class, 'calendar__event-title')]")?.InnerText;
                    }
                    else if (column.HasClass("currency"))
                    {
                        symbol = column.InnerText;
                    }
                    else if (column.HasClass("forecast"))
                    {
                        if (Double.TryParse(column.InnerText, out double result))
                        {
                            forecast = result;
                        }
                    }
                    else if (column.HasClass("previous"))
                    {
                        if (Double.TryParse(column.InnerText, out double result))
                        {
                            previous = result;
                        }
                    }
                    else if (column.HasClass("calendar__impact--low"))
                    {
                        impact = 1;
                    }
                    else if (column.HasClass("calendar__impact--medium"))
                    {
                        impact = 2;
                    }
                    else if (column.HasClass("calendar__impact--high"))
                    {
                        impact = 3;
                    }
                }

                events.Add(new EconomicEvent(time, title, symbol, impact, forecast, previous));
            }

            return events;
        }

        static private string UrlForDate(DateTime date)
        {
            return BaseUrl + $"{date.ToString("MMM")}.{date.Day}.{date.Year}";
        }
    }
}
