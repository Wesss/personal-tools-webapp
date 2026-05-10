using Utils.Sqlite.ORM;

namespace MtgManager.API
{
    [SqliteTable("ArchidektDeck")]
    public class ArchidektDeck : SqliteRow
    {
        // Enforce a unique key so Upsert always targets the same record
        [SqliteColumn(SqliteColumnType.Integer, SqliteNull.NotNull, SqliteUniqueKey.UniqueKey)]
        public int Id { get; set; }

        // TODO document somehow that this joins to ArchidektUser
        [SqliteColumn(SqliteColumnType.Integer, SqliteNull.NotNull)]
        public string UserID { get; set; } = string.Empty;
    }
}