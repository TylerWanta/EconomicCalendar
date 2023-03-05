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

        private const string _daysDocumentIdFormat = "MM.dd.yyyy";

        private FirestoreDb _db;

        public EconomicEventsDB()
        {
            _db = FirestoreDb.Create("economiccalendar-3756b");
        }

        async public void AddEventsForDay(DateTime date, List<EconomicEvent> economicEvents)
        {
            if (IncrementCheckReads())
            {
                return;
            }

            string documentName = date.ToString(_daysDocumentIdFormat);
            CollectionReference events = _db.Collection("Days").Document(documentName).Collection("Events");

            foreach (EconomicEvent economicEvent in economicEvents)
            {
                if (IncrementCheckWrites())
                {
                    return;
                }

                DocumentReference documentRef = events.Document();
                await documentRef.CreateAsync(economicEvent);
            }
        }

        async public Task<DateTime?> MostRecentDayThatHasEvents()
        {
            if (IncrementCheckReads())
            {
                return null;
            }

            CollectionReference daysRef = _db.Collection("Days");
            Query mostRecentDay = daysRef.OrderByDescending(FieldPath.DocumentId).Limit(1);
            QuerySnapshot querySnapshot = await mostRecentDay.GetSnapshotAsync();

            if (querySnapshot.Documents.Count <= 0)
            {
                return null;
            }

            string documentId = querySnapshot.Documents[0].Id;
            return DateTime.ParseExact(documentId, _daysDocumentIdFormat, CultureInfo.InvariantCulture);
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

            _exceededWrites = maxFreeWritesThreshold > _writes;
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
