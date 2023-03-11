using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Google.Cloud.Firestore;
using WebScraper.Types;

namespace WebScraper.Firestore
{
    class EconomicEventsDB
    {
        private int _reads = 0;
        private bool _notifiedExceededReads = false;

        private bool _exceededReads;
        public bool ExceededReads { get { return _exceededReads; } } 

        private int _writes = 0;
        private bool _notifiedExceededWrites = false;

        private bool _exceededWrites;
        public bool ExceededWrites { get { return _exceededWrites; } }

        private FirestoreDb _db;

        private string _eventsCollection => "Events";

        public EconomicEventsDB()
        {
            _db = FirestoreDb.Create("economiccalendar-3756b");
        }

        async public Task AddEvents(List<EconomicEvent> economicEvents)
        {
            foreach (EconomicEvent economicEvent in economicEvents)
            {
                if (IncrementCheckWrites())
                {
                    return;
                }

                DocumentReference eventReference = _db.Collection(_eventsCollection).Document(economicEvent.Id);
                await eventReference.SetAsync(economicEvent); // will overwrite event if it already exists
            }
        }

        async public Task<List<EconomicEvent>> GetEventsBetween(DateTime utcFrom, DateTime utcTo)
        {
            CollectionReference eventsRef = _db.Collection(_eventsCollection);
            Query eventsFromDate = eventsRef
                .WhereGreaterThanOrEqualTo(nameof(EconomicEvent.Date), utcFrom)
                .WhereLessThan(nameof(EconomicEvent.Date), utcTo)
                .OrderBy(nameof(EconomicEvent.Date));

            QuerySnapshot querySnapshot = await eventsFromDate.GetSnapshotAsync();
            return querySnapshot.Documents.Select(ds => new EconomicEvent(ds)).ToList();
        }

        async public Task<DateTime?> MostRecentDayThatHasEvents()
        {
            if (IncrementCheckReads())
            {
                return null;
            }

            CollectionReference eventsRef = _db.Collection(_eventsCollection);
            Query mostRecentEvent = eventsRef.OrderByDescending(nameof(EconomicEvent.Date)).Limit(1);
            QuerySnapshot querySnapshot = await mostRecentEvent.GetSnapshotAsync();

            if (querySnapshot.Documents.Count <= 0)
            {
                return null;
            }

            Timestamp mostRecentDate = querySnapshot.Documents[0].GetValue<Timestamp>(nameof(EconomicEvent.Date));
            return mostRecentDate.ToDateTime();
        }

        private bool IncrementCheckWrites()
        {
            _reads += 1;
            int maxFreeReadsThreshold = 45000; // actual is 50,000 but we will go a little lower just to be safe

            _exceededReads = _reads > maxFreeReadsThreshold;
            if (_exceededReads && !_notifiedExceededReads)
            {
                // notif me somehow
                Console.WriteLine("Reached maximum reads for today");
                _notifiedExceededReads = true;
            }

            return _exceededReads;
        }

        private bool IncrementCheckReads()
        {
            _writes += 1;
            int maxFreeWritesThreshold = 15000; // actual is 20,000 but we will go a little lower just to be safe

            _exceededWrites = _writes > maxFreeWritesThreshold;
            if (_exceededWrites && !_notifiedExceededWrites)
            {
                // notif me somehow
                Console.WriteLine("Reached maximum writes for today");
                _notifiedExceededWrites = true;
            }

            return _exceededWrites;
        }
    }
}
