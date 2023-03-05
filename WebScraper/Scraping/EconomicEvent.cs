﻿using System;
using Google.Cloud.Firestore;

namespace WebScraper.Scraping
{
    [FirestoreData]
    class EconomicEvent
    {
        private DateTime _date;

        [FirestoreProperty]
        public DateTime Date => _date;

        private bool _allDay;

        [FirestoreProperty]
        public bool AllDay => _allDay;

        private string _title;

        [FirestoreProperty]
        public string Title => _title;

        private string _symbol;

        [FirestoreProperty]
        public string Symbol => _symbol;

        private byte _impact;

        [FirestoreProperty]
        public byte Impact => _impact;

        private double? _forecast;

        [FirestoreProperty]
        public double? Forecast => _forecast;

        private double? _previous;

        [FirestoreProperty]
        public double? Previous => _previous;

        public EconomicEvent() { }
        
        public EconomicEvent(DateTime date, bool allDay, string title, string symbol, byte impact, double? forecast, double? previous)
        {
            _date = date;
            _allDay = allDay;
            _title = title;
            _symbol = symbol;
            _impact = impact;
            _forecast = forecast;
            _previous = previous;

        }
    }
}
