using Microsoft.AspNetCore.Mvc;

namespace MtgManager.API
{
    [ApiController]
    [Route("[controller]")]
    public class ImportDecksController : ControllerBase
    {
        private readonly ILogger<ImportDecksController> _logger;

        public ImportDecksController(ILogger<ImportDecksController> logger)
        {
            _logger = logger;
        }

        [HttpPost]
        public JsonResult Post([FromBody] string deckText)
        {
            if (string.IsNullOrEmpty(deckText))
            {
                // TODO WESD return some kind of error
            }
            var rand = new Random();
            return new JsonResult(deckText + rand.Next(0, 10));
        }
    }
}
