using Microsoft.Extensions.Logging;
using System;

namespace UtilsTests.Sqlite.ORM
{
    public class MSTestLogger<T> : ILogger<T>
    {
        private readonly Action<string> _logAction;

        public MSTestLogger(Action<string> logAction)
        {
            _logAction = logAction ?? throw new ArgumentNullException(nameof(logAction));
        }

        public IDisposable BeginScope<TState>(TState state) where TState : notnull
        {
            return default!;
        }

        // Adjust this if you want to filter out Trace/Debug logs
        public bool IsEnabled(LogLevel logLevel)
        {
            return true;
        }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            if (!IsEnabled(logLevel)) return;

            var message = formatter(state, exception);
            var logEntry = $"[{logLevel}] {typeof(T).Name}: {message}";

            if (exception != null)
            {
                logEntry += Environment.NewLine + exception;
            }

            // Write the formatted log to the MSTest output stream
            _logAction(logEntry);
        }
    }
}