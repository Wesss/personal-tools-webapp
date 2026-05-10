using Utils.Sqlite.ORM;

namespace MtgManager.API
{
    // TODO AI build out a helper class that essentially acts as a global, persisted key value store, saving and fetching values using an orm of AppCacheEntry
    // defined below.

    [SqliteTable("AppCache")]
    public class AppCacheEntry : SqliteRow
    {
        [SqliteColumn(SqliteColumnType.Text, SqliteNull.NotNull, SqliteUniqueKey.UniqueKey)]
        public string Key { get; set; } = string.Empty;

        [SqliteColumn(SqliteColumnType.Text, SqliteNull.NotNull)]
        public string Value { get; set; } = string.Empty;
    }
}