using API.Firestore;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

namespace API.Controllers
{
    [Route("/[controller]")]
    [ApiController]
    public class EconomicEventsController : ControllerBase
    {
        private readonly EconomicEventsDB _economicEventsDB = new EconomicEventsDB();

        [EnableCors]
        [HttpGet]
        [Route("from/")]
        public async Task<ActionResult> GetEventsFrom(DateTime utcFrom, string? symbol, byte? impact)
        {
            if (EconomicEventsDBUsageTracker.ExceededReads)
            {
                return Conflict(EconomicEventsDBUsageTracker.ExceededReadsJson);
            }

            utcFrom = DateTime.SpecifyKind(utcFrom, DateTimeKind.Utc);

            string json = JsonConvert.SerializeObject(await _economicEventsDB.GetEventsFrom(utcFrom, symbol, impact));          
            return Ok(json);
        }

        [EnableCors]
        [HttpGet]
        [Route("between/")]
        public async Task<ActionResult> GetEvents(DateTime utcFrom, DateTime utcTo, string? symbol, byte? impact)
        {
            if (EconomicEventsDBUsageTracker.ExceededReads)
            {
                return Conflict(EconomicEventsDBUsageTracker.ExceededReadsJson);
            }

            utcFrom = DateTime.SpecifyKind(utcFrom, DateTimeKind.Utc);
            utcTo = DateTime.SpecifyKind(utcTo, DateTimeKind.Utc);

            string json = JsonConvert.SerializeObject(await _economicEventsDB.GetEventsBetween(utcFrom, utcTo, symbol, impact));
            return Ok(json);
        }
    }
}
