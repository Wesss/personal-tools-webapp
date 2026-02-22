using Dapper;
using System.Collections.Generic;
using Utils.Sqlite.ORM;

namespace Utils.Sqlite.Cache
{
    public class PersistentCache : IDisposable
    {
        private readonly SqliteORM<CacheRow> cacheORM;

        private bool disposedValue;

        public PersistentCache(string path, string group)
        {
            cacheORM = new SqliteORM<CacheRow>(Path.Combine(path, $"PersistentCache-{group}.sqlite"));
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
            // TODO WESD somehow remove out really old keys once in a while? VACCUM the database too while we're at it?
            var args = new Dictionary<string, object>();
            var filter = $"{nameof(CacheRow.CacheKey)} = @key";
            args["@key"] = key;
            if (maxAgeSeconds.HasValue)
            {
                filter += $" and {nameof(CacheRow.DateSet)} >= @dateSet";
                args["@dateSet"] = DateTime.Now.AddSeconds(-1 * maxAgeSeconds.Value);
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
            var upsert = new CacheRow();
            upsert.CacheKey = key;
            upsert.CacheVal = value;
            upsert.DateSet = DateTime.Now;
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