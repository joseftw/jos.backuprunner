using Serilog.Events;

namespace JOS.BackupRunner.Infrastructure.Logging
{
    public class LoggingOverride
    {
        public string Path { get; set; }
        public LogEventLevel Level { get; set; }
    }
}
