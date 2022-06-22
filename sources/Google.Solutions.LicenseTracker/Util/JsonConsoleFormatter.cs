using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging.Console;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;

namespace Google.Solutions.LicenseTracker.Util
{
    public class JsonConsoleFormatter : ConsoleFormatter
    {
        public static readonly string FormatterName = typeof(JsonConsoleFormatter).Name;

        private static readonly IDictionary<LogLevel, string> Severities = new Dictionary<LogLevel, string>
        {
            { LogLevel.Trace, "DEBUG" },
            { LogLevel.Debug, "DEBUG" },
            { LogLevel.Information, "INFO" },
            { LogLevel.Warning, "warning" },
            { LogLevel.Error, "ERROR" },
            { LogLevel.Critical, "CRITICAL" },
        };

        private readonly Options options;

        public JsonConsoleFormatter(IOptionsMonitor<Options> options)
            : base(FormatterName)
        {
            this.options = options.CurrentValue;
        }

        public override void Write<TState>(
            in LogEntry<TState> logEntry,
            IExternalScopeProvider scopeProvider,
            TextWriter textWriter)
        {
            var entry = new StructuredLogEntry()
            {
                Severity = Severities.TryGet(logEntry.LogLevel) ?? "DEFAULT",
                Component = logEntry.Category,
                Message = logEntry.Formatter?.Invoke(logEntry.State, logEntry.Exception) ?? string.Empty
            };

            if (logEntry.Exception != null)
            {
                if (this.options.IncludeStackTraces)
                {
                    entry.Message += $": {logEntry.Exception.FullMessage()}: {logEntry.Exception.Unwrap().StackTrace}";
                }
                else
                {
                    entry.Message += $": {logEntry.Exception.FullMessage()}";
                }
            }

            textWriter.WriteLine(entry.ToString());
        }


        /// <summary>
        /// Structured log entry that complies with the Cloud Run conventions, see
        /// https://cloud.google.com/run/docs/logging#writing_structured_logs.
        /// </summary>
        public class StructuredLogEntry
        {
            [JsonProperty("message")]
            public string? Message { get; set; }

            [JsonProperty("severity")]
            public string? Severity { get; set; }

            [JsonProperty("component")]
            public string? Component { get; set; }

            public override string ToString()
            {
                return JsonConvert.SerializeObject(this);
            }
        }

        public class Options : ConsoleFormatterOptions
        {
            public bool IncludeStackTraces { get; set; }
        }
    }
}
