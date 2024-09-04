using System;
using Google.Cloud.Firestore;

namespace WebScraper.Types
{
    [FirestoreData]
    public class EconomicEvent
    {
        static public DateTime EarliestEventTime = new DateTime(2011, 1, 1, 0, 0, 0, DateTimeKind.Utc);

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

        private string _forecast;

        [FirestoreProperty]
        public string Forecast => _forecast;

        private string _previous;

        [FirestoreProperty]
        public string Previous => _previous;

        // used as the Event Document's __name__ in firestore. This is so we don't create a duplicate record when we re scrape a day, which we 
        // need to do since the times are stored in UTC but the website displays in UTC-5 i.e. there will always be overlap with events we've already
        // scraped.
        private string _id;
        public string Id { get { return _id; } }

        public EconomicEvent() { }
        
        public EconomicEvent(DateTime date, bool allDay, string title, string symbol, byte impact, string forecast, string previous)
        {
            _date = date;
            _allDay = allDay;
            _title = title;
            _symbol = symbol;
            _impact = impact;
            _forecast = forecast;
            _previous = previous;

            // make sure not to include any '/' or else nexted collections / documents will be created i.e. they are treated like a filepath
            // format yyyy.MM.dd so that they will be in order when viewing them in the Firestore console
            _id = date.ToString("yyyy.MM.dd") + symbol + title.Replace('/', ' ');
        }

        public EconomicEvent(DocumentSnapshot doc)
        {
             _id = doc.Id;
            
            if (doc.TryGetValue<DateTime>("Date", out DateTime date))
            {
                _date = date;
            }

            if (doc.TryGetValue<bool>("AllDay", out bool allDay))
            {
                _allDay = allDay;
            }


            if (doc.TryGetValue<string>("Title", out string title))
            {
                _title = title;
            }


            if (doc.TryGetValue<string>("Symbol", out string symbol))
            {
                _symbol = symbol;
            }


            if (doc.TryGetValue<byte>("Impact", out byte impact))
            {
                _impact = impact;
            }

            if (doc.TryGetValue<string>("Forecast", out string forecast))
            {
                _forecast = forecast;
            }

            if (doc.TryGetValue<string>("Previous", out string previous))
            {
                _previous = previous;
            }
        }
    }
}
