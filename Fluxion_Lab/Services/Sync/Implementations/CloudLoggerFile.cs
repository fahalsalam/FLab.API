using System;
using System.IO;
using System.Threading.Tasks;
using Fluxion_Lab.Services.Sync.Interfaces;

namespace Fluxion_Lab.Services.Sync.Implementations
{
    public class CloudLoggerFile : ICloudLogger
    {
        private readonly string _logDirectory;
        private readonly string _logFilePath;
        public CloudLoggerFile(string logDirectory)
        {
            _logDirectory = logDirectory;
            _logFilePath = Path.Combine(_logDirectory, "sync-log.txt");
        }

        public void EnsureDirectory()
        {
            if (!Directory.Exists(_logDirectory))
                Directory.CreateDirectory(_logDirectory);
        }

        public void SetLogDirectory(string path)
        {
            try
            {
                if (!string.IsNullOrEmpty(path))
                {
                    var field = typeof(CloudLoggerFile).GetField("_logDirectory", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    var fileField = typeof(CloudLoggerFile).GetField("_logFilePath", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    field?.SetValue(this, path);
                    fileField?.SetValue(this, Path.Combine(path, "sync-log.txt"));
                }
            }
            catch { }
        }

        public async Task LogAsync(string message)
        {
            EnsureDirectory();
            var line = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {message}{Environment.NewLine}";
            await File.AppendAllTextAsync(_logFilePath, line);
        }
    }
}
