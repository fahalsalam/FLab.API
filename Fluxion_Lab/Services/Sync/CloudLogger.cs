using System;
using System.IO;
using System.Threading.Tasks;

namespace Fluxion_Lab.Services.Sync
{
    public class CloudLogger
    {
        private readonly string _logDirectory;
        private readonly string _logFilePath;

        public CloudLogger(string logDirectory)
        {
            _logDirectory = logDirectory;
            _logFilePath = Path.Combine(_logDirectory, "sync-log.txt");
        }

        public void EnsureDirectory()
        {
            if (!Directory.Exists(_logDirectory))
                Directory.CreateDirectory(_logDirectory);
        }

        public async Task LogAsync(string message)
        {
            EnsureDirectory();
            var line = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {message}{Environment.NewLine}";
            await File.AppendAllTextAsync(_logFilePath, line);
        }
    }
}
