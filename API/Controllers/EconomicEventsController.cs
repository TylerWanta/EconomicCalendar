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
            string json = "";
            if (EconomicEventsDBUsageTracker.ExceededReads)
            {
                json = EconomicEventsDBUsageTracker.ExceededReadsJson;
            }
            else
            {
                json = JsonConvert.SerializeObject(await _economicEventsDB.GetEventsFrom(utcFrom));
            }

            return new JsonResult(json);
        }

        [HttpGet]
        [Route("between/{utcFrom}/{utcTo}")]
        public async Task<ActionResult> GetEvents(DateTime utcFrom, DateTime utcTo)
        {
            string json = "";
            if (EconomicEventsDBUsageTracker.ExceededReads)
            {
                json = EconomicEventsDBUsageTracker.ExceededReadsJson;
            }
            else
            {
                json = JsonConvert.SerializeObject(await _economicEventsDB.GetEventsBetween(utcFrom, utcTo));
            }

            return new JsonResult(json);
        }
    }
}
