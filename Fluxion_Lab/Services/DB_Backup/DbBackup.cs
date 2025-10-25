using Fluxion_Lab.Classes.DBOperations;
using System.Data.SqlClient;
using System.Diagnostics;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.Extensions.Configuration;
using System.Text;

namespace Fluxion_Lab.Services.DB_Backup
{
    public class DbBackup : IHostedService
    {
        private readonly ILogger<DbBackup> _logger;
        private readonly IConfiguration _configuration;

        // Hardcoded configuration values
        private readonly string _connectionString;
        private readonly string _encryptionPassword = "FS@987";
        private readonly string _sevenZipPath = @"C:\Program Files\7-Zip\7z.exe";

        // Azure Blob Storage configuration
        private readonly string _azureStorageConnectionString;
        private readonly string _containerName = "database-backups";
        private readonly string _clientId;

        private Timer _timer;
        private BlobServiceClient _blobServiceClient;
        private string _backupLogFilePath;

        public DbBackup(ILogger<DbBackup> logger, IConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;

            // Read and decrypt the database connection string from environment variable
            var encryptedConString = Environment.GetEnvironmentVariable("ConStr", EnvironmentVariableTarget.Machine);
            if (string.IsNullOrEmpty(encryptedConString))
            {
                throw new InvalidOperationException("The connection string environment variable 'ConStr' is not set.");
            }

            _connectionString = Fluxion_Handler.DecryptString(encryptedConString, Fluxion_Handler.APIString);

            // Read Azure Storage connection string from environment variable
            //_azureStorageConnectionString = Environment.GetEnvironmentVariable("AzureStorageConnectionString", EnvironmentVariableTarget.Machine);
            //if (string.IsNullOrEmpty(_azureStorageConnectionString))
            //{
            //    throw new InvalidOperationException("The Azure Storage connection string environment variable 'AzureStorageConnectionString' is not set.");
            //}

            // Read Client ID from environment variable
            //_clientId = Environment.GetEnvironmentVariable("ClientId", EnvironmentVariableTarget.Machine);
            //if (string.IsNullOrEmpty(_clientId))
            //{
            //    throw new InvalidOperationException("The Client ID environment variable 'ClientId' is not set.");
            //}

            // Initialize Azure Blob Service Client
            //_blobServiceClient = new BlobServiceClient(_azureStorageConnectionString);
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Backup service starting...");

            // Ensure Azure container exists
            await EnsureContainerExistsAsync();

            // Read backup time from config (e.g., "14:00" for 2 PM)
            string backupTimeStr = _configuration["BackupSchedule:Time"] ?? "17:00"; // fallback to 5 PM
            if (!TimeSpan.TryParse(backupTimeStr, out TimeSpan targetTime))
            {
                targetTime = new TimeSpan(17, 0, 0); // fallback to 5 PM
            }

            TimeSpan currentTime = DateTime.Now.TimeOfDay;
            TimeSpan initialDelay;

            if (currentTime < targetTime)
            {
                initialDelay = targetTime - currentTime;
            }
            else
            {
                initialDelay = (TimeSpan.FromDays(1) - currentTime) + targetTime;
            }

            // Schedule the backup task to run every 24 hours starting at 5 PM
            _timer = new Timer(async (state) => await PerformBackupAsync(state), null, initialDelay, TimeSpan.FromDays(1));

            return;
        }

        private async Task EnsureContainerExistsAsync()
        {
            //try
            //{
            //    var containerClient = _blobServiceClient.GetBlobContainerClient(_containerName);
            //    await containerClient.CreateIfNotExistsAsync(PublicAccessType.None);
            //    _logger.LogInformation($"Azure container '{_containerName}' is ready.");
            //}
            //catch (Exception ex)
            //{
            //    _logger.LogError($"Error ensuring Azure container exists: {ex.Message}");
            //    throw;
            //}
        }

        private async Task<string> GetBackupPathAsync()
        {
            await using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                using (var command = new SqlCommand("SELECT TOP 1 BackupPath FROM mtbl_ClientMaster", connection))
                {
                    var result = await command.ExecuteScalarAsync();
                    if (result == null || result == DBNull.Value)
                        throw new Exception("BackupPath not found in mtbl_ClientMaster.");
                    return result.ToString();
                }
            }
        }

        private void WriteToLogFile(string message, string logLevel = "INFO")
        {
            try
            {
                if (string.IsNullOrEmpty(_backupLogFilePath))
                    return;

                string logEntry = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [{logLevel}] {message}";
                File.AppendAllText(_backupLogFilePath, logEntry + Environment.NewLine);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed to write to log file: {ex.Message}");
            }
        }

