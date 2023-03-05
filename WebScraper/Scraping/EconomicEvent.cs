using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WebScraper.Scraping
{
    class EconomicEvent
    {
        private DateTime _date;
        public DateTime Date => _date;

        private string _title;
        public string Title => _title;

        private string _symbol;
        public string Symbol => _symbol;

        private int _impact;
        public int Impact => _impact;

        private double _forecast;
        public double Forecast => _forecast;

        private double _previous;
        public double Previous => _previous;
        
        public EconomicEvent(DateTime date, string title, string symbol, int impact, double forecast, double previous)
        {
            _date = date;
            _title = title;
            _symbol = symbol;
            _impact = impact;
            _forecast = forecast;
            _previous = previous;

        }
    }
}
