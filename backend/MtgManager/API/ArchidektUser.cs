using Utils.Sqlite.ORM;

namespace MtgManager.API
{
    [SqliteTable("ArchidektUser")]
    public class ArchidektUser : SqliteRow
    {
        // Enforce a unique key so Upsert always targets the same record
        [SqliteColumn(SqliteColumnType.Integer, SqliteNull.NotNull, SqliteUniqueKey.UniqueKey)]
        public int Id { get; set; }

        [SqliteColumn(SqliteColumnType.Text, SqliteNull.NotNull)]
        public string Username { get; set; } = string.Empty;
    }
}