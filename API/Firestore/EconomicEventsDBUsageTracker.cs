using Newtonsoft.Json;

namespace API.Firestore
{
    public static class EconomicEventsDBUsageTracker
    {
        static private int _reads = 0;
        static private bool _exceededReads;
        static public bool ExceededReads { get { return _exceededReads; } }
        static public string ExceededReadsJson = JsonConvert.SerializeObject(new { message = "Exceeded the max allowed reads for today." });

        static private int _writes = 0;
        static private bool _exceededWrites;
        static public bool ExceededWrites { get { return _exceededWrites; } }
        static public string ExceededWritesJson = JsonConvert.SerializeObject(new { message = "Exceeded the max allowed writes for today." });

        static private DateTime _currentTrackingDayPST = DateTime.Now.AddHours(-2);

        static public void SetDate()
        {
            // Reset usage is tracked in PST time, I live in central which is 2 hours ahead
            _currentTrackingDayPST = DateTime.Now.AddHours(-2);
        }

        static private void CheckNewDay()
        {
            DateTime currentDayPST = DateTime.Now.AddHours(-2);
            if (currentDayPST.Day > _currentTrackingDayPST.Day)
            {
                _reads = 0;
                _writes = 0;

                _exceededReads = false;
                _exceededWrites = false;

                _currentTrackingDayPST = currentDayPST;
            }
        }

        static public void IncrementReads(int count)
        {
            CheckNewDay();

            int maxReadsPerDay = 45000; // actual is 50,000 but we will go a little lower just to be safe
            if (_reads + count > maxReadsPerDay)
            {
                _exceededReads = true;
                return;
            }

            _reads += count;
        }

        static public void IncrementWrites(int count)
        {
            CheckNewDay();

            int maxWritesPerDay = 15000; // actual is 20,000 but we will go a little lower just to be safe
            if (_writes + count > maxWritesPerDay)
            {
                _exceededWrites = true;
                return;
            }

            _writes += count;
        }
    }
}
