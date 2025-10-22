using Fluxion_Lab.Classes.DBOperations;
using System.Data.SqlClient;
using System.Diagnostics;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.Extensions.Configuration; // Add this for config

namespace Fluxion_Lab.Services.DB_Backup
{
    public class DbBackup : IHostedService
    {
        private readonly ILogger<DbBackup> _logger;
        private readonly IConfiguration _configuration; // Add this for DI

        // Hardcoded configuration values
        private readonly string _connectionString; // Database connection string
        private readonly string _encryptionPassword = "FS@987"; // Encryption password
        private readonly string _sevenZipPath = @"C:\Program Files\7-Zip\7z.exe"; // Path to 7-Zip executable

        // Azure Blob Storage configuration
        private readonly string _azureStorageConnectionString; // Azure Storage connection string
        private readonly string _containerName = "database-backups"; // Container name
        private readonly string _clientId; // Client ID for directory structure

        private Timer _timer;
        private BlobServiceClient _blobServiceClient;

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

        private async Task PerformBackupAsync(object state)
        {
            string localBackupFilePath = null;
            string localEncryptedFilePath = null;

            try
            {
                _logger.LogInformation("Starting backup process...");

                // Perform the backup and encryption locally
                localBackupFilePath = await PerformDatabaseBackupAsync();
                localEncryptedFilePath = EncryptBackup(localBackupFilePath);

                // Delete the unprotected .bak file immediately after encryption
                if (!string.IsNullOrEmpty(localBackupFilePath) && File.Exists(localBackupFilePath))
                {
                    File.Delete(localBackupFilePath);
                    _logger.LogInformation($"Unprotected backup file deleted after encryption: {localBackupFilePath}");
                }

                // Delete all previous .7z files except the latest one
                string backupDirectory = Path.GetDirectoryName(localEncryptedFilePath);
                var all7zFiles = Directory.GetFiles(backupDirectory, "*.7z");
                var latest7z = all7zFiles.OrderByDescending(f => File.GetCreationTimeUtc(f)).FirstOrDefault();
                foreach (var file in all7zFiles)
                {
                    if (!string.Equals(file, latest7z, StringComparison.OrdinalIgnoreCase))
                    {
                        File.Delete(file);
                        _logger.LogInformation($"Old encrypted backup file deleted: {file}");
                    }
                }

                // Upload to Azure Blob Storage
                //await UploadToAzureBlobAsync(localEncryptedFilePath);

                _logger.LogInformation($"Backup process completed successfully. File uploaded to Azure Blob Storage.");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error during backup process: {ex.Message}");
            }
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
            }

            // Delete all existing .bak files before creating a new one
            var bakFiles = Directory.GetFiles(backupFolder, "*.bak");
            foreach (var bakFile in bakFiles)
            {
                File.Delete(bakFile);
                _logger.LogInformation($"Old backup file deleted before new backup: {bakFile}");
            }

            // SQL backup command
            string sqlCommand = $@"
            BACKUP DATABASE [db_Fluxion]
            TO DISK  = N'{backupFilePath}'
            WITH FORMAT, MEDIANAME = 'SQLServerBackups', NAME = 'Full Backup of db_Fluxion';";

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
                    await command.ExecuteNonQueryAsync();
                }
            }
        }

        private string EncryptBackup(string filePath)
        {
            string encryptedFilePath = Path.ChangeExtension(filePath, ".7z");

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
            process.WaitForExit();

            if (process.ExitCode != 0)
            {
                string errorMessage = process.StandardError.ReadToEnd();
                throw new Exception($"7-Zip encryption failed: {errorMessage}");
            }

            return encryptedFilePath;
        }

        //private async Task UploadToAzureBlobAsync(string localFilePath)
        //{
        //    try
        //    {
        //        var containerClient = _blobServiceClient.GetBlobContainerClient(_containerName);

        //        // Create blob name with client ID directory structure
        //        string fileName = Path.GetFileName(localFilePath);
        //        string blobName = $"{_clientId}/{fileName}";

        //        // Check if backup already exists and delete it (overwrite functionality)
        //        await DeleteExistingBackupsAsync(containerClient, _clientId);

        //        // Upload the new backup file
        //        var blobClient = containerClient.GetBlobClient(blobName);

        //        using (var fileStream = File.OpenRead(localFilePath))
        //        {
        //            await blobClient.UploadAsync(fileStream, overwrite: true);
        //        }

        //        _logger.LogInformation($"Backup uploaded successfully to Azure Blob Storage: {blobName}");
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError($"Error uploading backup to Azure Blob Storage: {ex.Message}");
        //        throw;
        //    }
        //}

        private async Task DeleteExistingBackupsAsync(BlobContainerClient containerClient, string clientId)
        {
            try
            {
                // List all blobs in the client's directory
                var blobs = containerClient.GetBlobsAsync(prefix: $"{clientId}/");

                await foreach (var blob in blobs)
                {
                    // Delete existing backup files (they should all be .7z files)
                    if (blob.Name.EndsWith(".7z", StringComparison.OrdinalIgnoreCase))
                    {
                        var blobClient = containerClient.GetBlobClient(blob.Name);
                        await blobClient.DeleteIfExistsAsync();
                        _logger.LogInformation($"Deleted existing backup: {blob.Name}");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"Error deleting existing backups: {ex.Message}");
                // Don't throw here as we still want to upload the new backup
            }
        }

        private void DeleteUnprotectedBackup(string filePath)
        {
            try
            {
                // Get the directory from the file path
                string backupDirectory = Path.GetDirectoryName(filePath);
                if (!string.IsNullOrEmpty(backupDirectory) && Directory.Exists(backupDirectory))
                {
                    // Delete all .bak files in the backup directory
                    var bakFiles = Directory.GetFiles(backupDirectory, "*.bak");
                    foreach (var bakFile in bakFiles)
                    {
                        File.Delete(bakFile);
                        _logger.LogInformation($"Unprotected backup file deleted: {bakFile}");
                    }
                }
                else if (File.Exists(filePath))
                {
                    // Fallback: delete the specific file if directory logic fails
                    File.Delete(filePath);
                    _logger.LogInformation($"Unprotected backup file deleted: {filePath}");
                }
                else
                {
                    _logger.LogWarning($"Unprotected backup file or directory not found for deletion: {filePath}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error deleting unprotected backup files: {ex.Message}");
            }
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Backup service stopping...");

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