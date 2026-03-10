using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Utils.Logging
{
    public class GlobalLogger
    {
        public static ILoggerFactory LoggerFactory { get; set; } = new NullLoggerFactory();

        public static ILogger<T> Get<T>()
        {
            return LoggerFactory.CreateLogger<T>();
        }
    }
}