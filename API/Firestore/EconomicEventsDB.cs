using API.Models;
using Google.Cloud.Firestore;

namespace API.Firestore
{
    class EconomicEventsDB
    {
        private FirestoreDb _db;

        private string _eventsCollection => "Events";

        public EconomicEventsDB()
        {
            _db = FirestoreDb.Create("economiccalendar-3756b");
        }

        async public Task<List<EconomicEvent>> GetEventsFrom(DateTime utcFrom, string symbol, byte? impact)
        {
            CollectionReference eventsRef = _db.Collection(_eventsCollection);
            Query eventsFromDate = eventsRef
                .WhereGreaterThanOrEqualTo(nameof(EconomicEvent.Date), utcFrom)
                .OrderBy(nameof(EconomicEvent.Date));

            TryFilterBySymbolAndImpact(eventsFromDate, symbol, impact);

            QuerySnapshot querySnapshot = await eventsFromDate.GetSnapshotAsync();
            EconomicEventsDBUsageTracker.IncrementReads(querySnapshot.Documents.Count);

            return querySnapshot.Documents.Cast<EconomicEvent>().ToList();
        }

        async public Task<List<EconomicEvent>> GetEventsBetween(DateTime utcFrom, DateTime utcTo, string symbol, byte? impact)
        {
            CollectionReference eventsRef = _db.Collection(_eventsCollection);
            Query eventsBetween = eventsRef
                .WhereGreaterThanOrEqualTo(nameof(EconomicEvent.Date), utcFrom)
                .WhereLessThanOrEqualTo(nameof(EconomicEvent.Date), utcTo)
                .OrderBy(nameof(EconomicEvent.Date));

            TryFilterBySymbolAndImpact(eventsBetween, symbol, impact);

            QuerySnapshot querySnapshot = await eventsBetween.GetSnapshotAsync();
            EconomicEventsDBUsageTracker.IncrementReads(querySnapshot.Documents.Count);

            return querySnapshot.Documents.Cast<EconomicEvent>().ToList();
        }

        private void TryFilterBySymbolAndImpact(Query query, string symbol, byte? impact)
        {
            if (!string.IsNullOrEmpty(symbol))
            {
                query.WhereEqualTo(nameof(EconomicEvent.Symbol), symbol);
            }

            if (impact.HasValue)
            {
                query.WhereEqualTo(nameof(EconomicEvent.Impact), symbol);
            }
        }
    }
}
