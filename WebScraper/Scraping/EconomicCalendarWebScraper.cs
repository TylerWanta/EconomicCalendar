using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.UI.WebControls.WebParts;
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

        static public List<EconomicEvent> ScrapeFromDate(DateTime fromDate)
        {
            List<EconomicEvent> events = new List<EconomicEvent>();
            using (HttpClient client = new HttpClient())
            {
                int count = 0;
                while (fromDate <= DateTime.Now)
                {
                    // sleep every few requests so that we don't overload their servers
                    if (count >= 50)
                    {
                        System.Threading.Thread.Sleep(10000);
                        count = 0;
                    }

                    events.AddRange(Scrape(client,fromDate).Result);
                    fromDate.AddDays(1);

                    count += 1;
                }
            }

            return events;
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
                int impact = -1;
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
