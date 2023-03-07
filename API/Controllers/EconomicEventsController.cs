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
        public async Task<ActionResult> GetEventsFrom(DateTime utcFrom)
        {
            if (EconomicEventsDBUsageTracker.ExceededReads)
            {
                return Conflict(EconomicEventsDBUsageTracker.ExceededReadsJson);
            }

            string json = JsonConvert.SerializeObject(await _economicEventsDB.GetEventsFrom(utcFrom));          
            return Ok(json);
        }

        [HttpGet]
        [Route("between/{utcFrom}/{utcTo}")]
        public async Task<ActionResult> GetEvents(DateTime utcFrom, DateTime utcTo)
        {
            if (EconomicEventsDBUsageTracker.ExceededReads)
            {
                return Conflict(EconomicEventsDBUsageTracker.ExceededReadsJson);
            }

            string json = JsonConvert.SerializeObject(await _economicEventsDB.GetEventsBetween(utcFrom, utcTo));
            return Ok(json);
        }
    }
}