        private void InitializeLogFile(string backupDirectory)
        {
            try
            {
                _backupLogFilePath = Path.Combine(backupDirectory, "BackupLog.txt");

                // Create or append to log file
                if (!File.Exists(_backupLogFilePath))
                {
                    File.Create(_backupLogFilePath).Dispose();
                    WriteToLogFile("=== Backup Log File Created ===", "INFO");
                }

                WriteToLogFile("==========================================", "INFO");
                WriteToLogFile("Backup Process Started", "INFO");
                WriteToLogFile("==========================================", "INFO");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed to initialize log file: {ex.Message}");
            }
        }

        private async Task PerformBackupAsync(object state)
        {
            string localBackupFilePath = null;
            string localEncryptedFilePath = null;
            DateTime startTime = DateTime.Now;

            try
            {
                _logger.LogInformation("Starting backup process...");

                // Get backup path first to initialize log file
                string backupFolder = await GetBackupPathAsync();
                InitializeLogFile(backupFolder);

                WriteToLogFile($"Backup process initiated at {startTime:yyyy-MM-dd HH:mm:ss}", "INFO");
                WriteToLogFile($"Backup directory: {backupFolder}", "INFO");

                // Perform the backup and encryption locally
                WriteToLogFile("Creating database backup...", "INFO");
                localBackupFilePath = await PerformDatabaseBackupAsync();
                WriteToLogFile($"Database backup created successfully: {Path.GetFileName(localBackupFilePath)}", "SUCCESS");

                WriteToLogFile("Encrypting backup file...", "INFO");
                localEncryptedFilePath = EncryptBackup(localBackupFilePath);
                WriteToLogFile($"Backup encrypted successfully: {Path.GetFileName(localEncryptedFilePath)}", "SUCCESS");

                // Get file sizes for logging
                long backupSize = new FileInfo(localBackupFilePath).Length;
                long encryptedSize = new FileInfo(localEncryptedFilePath).Length;
                WriteToLogFile($"Backup file size: {FormatFileSize(backupSize)}", "INFO");
                WriteToLogFile($"Encrypted file size: {FormatFileSize(encryptedSize)}", "INFO");

                // Delete the unprotected .bak file immediately after encryption
                if (!string.IsNullOrEmpty(localBackupFilePath) && File.Exists(localBackupFilePath))
                {
                    File.Delete(localBackupFilePath);
                    _logger.LogInformation($"Unprotected backup file deleted after encryption: {localBackupFilePath}");
                    WriteToLogFile($"Unprotected .bak file deleted: {Path.GetFileName(localBackupFilePath)}", "INFO");
                }

                // Delete all previous .7z files except the latest one
                string backupDirectory = Path.GetDirectoryName(localEncryptedFilePath);
                var all7zFiles = Directory.GetFiles(backupDirectory, "*.7z");
                var latest7z = all7zFiles.OrderByDescending(f => File.GetCreationTimeUtc(f)).FirstOrDefault();

                int deletedCount = 0;
                foreach (var file in all7zFiles)
                {
                    if (!string.Equals(file, latest7z, StringComparison.OrdinalIgnoreCase))
                    {
                        File.Delete(file);
                        _logger.LogInformation($"Old encrypted backup file deleted: {file}");
                        WriteToLogFile($"Old encrypted backup deleted: {Path.GetFileName(file)}", "INFO");
                        deletedCount++;
                    }
                }

                if (deletedCount > 0)
                {
                    WriteToLogFile($"Total old backups cleaned up: {deletedCount}", "INFO");
                }

                // Upload to Azure Blob Storage
                //await UploadToAzureBlobAsync(localEncryptedFilePath);

                DateTime endTime = DateTime.Now;
                TimeSpan duration = endTime - startTime;

                _logger.LogInformation($"Backup process completed successfully. File uploaded to Azure Blob Storage.");
                WriteToLogFile("==========================================", "SUCCESS");
                WriteToLogFile($"BACKUP COMPLETED SUCCESSFULLY", "SUCCESS");
                WriteToLogFile($"Duration: {duration.TotalMinutes:F2} minutes ({duration.TotalSeconds:F0} seconds)", "INFO");
                WriteToLogFile($"End time: {endTime:yyyy-MM-dd HH:mm:ss}", "INFO");
                WriteToLogFile($"Latest backup file: {Path.GetFileName(localEncryptedFilePath)}", "INFO");
                WriteToLogFile("==========================================", "SUCCESS");
                WriteToLogFile("", "INFO"); // Empty line for readability
            }
            catch (Exception ex)
            {
                DateTime endTime = DateTime.Now;
                TimeSpan duration = endTime - startTime;

                _logger.LogError($"Error during backup process: {ex.Message}");

                WriteToLogFile("==========================================", "ERROR");
                WriteToLogFile($"BACKUP FAILED", "ERROR");
                WriteToLogFile($"Error: {ex.Message}", "ERROR");
                WriteToLogFile($"Stack Trace: {ex.StackTrace}", "ERROR");
                WriteToLogFile($"Duration before failure: {duration.TotalMinutes:F2} minutes", "ERROR");
                WriteToLogFile($"Failed at: {endTime:yyyy-MM-dd HH:mm:ss}", "ERROR");

                if (!string.IsNullOrEmpty(localBackupFilePath))
                {
                    WriteToLogFile($"Backup file (if created): {Path.GetFileName(localBackupFilePath)}", "ERROR");
                }
                if (!string.IsNullOrEmpty(localEncryptedFilePath))
                {
                    WriteToLogFile($"Encrypted file (if created): {Path.GetFileName(localEncryptedFilePath)}", "ERROR");
                }

                WriteToLogFile("==========================================", "ERROR");
                WriteToLogFile("", "ERROR"); // Empty line for readability
            }
        }

