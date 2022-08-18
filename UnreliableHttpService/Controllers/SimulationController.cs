using Microsoft.AspNetCore.Mvc;

namespace UnreliableHttpService.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SimulationController : ControllerBase
    {
        static int RequestCount = 0;
        static int StandardDelay = 100;
        static int IntermittentBadMod = 3;

        [HttpGet("intermittent")]
        public async Task<IActionResult> MostlyBad()
        {
            RequestCount++;
            await Task.Delay(StandardDelay);
            if (RequestCount % IntermittentBadMod != 0)
            {
                throw new Exception("Intermittently Bad!");
            }
            return Ok();
        }

        [HttpGet("updown")]
        public async Task<IActionResult> UpAndDown()
        {
            RequestCount++;
            await Task.Delay(StandardDelay);
            if (DateTime.Now.Minute % 2 == 0)
            {
                throw new Exception("Up And Down!");
            }
            return Ok();
        }

        [HttpGet("badrequest")]
        public async Task<IActionResult> Mostly4xx()
        {
            RequestCount++;
            await Task.Delay(StandardDelay);
            if (RequestCount % IntermittentBadMod != 0)
            {
                return BadRequest("Nope!");
            }
            return Ok();
        }
    }
}
