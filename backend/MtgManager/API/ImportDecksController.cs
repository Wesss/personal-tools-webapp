using Microsoft.AspNetCore.Mvc;
using Utils.Logging;
using Utils.Sqlite.ORM;

namespace MtgManager.API
{
    [ApiController]
    [Route("[controller]")]
    public class ImportDecksController : ControllerBase
    {
        private const string SqlitePath = @"D:\PersonalToolsWebapp\MtgManager.sqlite";
        private readonly ILogger<ImportDecksController> _log;
        private readonly ISqliteORM<ArchidektUserRecord> _orm;

        public ImportDecksController() : this(
            SqliteORM<ArchidektUserRecord>.Get(SqlitePath),
            GlobalLogger.LoggerFactory.CreateLogger<ImportDecksController>()
        )
        {
        }

        public ImportDecksController(ISqliteORM<ArchidektUserRecord> orm, ILogger<ImportDecksController> log)
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

        [HttpPut]
        public JsonResult ArchidektUser([FromQuery] string user)
        {
            if (string.IsNullOrWhiteSpace(user))
            {
                return new JsonResult(new { error = "User cannot be empty." }) { StatusCode = 400 };
            }

            try
            {
                var record = new ArchidektUserRecord
                {
                    Id = 1,
                    Username = user
                };

                _orm.Upsert(record);
                return new JsonResult(new { success = true, user = record.Username });
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "Failed to save Archidekt user.");
                return new JsonResult(new { error = "Failed to save user." }) { StatusCode = 500 };
            }
        }
    }

    [SqliteTable("ArchidektSettings")]
    public class ArchidektUserRecord : SqliteRow
    {
        // Enforce a unique key so Upsert always targets the same record
        [SqliteColumn(SqliteColumnType.Integer, SqliteNull.NotNull, SqliteUniqueKey.UniqueKey)]
        public int Id { get; set; }

        [SqliteColumn(SqliteColumnType.Text, SqliteNull.NotNull)]
        public string Username { get; set; } = string.Empty;
    }
}