        private string FormatFileSize(long bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB", "TB" };
            double len = bytes;
            int order = 0;
            while (len >= 1024 && order < sizes.Length - 1)
            {
                order++;
                len = len / 1024;
            }
            return $"{len:0.##} {sizes[order]}";
        }

        private async Task<string> PerformDatabaseBackupAsync()
        {
            string backupFolder = await GetBackupPathAsync();
            string timestamp = DateTime.Now.ToString("yyyyMMddHHmmss");
            string backupFilePath = Path.Combine(backupFolder, $"DatabaseBackup_{timestamp}.bak");

            // Ensure the backup folder exists
            if (!Directory.Exists(backupFolder))
            {
                Directory.CreateDirectory(backupFolder);
                WriteToLogFile($"Backup directory created: {backupFolder}", "INFO");
            }

            // Delete all existing .bak files before creating a new one
            var bakFiles = Directory.GetFiles(backupFolder, "*.bak");
            foreach (var bakFile in bakFiles)
            {
                File.Delete(bakFile);
                _logger.LogInformation($"Old backup file deleted before new backup: {bakFile}");
                WriteToLogFile($"Old .bak file deleted: {Path.GetFileName(bakFile)}", "INFO");
            }

            // SQL backup command
            string sqlCommand = $@"
            BACKUP DATABASE [db_Fluxion]
            TO DISK  = N'{backupFilePath}'
             WITH FORMAT, MEDIANAME = 'SQLServerBackups', NAME = 'Full Backup of db_Fluxion';";

            WriteToLogFile("Executing SQL backup command...", "INFO");

            // Execute the SQL command
            await ExecuteSqlCommandAsync(sqlCommand);

            return backupFilePath;
        }

        private async Task ExecuteSqlCommandAsync(string commandText)
        {
            await using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();

                using (var command = new SqlCommand(commandText, connection))
                {
                    command.CommandTimeout = 300; // 5 minutes timeout for backup operations
                    await command.ExecuteNonQueryAsync();
                }
            }
        }

        private string EncryptBackup(string filePath)
        {
            string encryptedFilePath = Path.ChangeExtension(filePath, ".7z");

            WriteToLogFile($"Starting 7-Zip encryption for: {Path.GetFileName(filePath)}", "INFO");

            // 7-Zip command to encrypt the backup file
            string arguments = $"a -p{_encryptionPassword} \"{encryptedFilePath}\" \"{filePath}\"";

            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = _sevenZipPath,
                    Arguments = arguments,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                }
            };

            process.Start();
            string output = process.StandardOutput.ReadToEnd();
            string error = process.StandardError.ReadToEnd();
            process.WaitForExit();

            if (process.ExitCode != 0)
            {
                string errorMessage = string.IsNullOrEmpty(error) ? "Unknown error" : error;
                WriteToLogFile($"7-Zip encryption failed with exit code {process.ExitCode}", "ERROR");
                WriteToLogFile($"7-Zip error: {errorMessage}", "ERROR");
                throw new Exception($"7-Zip encryption failed: {errorMessage}");
            }

            if (!string.IsNullOrEmpty(output))
            {
                WriteToLogFile($"7-Zip output: {output.Substring(0, Math.Min(200, output.Length))}", "INFO");
            }

            return encryptedFilePath;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Backup service stopping...");

            if (!string.IsNullOrEmpty(_backupLogFilePath))
            {
                WriteToLogFile("Backup service stopped", "INFO");
                WriteToLogFile("==========================================", "INFO");
                WriteToLogFile("", "INFO");
            }

            _timer?.Change(Timeout.Infinite, 0);
            _timer?.Dispose();

            return Task.CompletedTask;
        }

        public void Dispose()
        {
            _timer?.Dispose();
        }
    }
}