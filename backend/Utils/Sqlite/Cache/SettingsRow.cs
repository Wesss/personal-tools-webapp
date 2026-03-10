using Utils.Sqlite.ORM;

namespace Utils.Sqlite.Cache
{
    [SqliteTable("Settings")]
    public class SettingsRow : SqliteRow
    {
        [SqliteColumn(SqliteColumnType.Text, SqliteNull.NotNull, SqliteUniqueKey.UniqueKey)]
        public string? SettingKey { get; set; }

        [SqliteColumn(SqliteColumnType.Text, SqliteNull.NotNull)]
        public string? SettingVal { get; set; }
    }
}
