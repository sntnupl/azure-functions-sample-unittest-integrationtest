using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;

namespace InvoiceProcessor.Tests.Unit.TestCommon
{
    public enum LoggerTypes
    {
        Null,
        List
    }

    public class ListLogger : ILogger
    {
        public IList<string> Logs;

        public IDisposable BeginScope<TState>(TState state) => NullScope.Instance;

        public bool IsEnabled(LogLevel logLevel) => false;

        public ListLogger()
        {
            this.Logs = new List<string>();
        }

        public void Log<TState>(LogLevel logLevel,
                                EventId eventId,
                                TState state,
                                Exception exception,
                                Func<TState, Exception, string> formatter)
        {
            string message = formatter(state, exception);
            this.Logs.Add($"{LogLevelLabel(logLevel)} {message}");
        }

        private string LogLevelLabel(LogLevel logLevel)
        {
            return logLevel switch {
                LogLevel.Trace => "[TRACE]",
                LogLevel.Debug => "[DEBUG]",
                LogLevel.Information => "[INFORMATION]",
                LogLevel.Warning => "[WARNING]",
                LogLevel.Error => "[ERROR]",
                LogLevel.Critical => "[CRITICAL]",
                _ => string.Empty,
            };
        }
    }
}
