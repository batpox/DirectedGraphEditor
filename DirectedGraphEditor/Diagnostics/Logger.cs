
using System;

namespace DirectedGraphEditor.Diagnostics
{
    public sealed class DirectedGraphEventArgs : EventArgs
    {
        public DirectedGraphEventArgs(DirectedGraphLogMessage logMessage)
        {
            LogMessage = logMessage ?? throw new ArgumentNullException(nameof(logMessage));
        }

        public DirectedGraphLogMessage LogMessage { get; }
    }

    public sealed class DirectedGraphLogMessage
    {
        public string LogId { get; set; }

        public DateTimeOffset Timestamp { get; set; }

        public int ThreadId { get; set; }

        public string Source { get; set; }

        public DirectedGraphLogLevel Level { get; set; }

        public string Message { get; set; }

        public Exception Exception { get; set; }

        public override string ToString()
        {
            var result = $"[{Timestamp:O}] [{LogId}] [{ThreadId}] [{Source}] [{Level}]: {Message}";
            if (Exception != null)
            {
                result += Environment.NewLine + Exception;
            }

            return result;
        }
    }

    public enum DirectedGraphLogLevel
    {
        Verbose,

        Info,

        Warning,

        Error
    }
}
