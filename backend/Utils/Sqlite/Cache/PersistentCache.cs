using Dapper;
using System.IO;
using Utils.Sqlite.ORM;

namespace Utils.Sqlite.Cache
{
    public class PersistentCache : IDisposable
    {
        private readonly ISqliteORM<CacheRow> cacheORM;
        private readonly ISqliteORM<SettingsRow> settingsORM;
        private readonly TimeProvider timeProvider;
        private TimeSpan cacheMaxAge;
        private DateTime lastMaintenance = DateTime.MinValue;
        private readonly object maintenanceLock = new object();

        private bool disposedValue;

        public static PersistentCache Get(string path, string group, TimeSpan cacheMaxAge = default)
        {
            var dbPath = Path.Combine(path, $"PersistentCache-{group}.sqlite");
            var cacheORM = SqliteORM<CacheRow>.Get(dbPath);
            var settingsORM = SqliteORM<SettingsRow>.Get(dbPath);
            
            return new PersistentCache(
                cacheORM,
                settingsORM,
                TimeProvider.System
            );
        }

        public PersistentCache(
            ISqliteORM<CacheRow> cacheORM,
            ISqliteORM<SettingsRow> settingsORM,
            TimeProvider timeProvider,
            TimeSpan? cacheMaxAge = null
        )
        {
            this.cacheORM = cacheORM;
            this.settingsORM = settingsORM;
            this.timeProvider = timeProvider;
            this.cacheMaxAge = cacheMaxAge ?? TimeSpan.FromDays(30);
        }

        /// <summary>
        /// Check to run routine mainetnance
        /// </summary>
        public void CheckRunMaintenance()
        {
            var now = timeProvider.GetUtcNow().DateTime;

            if ((now - lastMaintenance) < cacheMaxAge) return;

            lock (maintenanceLock)
            {
                var lastMaintRow = settingsORM.Get("SettingKey = 'LastMaintenance'").FirstOrDefault();
                if (lastMaintRow != null && DateTime.TryParse(lastMaintRow.SettingVal, out DateTime parsedMaint))
                {
                    lastMaintenance = parsedMaint;
                }

                if ((now - lastMaintenance) < cacheMaxAge) return;

                var cutoffDate = now.Subtract(cacheMaxAge);
                cacheORM.Delete("DateSet < @cutoff", new { cutoff = cutoffDate });
                cacheORM.Vacuum();

                lastMaintenance = now;
                settingsORM.Upsert(new SettingsRow
                {
                    SettingKey = "LastMaintenance",
                    SettingVal = now.ToString("O")
                });
            }
        }

        /// <summary>
        /// Gets the value stored at with the given key.
        /// If key is not present or has expired, null is returned.
        /// </summary>
        /// <param name="maxAgeSeconds">
        /// If set, cache will not return values more than this many seconds old
        /// </param>
        public string? Get(string key, int? maxAgeSeconds)
        {
            var args = new Dictionary<string, object>();
            var filter = $"{nameof(CacheRow.CacheKey)} = @key";
            args["@key"] = key;
            if (maxAgeSeconds.HasValue)
            {
                filter += $" and {nameof(CacheRow.DateSetUTC)} >= @dateSet";
                args["@dateSet"] = timeProvider.GetUtcNow().DateTime.AddSeconds(-1 * maxAgeSeconds.Value);
            }
            var row = cacheORM.Get(filter, new DynamicParameters(args)).FirstOrDefault();
            return row?.CacheVal;
        }

        /// <summary>
        /// Sets the given value at the given key
        /// </summary>
        public void Set(string key, string value)
        {
            if (value == null) throw new ArgumentNullException(nameof(value));
            CheckRunMaintenance();

            var upsert = new CacheRow();
            upsert.CacheKey = key;
            upsert.CacheVal = value;
            upsert.DateSetUTC = timeProvider.GetUtcNow().DateTime;
            cacheORM.Upsert(upsert);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // dispose managed state (managed objects)
                }

                // free unmanaged resources (unmanaged objects) and override finalizer
                //if (connection != null && connection.IsValueCreated) connection.Value.Dispose();
                cacheORM?.Dispose();
                // set large fields to null
                disposedValue = true;
            }
        }

        // override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
        ~PersistentCache()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: false);
        }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}