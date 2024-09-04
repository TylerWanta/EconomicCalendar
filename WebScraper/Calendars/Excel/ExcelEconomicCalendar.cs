using System;
using System.Collections.Generic;
using System.IO;
using IronXL;
using WebScraper.Types;

namespace WebScraper.Calendars.Excel
{
    public class ExcelEconomicCalendar
    {
        private string _directory;

        private string _eventsDocument => "Events.csv";

        private string _worksheetName => "Events";

        private string _delimiter => ",";

        public ExcelEconomicCalendar(string dir) 
        {
            _directory = dir;
        }

        private string EventsPath(DateTime date)
        {
            return $"{_directory}/{date.ToString("yyyy/MM/dd")}";
        }

        public void AddEvents(DateTime date, List<EconomicEvent> economicEvents)
        {
            WorkBook book = null;
            WorkSheet worksheet = null;

            string pathToFile = EventsPath(date);

            if (Directory.Exists($"{pathToFile}/{_worksheetName}.{_eventsDocument}"))
            {
                book = WorkBook.LoadCSV(pathToFile, ExcelFileFormat.XLSX, _delimiter);
                worksheet = book.GetWorkSheet(_worksheetName);
            }
            else
            {
                Directory.CreateDirectory($"{pathToFile}");
                book = WorkBook.Create();
                worksheet = book.CreateWorkSheet(_worksheetName);
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

            // will automatically append the worksheet name right before the document name. Will end up as /Events.Events.csv
            book.SaveAsCsv($"{pathToFile}/{_eventsDocument}", _delimiter);
            book.Close();
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
            worksheet[$"B{row}"].StringValue = economicEvent.Date.ToString("yyyy.MM.dd HH:mm"); // MQL4 supported datetime format
            worksheet[$"C{row}"].BoolValue = economicEvent.AllDay;
            worksheet[$"D{row}"].StringValue = economicEvent.Title;
            worksheet[$"E{row}"].StringValue = economicEvent.Symbol;
            worksheet[$"F{row}"].IntValue = economicEvent.Impact;
            worksheet[$"G{row}"].StringValue = economicEvent.Forecast;
            worksheet[$"H{row}"].StringValue = economicEvent.Previous;
        }
    }
}
