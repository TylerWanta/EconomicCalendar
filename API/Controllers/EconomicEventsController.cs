using API.Firestore;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

namespace API.Controllers
{
    [Route("EconomicEvents/[controller]")]
    [ApiController]
    public class EconomicEventsController : ControllerBase
    {
        private readonly EconomicEventsDB _economicEventsDB = new EconomicEventsDB();
   
        [HttpGet]
        [Route("from/{utcFrom}")]
        public async Task<ActionResult> GetEventsFrom(DateTime utcFrom, string symbol, byte? impact)
        {
            if (EconomicEventsDBUsageTracker.ExceededReads)
            {
                return Conflict(EconomicEventsDBUsageTracker.ExceededReadsJson);
            }

            string json = JsonConvert.SerializeObject(await _economicEventsDB.GetEventsFrom(utcFrom, symbol, impact));          
            return Ok(json);
        }

        [HttpGet]
        [Route("between/{utcFrom}/{utcTo}")]
        public async Task<ActionResult> GetEvents(DateTime utcFrom, DateTime utcTo, string symbol, byte? impact)
        {
            if (EconomicEventsDBUsageTracker.ExceededReads)
            {
                return Conflict(EconomicEventsDBUsageTracker.ExceededReadsJson);
            }

            string json = JsonConvert.SerializeObject(await _economicEventsDB.GetEventsBetween(utcFrom, utcTo, symbol, impact));
            return Ok(json);
        }
    }
}
