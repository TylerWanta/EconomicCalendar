using Google.Cloud.Firestore;

namespace API.Models
{
    [FirestoreData]
    public class EconomicEvent
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

        private string _forecast;

        [FirestoreProperty]
        public string Forecast => _forecast;

        private string _previous;

        [FirestoreProperty]
        public string Previous => _previous;

        public EconomicEvent() { }

        //public EconomicEvent(DocumentSnapshot ds)
        //{ 
        //    if (ds.TryGetValue<DateTime>(nameof(Date), out DateTime date))
        //    {
        //        _date = date;
        //    }

        //    if (ds.TryGetValue<bool>(nameof(AllDay), out bool allDay))
        //    {
        //        _allDay = allDay;
        //    }

        //    if (ds.TryGetValue<bool>(nameof(AllDay), out bool allDay))
        //    {
        //        _allDay = allDay;
        //    }

        //    if (ds.TryGetValue<bool>(nameof(AllDay), out bool allDay))
        //    {
        //        _allDay = allDay;
        //    }

        //    if (ds.TryGetValue<bool>(nameof(AllDay), out bool allDay))
        //    {
        //        _allDay = allDay;
        //    }

        //    if (ds.TryGetValue<bool>(nameof(AllDay), out bool allDay))
        //    {
        //        _allDay = allDay;
        //    }

        //    if (ds.TryGetValue<bool>(nameof(AllDay), out bool allDay))
        //    {
        //        _allDay = allDay;
        //    }
        //}

        public EconomicEvent(DateTime date, bool allDay, string title, string symbol, byte impact, string forecast, string previous)
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
