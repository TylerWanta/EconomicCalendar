using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Google.Cloud.Firestore;
using Google.Cloud.Firestore.V1;
using WebScraper.Scraping;

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

        // store starting with year then month then date so that sorting by text goes from oldest to newest
        private const string _daysDocumentIdFormat = "yyyy.MM.dd";

        private FirestoreDb _db;

        public EconomicEventsDB()
        {
            _db = FirestoreDb.Create("economiccalendar-3756b");
        }

        async public Task AddEventsForDay(DateTime date, List<EconomicEvent> economicEvents)
        {
            if (IncrementCheckReads())
            {
                return;
            }

            string documentName = date.ToString(_daysDocumentIdFormat);
            DocumentReference dayReference = _db.Collection("Days").Document(documentName);
            DocumentSnapshot daySnapShot = await dayReference.GetSnapshotAsync();

            if (!daySnapShot.Exists)
            {
                await dayReference.CreateAsync(new { date });
            }

            foreach (EconomicEvent economicEvent in economicEvents)
            {
                if (IncrementCheckWrites())
                {
                    return;
                }

                DocumentReference eventReference = dayReference.Collection("Events").Document();
                await eventReference.SetAsync(economicEvent);
            }
        }

        async public Task<DateTime?> MostRecentDayThatHasEvents()
        {
            if (IncrementCheckReads())
            {
                return null;
            }

            CollectionReference daysRef = _db.Collection("Days");
            Query mostRecentDay = daysRef.OrderByDescending("date").Limit(1);
            QuerySnapshot querySnapshot = await mostRecentDay.GetSnapshotAsync();

            if (querySnapshot.Documents.Count <= 0)
            {
                return null;
            }

            string documentId = querySnapshot.Documents[0].Id + "6:00";
            return DateTime.ParseExact(documentId, _daysDocumentIdFormat +"h:mm", CultureInfo.InvariantCulture);
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
            int maxFreeWritesThreshold = 15000; // actual is 50,000 but we will go a little lower just to be safe

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
