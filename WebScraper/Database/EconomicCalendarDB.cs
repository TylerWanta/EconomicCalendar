using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using IronXL;
using WebScraper.Types;

namespace WebScraper.Database
{
    public class EconomicCalendarDB
    {
        public string DBDirectory => "C:\\Users\\WantaTyler\\source\\repos\\EconomicCalendar\\WebScraper\\Database\\Data";

        public string EventsDocument => "Events.csv";

        public string WorkSheetName => "Events";

        public string Delimiter => ",";

        public EconomicCalendarDB() { }

        public string EventsPath(DateTime date)
        {
            return $"{DBDirectory}\\{date.ToString("yyyy\\MM\\dd")}\\{EventsDocument}";
        }

        public void Add(DateTime date, List<EconomicEvent> economicEvents)
        {
            ValidateEvents(date, economicEvents);

            WorkBook book = null;
            WorkSheet worksheet = null;

            string path = EventsPath(date);

            if (Directory.Exists(path))
            {
                book = WorkBook.LoadCSV(path, ExcelFileFormat.XLSX, Delimiter);
                worksheet = book.GetWorkSheet(WorkSheetName);
            }
            else
            {
                book = WorkBook.Create();
                worksheet = book.CreateWorkSheet(WorkSheetName);
            }

            CheckWriteHeaders(worksheet);
            int row = NextRowNumber(worksheet);

            foreach (EconomicEvent economicEvent in economicEvents)
            {
                if (!ContainsEvent(worksheet, economicEvent))
                {
                    WriteEvent(row, worksheet, economicEvent);
                    row += 1;
                }
            }

            book.SaveAsCsv(path, Delimiter);
            book.Close();
        }

        private void ValidateEvents(DateTime date, List<EconomicEvent> events)
        {
            List<EconomicEvent> eventsNotWithinDay = events.Where(e => e.Date.Day != date.Day).ToList();

            if (events.Where(e => e.Date.Day != date.Day).Count() > 0)
            {
                throw new Exception("Not all events occur on the same day");
            }

            if (events.Where(e => e.Date.Kind == DateTimeKind.Utc).Count() > 0)
            {
                throw new Exception("Not all dates are in UTC");
            }
        }

        private int NextRowNumber(WorkSheet worksheet)
        {
            int rowNumber = 2;
            while (!string.IsNullOrEmpty(worksheet[$"A{rowNumber}"].StringValue))
            {
                rowNumber += 1;
            }

            return rowNumber;
        }

        private void CheckWriteHeaders(WorkSheet worksheet)
        {
            if (!string.IsNullOrEmpty(worksheet["A1"].StringValue))
            {
                return;
            }

            worksheet["A1"].StringValue = "Id";
            worksheet["B1"].StringValue = "Date";
            worksheet["C1"].StringValue = "All Day";
            worksheet["D1"].StringValue = "Title";
            worksheet["E1"].StringValue = "Symbol";
            worksheet["F1"].StringValue = "Impact";
            worksheet["G1"].StringValue = "Forecast";
            worksheet["H1"].StringValue = "Previous";
        }

        private bool ContainsEvent(WorkSheet worksheet, EconomicEvent economicEvent)
        {
            int row = 2;
            while (!string.IsNullOrEmpty(worksheet[$"A{row}"].StringValue))
            {
                if (worksheet[$"A{row}"].StringValue == economicEvent.Id)
                {
                    return true;
                }

                row += 1;
            }

            return false;
        }

        private void WriteEvent(int row, WorkSheet worksheet, EconomicEvent economicEvent)
        {
            worksheet[$"A{row}"].StringValue = economicEvent.Id;
            worksheet[$"B{row}"].DateTimeValue = economicEvent.Date;
            worksheet[$"C{row}"].BoolValue = economicEvent.AllDay;
            worksheet[$"D{row}"].StringValue = economicEvent.Title;
            worksheet[$"E{row}"].StringValue = economicEvent.Symbol;
            worksheet[$"F{row}"].IntValue = economicEvent.Impact;
            worksheet[$"G{row}"].StringValue = economicEvent.Forecast;
            worksheet[$"H{row}"].StringValue = economicEvent.Previous;
        }
    }
}
