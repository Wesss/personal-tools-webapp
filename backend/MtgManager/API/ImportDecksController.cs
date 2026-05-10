using Microsoft.AspNetCore.Mvc;
using Utils.Logging;
using Utils.Sqlite.ORM;

namespace MtgManager.API
{
    [ApiController]
    [Route("[controller]/[action]")]
    public class ImportDecksController : ControllerBase
    {
        private const string SqlitePath = @"D:\PersonalToolsWebapp\MtgManager.sqlite";
        private readonly ILogger<ImportDecksController> _log;
        private readonly ISqliteORM<ArchidektUser> _orm;

        public ImportDecksController() : this(
            SqliteORM<ArchidektUser>.Get(SqlitePath),
            GlobalLogger.LoggerFactory.CreateLogger<ImportDecksController>()
        )
        {
        }

        internal ImportDecksController(ISqliteORM<ArchidektUser> orm, ILogger<ImportDecksController> log)
        {
            _orm = orm;
            _log = log;
        }

        [HttpGet]
        public JsonResult ArchidektUser()
        {
            try
            {
                var record = _orm.Get("Id = 1").FirstOrDefault();
                return new JsonResult(record?.Username);
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "Failed to retrieve Archidekt user.");
                return new JsonResult(null) { StatusCode = 500 };
            }
        }

        [HttpPost]
        public JsonResult ListDecks([FromBody] string user)
        {
            if (string.IsNullOrWhiteSpace(user))
            {
                return new JsonResult(new { error = "User cannot be empty." }) { StatusCode = 400 };
            }

            // TODO AI move this into a helper method, have helper method throw exception if saving failed. Don't catch it here, just allow it to propagate to user.
            try
            {
                var record = new ArchidektUser
                {
                    Id = 1,
                    Username = user
                };

                _orm.Upsert(record);
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "Failed to save Archidekt user.");
                return new JsonResult(new { error = "Failed to save user." }) { StatusCode = 500 };
            }

            // TODO AI scrape the user's archidekt page (as ArchidektDeck models), check if collection is present, and return this info.
            // this doesn't need to be persisted yet; but it does need to be serialized to user.
            return new JsonResult(new {}) { StatusCode = 200 };
        }
    }
}