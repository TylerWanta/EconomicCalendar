using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WebScraper.Calendars.Firestore;
using WebScraper.Types;

namespace WebScraper.Calendars.Excel
{
    static class ExcelCalendars
    {
        // path to MT4 directories. Should be /MQL4/files/ for running strategies in real time or /tester/files/ for backtesting
        // can add different MT4 Instances if needed
        static public string[] CalendarLocations => new string[] 
        {
            "E:/MT4s/MT4-Backtest/tester/files/EconomicCalendar/JustEvents"
        };

        // have this be a Task<bool> so we don't break when awaiting this in Program.cs
        // can't call GetAwaiter().GetResult() with null as the return type
        async static public Task<bool> SyncWithFirestore()
        {
            FirestoreEconomicCalendar firestoreDB = new FirestoreEconomicCalendar();

            foreach (string dir in CalendarLocations)
            {
                DateTime? startDate = null;
                DateTime endDate = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, DateTime.UtcNow.Day, 0, 0, 0, DateTimeKind.Utc);

                if (MostRecentEventDirectory(dir, out int? year, out int? month, out int? day))
                {
                    startDate = new DateTime(year.Value, month.Value, day.Value, 0, 0, 0, DateTimeKind.Utc);

                    // add one since this was the furthest day that we already have
                    startDate = startDate.Value.AddDays(1); 
                }
                else
                {
                    startDate = EconomicEvent.EarliestEventTime;
                }

                ExcelEconomicCalendar excelDB = new ExcelEconomicCalendar(dir);

                DateTime dateLow = startDate.Value;
                DateTime dateHigh = startDate.Value.AddDays(1);

                while (dateLow <= endDate)
                {
                    List<EconomicEvent> events = await firestoreDB.GetEventsBetween(dateLow, dateHigh);
                    if (events.Any())
                    {
                        excelDB.AddEvents(dateLow, events);
                    }

                    dateLow = dateLow.AddDays(1);
                    dateHigh = dateHigh.AddDays(1);
                }
            }

            return true;
        }

        static public void AddEventsToCalendars(DateTime date, List<EconomicEvent> events)
        {
            ValidateEvents(date, events);

            foreach (string dir in CalendarLocations)
            {
                ExcelEconomicCalendar excelDB = new ExcelEconomicCalendar(dir);
                excelDB.AddEvents(date, events);
            }
        }

        static private bool MostRecentEventDirectory(string dir, out int? year, out int? month, out int? day)
        {
            year = null;
            month = null;
            day = null;

            if (!Directory.Exists(dir))
            {
                return false;
            }

            // these will all have the same path up to the year so we can just order them and pick the last one
            string furthestYearDir = Directory.GetDirectories(dir).OrderBy(x => x).Last();
            if (string.IsNullOrEmpty(furthestYearDir))
            {
                return false;
            }

            year = int.Parse(furthestYearDir.Substring(furthestYearDir.Length - 4));

            string furthestMonthDir = Directory.GetDirectories(furthestYearDir).OrderBy(x => x).Last();
            if (string.IsNullOrEmpty(furthestMonthDir))
            {
                return false;
            }

            month = int.Parse(furthestMonthDir.Substring(furthestMonthDir.Length - 2));

            string furthestDayDir = Directory.GetDirectories(furthestMonthDir).OrderBy(x => x).Last();
            if (string.IsNullOrEmpty(furthestDayDir))
            {
                return false;
            }

            day = int.Parse(furthestDayDir.Substring(furthestDayDir.Length - 2));
            return true;
        }

        static private void ValidateEvents(DateTime date, List<EconomicEvent> events)
        {
            List<EconomicEvent> eventsNotWithinDay = events.Where(e => e.Date.Day != date.Day).ToList();

            if (events.Where(e => e.Date.Day != date.Day).Count() > 0)
            {
                throw new Exception("Not all events occur on the same day");
            }

            if (events.Where(e => e.Date.Kind != DateTimeKind.Utc).Count() > 0)
            {
                throw new Exception("Not all dates are in UTC");
            }
        }
    }
}
