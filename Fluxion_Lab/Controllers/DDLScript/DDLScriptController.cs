using Azure.Storage.Blobs;
using Dapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics; // Added for system restart
using Microsoft.Extensions.Configuration; // Added for IConfiguration
using Fluxion_Lab.Classes.DBOperations;

namespace Fluxion_Lab.Controllers
{
    [Route("api/7890")]
    public class DDLScriptController : ControllerBase
    {
        private readonly string _connectionString;
        private readonly string _blobConnectionString;
        private readonly string _blobContainerName;
        private readonly string _blobFolderName = "SQL_Scripts";
        private readonly string _staticLocalPath = @"D:\Fluxion_Projects\Fluxion_Flab\Fluxion_Lab\bin\Release\net8.0\publish";
        private readonly string _staticExePath = @"D:\Fluxion_Projects\Fluxion_Flab\Fluxion_Lab\bin\Release\net8.0\publish";
        private readonly IConfiguration _configuration;

        // Define all event types to process
        private readonly List<string> _eventTypes = new List<string>
        {
            "CREATE_TABLE",
            "ALTER_TABLE",
            "CREATE_PROCEDURE",
            "ALTER_PROCEDURE",
            "CREATE_TRIGGER",
            "ALTER_TRIGGER",
            "CREATE_SCHEMA"
        };

        public DDLScriptController(IConfiguration configuration)
        {
            _configuration = configuration;
            _connectionString = "Server=localhost;Database=db_Fluxion_Dev;User Id=FS;Password=Fluxion@FS@987;Encrypt=True;TrustServerCertificate=True;";
            _blobConnectionString = configuration["AzureStorage:ConnectionString"];
            _blobContainerName = configuration["AzureStorage:ContainerName"];
        }

        /************** TEST BACKUP ENDPOINT - FOR TESTING PURPOSES **************/
        [AllowAnonymous]
        [HttpPost("testBackup")]
        public async Task<IActionResult> TestBackup()
        {
            string localBackupFilePath = null;
            string localEncryptedFilePath = null;
            string backupLogFilePath = null;
            DateTime startTime = DateTime.Now;
            List<string> logEntries = new List<string>();

            try
            {
                // Get encrypted connection string
                var encryptedConString = Environment.GetEnvironmentVariable("ConStr", EnvironmentVariableTarget.Machine);
                if (string.IsNullOrEmpty(encryptedConString))
                {
                    return BadRequest(new { message = "The connection string environment variable 'ConStr' is not set." });
                }

                string connectionString = Fluxion_Handler.DecryptString(encryptedConString, Fluxion_Handler.APIString);

                // Configuration
                string encryptionPassword = "FS@987";
                string sevenZipPath = @"C:\Program Files\7-Zip\7z.exe";

                logEntries.Add($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [INFO] ==========================================");
                logEntries.Add($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [INFO] TEST Backup Process Started");
                logEntries.Add($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [INFO] ==========================================");

                // Use hardcoded backup path for testing
                string backupFolder = @"D:\DBBackup";
                logEntries.Add($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [INFO] Using test backup directory: {backupFolder}");

                // Initialize log file
                backupLogFilePath = Path.Combine(backupFolder, "TestBackupLog.txt");

                // Ensure directory exists
                if (!Directory.Exists(backupFolder))
                {
                    Directory.CreateDirectory(backupFolder);
                    logEntries.Add($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [INFO] Backup directory created: {backupFolder}");
                }
                else
                {
                    logEntries.Add($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [INFO] Backup directory already exists: {backupFolder}");
                }

                // Create database backup
                logEntries.Add($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [INFO] Creating database backup...");
                string timestamp = DateTime.Now.ToString("yyyyMMddHHmmss");
                localBackupFilePath = Path.Combine(backupFolder, $"TestBackup_{timestamp}.bak");

                // Delete old .bak files
                var bakFiles = Directory.GetFiles(backupFolder, "*.bak");
                if (bakFiles.Length > 0)
                {
                    logEntries.Add($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [INFO] Found {bakFiles.Length} old .bak file(s) to delete");
                    foreach (var bakFile in bakFiles)
                    {
                        System.IO.File.Delete(bakFile);
                        logEntries.Add($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [INFO] Old .bak file deleted: {Path.GetFileName(bakFile)}");
                    }
                }
                else
                {
                    logEntries.Add($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [INFO] No old .bak files found");
                }

                // SQL backup command
                string sqlCommand = $@"
                   BACKUP DATABASE [db_Fluxion]
                    TO DISK = N'{localBackupFilePath}'
                    WITH FORMAT, MEDIANAME = 'SQLServerBackups', NAME = 'Full Backup of db_Fluxion';";

                logEntries.Add($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [INFO] Executing SQL backup command...");
                logEntries.Add($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [INFO] Target file: {Path.GetFileName(localBackupFilePath)}");

                await using (var connection = new SqlConnection(connectionString))
                {
                    await connection.OpenAsync();
                    logEntries.Add($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [INFO] Database connection opened successfully");

                    using (var command = new SqlCommand(sqlCommand, connection))
                    {
                        command.CommandTimeout = 300; // 5 minutes timeout
                        await command.ExecuteNonQueryAsync();
                    }
                }

                logEntries.Add($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [SUCCESS] Database backup created successfully: {Path.GetFileName(localBackupFilePath)}");

                // Get backup file size
                long backupSize = new FileInfo(localBackupFilePath).Length;
                logEntries.Add($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [INFO] Backup file size: {FormatFileSizeTest(backupSize)}");

                // Verify 7-Zip exists
                if (!System.IO.File.Exists(sevenZipPath))
                {
                    logEntries.Add($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [ERROR] 7-Zip not found at: {sevenZipPath}");
                    throw new Exception($"7-Zip executable not found at: {sevenZipPath}");
                }

                // Encrypt backup
                logEntries.Add($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [INFO] Encrypting backup file using 7-Zip...");
                localEncryptedFilePath = Path.ChangeExtension(localBackupFilePath, ".7z");
                logEntries.Add($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [INFO] Target encrypted file: {Path.GetFileName(localEncryptedFilePath)}");

                string arguments = $"a -p{encryptionPassword} \"{localEncryptedFilePath}\" \"{localBackupFilePath}\"";

                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = sevenZipPath,
                        Arguments = arguments,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        CreateNoWindow = true,
                    }
                };

                logEntries.Add($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [INFO] Starting 7-Zip process...");
                process.Start();
                string output = process.StandardOutput.ReadToEnd();
                string error = process.StandardError.ReadToEnd();
                process.WaitForExit();

                if (process.ExitCode != 0)
                {
                    string errorMessage = string.IsNullOrEmpty(error) ? "Unknown error" : error;
                    logEntries.Add($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [ERROR] 7-Zip encryption failed with exit code {process.ExitCode}");
                    logEntries.Add($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [ERROR] 7-Zip error: {errorMessage}");
                    throw new Exception($"7-Zip encryption failed: {errorMessage}");
                }

                logEntries.Add($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [SUCCESS] Backup encrypted successfully: {Path.GetFileName(localEncryptedFilePath)}");

                // Get encrypted file size
                long encryptedSize = new FileInfo(localEncryptedFilePath).Length;
                logEntries.Add($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [INFO] Encrypted file size: {FormatFileSizeTest(encryptedSize)}");
                logEntries.Add($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [INFO] Compression ratio: {((1 - (double)encryptedSize / backupSize) * 100):F2}%");

                // Delete unprotected .bak file
                if (System.IO.File.Exists(localBackupFilePath))
                {
                    System.IO.File.Delete(localBackupFilePath);
                    logEntries.Add($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [INFO] Unprotected .bak file deleted: {Path.GetFileName(localBackupFilePath)}");
                }

                // Clean up old .7z files except latest
                var all7zFiles = Directory.GetFiles(backupFolder, "*.7z");
                logEntries.Add($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [INFO] Found {all7zFiles.Length} .7z file(s) in backup directory");

                var latest7z = all7zFiles.OrderByDescending(f => System.IO.File.GetCreationTimeUtc(f)).FirstOrDefault();

                int deletedCount = 0;
                foreach (var file in all7zFiles)
                {
                    if (!string.Equals(file, latest7z, StringComparison.OrdinalIgnoreCase))
                    {
                        System.IO.File.Delete(file);
                        logEntries.Add($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [INFO] Old encrypted backup deleted: {Path.GetFileName(file)}");
                        deletedCount++;
                    }
                }

                if (deletedCount > 0)
                {
                    logEntries.Add($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [INFO] Total old backups cleaned up: {deletedCount}");
                }
                else
                {
                    logEntries.Add($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [INFO] No old backups to clean up");
                }

                DateTime endTime = DateTime.Now;
                TimeSpan duration = endTime - startTime;

                logEntries.Add($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [SUCCESS] ==========================================");
                logEntries.Add($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [SUCCESS] TEST BACKUP COMPLETED SUCCESSFULLY");
                logEntries.Add($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [INFO] Duration: {duration.TotalMinutes:F2} minutes ({duration.TotalSeconds:F0} seconds)");
                logEntries.Add($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [INFO] End time: {endTime:yyyy-MM-dd HH:mm:ss}");
                logEntries.Add($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [INFO] Latest backup file: {Path.GetFileName(localEncryptedFilePath)}");
                logEntries.Add($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [INFO] Log file location: {backupLogFilePath}");
                logEntries.Add($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [SUCCESS] ==========================================");
                logEntries.Add("");

                // Write to log file
                System.IO.File.AppendAllLines(backupLogFilePath, logEntries);
                logEntries.Add($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [INFO] Log file written successfully");

                return Ok(new
                {
                    success = true,
                    message = "Test backup completed successfully!",
                    backupFile = Path.GetFileName(localEncryptedFilePath),
                    backupSize = FormatFileSizeTest(backupSize),
                    encryptedSize = FormatFileSizeTest(encryptedSize),
                    compressionRatio = $"{((1 - (double)encryptedSize / backupSize) * 100):F2}%",

                    duration = $"{duration.TotalMinutes:F2} minutes",
                    backupPath = backupFolder,
                    logFile = backupLogFilePath,
                    oldBackupsDeleted = deletedCount,
                    logs = logEntries
                });
            }
            catch (Exception ex)
            {
                DateTime endTime = DateTime.Now;
                TimeSpan duration = endTime - startTime;

                logEntries.Add($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [ERROR] ==========================================");
                logEntries.Add($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [ERROR] TEST BACKUP FAILED");
                logEntries.Add($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [ERROR] Error: {ex.Message}");
                logEntries.Add($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [ERROR] Stack Trace: {ex.StackTrace}");
                logEntries.Add($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [ERROR] Duration before failure: {duration.TotalMinutes:F2} minutes");
                logEntries.Add($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [ERROR] Failed at: {endTime:yyyy-MM-dd HH:mm:ss}");

                if (!string.IsNullOrEmpty(localBackupFilePath))
                {
                    logEntries.Add($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [ERROR] Backup file (if created): {Path.GetFileName(localBackupFilePath)}");
                }
                if (!string.IsNullOrEmpty(localEncryptedFilePath))
                {
                    logEntries.Add($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [ERROR] Encrypted file (if created): {Path.GetFileName(localEncryptedFilePath)}");
                }

                logEntries.Add($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [ERROR] ==========================================");
                logEntries.Add("");

                // Write to log file even on failure
                if (!string.IsNullOrEmpty(backupLogFilePath))
                {
                    try
                    {
                        System.IO.File.AppendAllLines(backupLogFilePath, logEntries);
                        logEntries.Add($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [INFO] Error log written successfully");
                    }
                    catch (Exception logEx)
                    {
                        logEntries.Add($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [ERROR] Failed to write error log: {logEx.Message}");
                    }
                }

                return BadRequest(new
                {
                    success = false,
                    message = "Test backup failed!",
                    error = ex.Message,
                    stackTrace = ex.StackTrace,
                    duration = $"{duration.TotalMinutes:F2} minutes",
                    backupPath = @"D:\DBBackup",
                    logFile = backupLogFilePath,
                    logs = logEntries
                });
            }
        }

        private async Task<string> GetBackupPathFromDb(string connectionString)
        {
            await using (var connection = new SqlConnection(connectionString))
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

        private string FormatFileSizeTest(long bytes)
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
        /******************* END TEST BACKUP ENDPOINT *******************/

        [AllowAnonymous]
        [HttpPost("generate")]
        public async Task<IActionResult> GenerateDDLScript([FromBody] DDLRequest request)
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    connection.Open();

                    // Generate consolidated script for all event types
                    var consolidatedScript = await GenerateCompleteScript(connection, request.FilterDate);

                    if (request.UploadedToCloud)
                    {
                        await UploadScriptToBlobAsync(consolidatedScript, "Fluxion_DB.sql");
                    }

                    // Return the script as plain text
                    return Ok(consolidatedScript);
                }
            }
            catch (Exception ex)
            {
                return BadRequest(new
                {
                    message = "An error occurred while generating the DDL script.",
                    error = ex.Message
                });
            }
        }

        [AllowAnonymous]
        [HttpPost("uploadApiFiles")]
        public async Task<IActionResult> UploadApiFiles()
        {
            string staticFolderPath = @"D:\Fluxion_Projects\Fluxion_Flab\Fluxion_Lab\bin\Release\net8.0\publish";
            string blobFolderName = "api-files";
            string zipBlobName = "api-files.zip";

            if (!Directory.Exists(staticFolderPath))
            {
                return BadRequest(new { message = "Static publish folder does not exist." });
            }

            try
            {
                var blobServiceClient = new BlobServiceClient(_blobConnectionString);
                var containerClient = blobServiceClient.GetBlobContainerClient(_blobContainerName);

                using (var memoryStream = new MemoryStream())
                {
                    using (var archive = new ZipArchive(memoryStream, ZipArchiveMode.Create, true))
                    {
                        var allFiles = Directory.GetFiles(_staticLocalPath, "*", SearchOption.AllDirectories);
                        foreach (var filePath in allFiles)
                        {
                            var relativePath = Path.GetRelativePath(_staticLocalPath, filePath).Replace("\\", "/");
                            var zipEntry = archive.CreateEntry(relativePath, CompressionLevel.Optimal);
                            using (var entryStream = zipEntry.Open())
                            using (var fileStream = System.IO.File.OpenRead(filePath))
                            {
                                await fileStream.CopyToAsync(entryStream);
                            }
                        }
                    }

                    memoryStream.Position = 0;
                    var blobClient = containerClient.GetBlobClient(zipBlobName);
                    await blobClient.UploadAsync(memoryStream, overwrite: true);
                }

                return Ok(new { message = $"Successfully uploaded as '{zipBlobName}' to container '{_blobContainerName}'." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = "Failed to zip and upload publish folder to blob storage.", error = ex.Message });
            }
        }

        [AllowAnonymous]
        [HttpGet("downloadApiZip")]
        public async Task<IActionResult> DownloadApiZip()
        {
            try
            {
                var blobServiceClient = new BlobServiceClient(_blobConnectionString);
                var containerClient = blobServiceClient.GetBlobContainerClient(_blobContainerName);

                var blobClient = containerClient.GetBlobClient("api-files.zip");

                if (await blobClient.ExistsAsync())
                {
                    var blobDownloadInfo = await blobClient.DownloadAsync();

                    return File(
                        blobDownloadInfo.Value.Content,
                        "application/zip",
                        "api-files.zip"
                    );
                }
                else
                {
                    return NotFound(new { message = "The file 'api-files.zip' does not exist in blob storage." });
                }
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = "Failed to download API ZIP from blob storage.", error = ex.Message });
            }
        }

        [AllowAnonymous]
        [HttpGet("downloadDataSyncApiZip")]
        public async Task<IActionResult> downloadDataSyncApiZip()
        {
            try
            {
                var blobServiceClient = new BlobServiceClient(_blobConnectionString);
                var containerClient = blobServiceClient.GetBlobContainerClient(_blobContainerName);

                var blobClient = containerClient.GetBlobClient("data-sync.zip");

                if (await blobClient.ExistsAsync())
                {
                    var blobDownloadInfo = await blobClient.DownloadAsync();

                    return File(
                        blobDownloadInfo.Value.Content,
                        "application/zip",
                        "data-sync.zip"
                    );
                }
                else
                {
                    return NotFound(new { message = "The file 'data-sync.zip' does not exist in blob storage." });
                }
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = "Failed to download API ZIP from blob storage.", error = ex.Message });
            }
        }

        [AllowAnonymous]
        [HttpPost("restartSystem")]
        public IActionResult RestartSystem([FromBody] RestartRequest request)
        {
            // Replace with a secure, strong secret in production
            const string secretKey = "User@987";

            if (request == null || string.IsNullOrWhiteSpace(request.SecretKey))
            {
                return BadRequest(new { message = "Secret key is required." });
            }

            if (request.SecretKey != secretKey)
            {
                return Unauthorized(new { message = "Invalid secret key." });
            }

            try
            {
                // Windows: Restart the system
                Process.Start(new ProcessStartInfo
                {
                    FileName = "shutdown",
                    Arguments = "/r /t 0",
                    CreateNoWindow = true,
                    UseShellExecute = false
                });

                return Ok(new { message = "System restart initiated." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Failed to restart system.", error = ex.Message });
            }
        }

        private async Task<string> GenerateCompleteScript(SqlConnection connection, DateTime filterDate)
        {
            StringBuilder completeScript = new StringBuilder();
            StringBuilder allScripts = new StringBuilder();

            // Add single transaction header
            completeScript.AppendLine("BEGIN TRY");
            completeScript.AppendLine("BEGIN TRANSACTION;");
            completeScript.AppendLine("DECLARE @ErrorIdentFlag NVARCHAR(MAX) = NULL;");
            completeScript.AppendLine();

            // Process each event type and collect scripts
            foreach (var eventType in _eventTypes)
            {
                var eventScript = await GenerateScriptForEventType(connection, eventType, filterDate);

                if (!string.IsNullOrWhiteSpace(eventScript))
                {
                    allScripts.AppendLine($"-- ========== {eventType} Scripts ==========");
                    allScripts.AppendLine(eventScript);
                    allScripts.AppendLine();
                }
            }

            // Add all scripts
            completeScript.Append(allScripts);

            // Add single transaction footer
            completeScript.AppendLine("COMMIT TRANSACTION;");
            completeScript.AppendLine("PRINT 'Transaction committed successfully.';");
            completeScript.AppendLine("END TRY");
            completeScript.AppendLine("BEGIN CATCH");
            completeScript.AppendLine("ROLLBACK TRANSACTION;");
            completeScript.AppendLine("PRINT 'Error encountered. Transaction rolled back.';");
            completeScript.AppendLine("PRINT 'Error Message: ' + @ErrorIdentFlag + ' - Error Message: ' + ERROR_MESSAGE();");
            completeScript.AppendLine("PRINT 'Error Severity: ' + CAST(ERROR_SEVERITY() AS NVARCHAR(10));");
            completeScript.AppendLine("PRINT 'Error State: ' + CAST(ERROR_STATE() AS NVARCHAR(10));");
            completeScript.AppendLine("PRINT 'Error Line: ' + CAST(ERROR_LINE() AS NVARCHAR(10));");
            completeScript.AppendLine("END CATCH;");

            return completeScript.ToString();
        }

        private async Task<string> GenerateScriptForEventType(SqlConnection connection, string eventType, DateTime filterDate)
        {
            // Special handling for ALTER_TABLE
            if (eventType == "ALTER_TABLE")
            {
                return await GenerateAlterTableScripts(filterDate);
            }

            // Handle other event types
            return await GenerateObjectScripts(connection, eventType, filterDate);
        }

        private async Task<string> GenerateAlterTableScripts(DateTime filterDate)
        {
            var query = @"
                SELECT e.ObjectName, e.EventDDL
                FROM [Fluxion_Internal].[sstbl_DDLEvents] e
                WHERE e.EventType = 'ALTER_TABLE'
                    AND e.EventDate >= @FilterDate
                    AND e.SchemaName <> 'Fluxion_Internal'
                    AND NOT EXISTS (
                        SELECT 1
                        FROM [Fluxion_Internal].[sstbl_DDLEvents] c
                        WHERE c.ObjectName = e.ObjectName
                            AND c.EventType = 'CREATE_TABLE'
                            AND c.EventDate >= @FilterDate
                    )";

            using (var connection = new SqlConnection(_connectionString))
            {
                var ddlEvents = await connection.QueryAsync<DDLEventAlter>(
                    query,
                    new { FilterDate = filterDate }
                );

                StringBuilder consolidatedScripts = new StringBuilder();

                foreach (var ddlEvent in ddlEvents)
                {
                    if (!string.IsNullOrEmpty(ddlEvent.EventDDL))
                    {
                        consolidatedScripts.AppendLine($"SET @ErrorIdentFlag = 'Executing table script for {ddlEvent.ObjectName}';");
                        consolidatedScripts.AppendLine($"EXEC sp_executesql N'{ddlEvent.EventDDL.Replace("'", "''")}';");
                    }
                }

                return consolidatedScripts.ToString();
            }
        }

        private async Task<string> GenerateObjectScripts(SqlConnection connection, string eventType, DateTime filterDate)
        {
            var query = @"
                SELECT DISTINCT ObjectName, 
                CASE 
                    WHEN @EventType LIKE '%TABLE%' THEN 'TABLE' 
                    WHEN @EventType LIKE '%PROCEDURE%' THEN 'PROCEDURE' 
                    WHEN @EventType LIKE '%TRIGGER%' THEN 'TRIGGER' 
                END AS ObjectType 
                FROM [Fluxion_Internal].[sstbl_DDLEvents] 
                WHERE EventType = @EventType 
                    AND EventDate >= @FilterDate 
                    AND SchemaName <> 'Fluxion_Internal'";

            using (var conn = new SqlConnection(_connectionString))
            {
                var ddlEvents = await conn.QueryAsync<DDLEvent>(
                    query,
                    new { EventType = eventType, FilterDate = filterDate }
                );

                StringBuilder consolidatedScripts = new StringBuilder();

                foreach (var ddlEvent in ddlEvents)
                {
                    string singleDDL = await GenerateSingleObjectScript(
                        connection,
                        ddlEvent.ObjectName,
                        ddlEvent.ObjectType,
                        eventType,
                        filterDate
                    );

                    if (!string.IsNullOrEmpty(singleDDL))
                    {
                        consolidatedScripts.AppendLine($"SET @ErrorIdentFlag = 'Executing {ddlEvent.ObjectType.ToLower()} script for {ddlEvent.ObjectName}';");
                        consolidatedScripts.AppendLine($"EXEC sp_executesql N'{singleDDL.Replace("'", "''")}';");
                    }
                }

                return consolidatedScripts.ToString();
            }
        }

        private async Task<string> GenerateSingleObjectScript(
            SqlConnection connection,
            string objectName,
            string objectType,
            string eventType,
            DateTime filterDate)
        {
            switch (objectType)
            {
                case "TABLE":
                    return await GenerateTableScript(connection, objectName, eventType == "ALTER_TABLE", filterDate);
                case "PROCEDURE":
                    return await GenerateProcedureScript(connection, objectName, eventType == "ALTER_PROCEDURE");
                case "TRIGGER":
                    return await GenerateTriggerScript(connection, objectName, eventType == "ALTER_TRIGGER");
                default:
                    return string.Empty;
            }
        }

        private async Task<string> GenerateTableScript(SqlConnection connection, string tableName, bool isAlter, DateTime filterDate)
        {
            if (isAlter)
            {
                using (var conn = new SqlConnection(_connectionString))
                {
                    var alterScript = await conn.QueryFirstOrDefaultAsync<string>(
                        @"SELECT EventDDL 
                          FROM [Fluxion_Internal].[sstbl_DDLEvents] 
                          WHERE EventType = 'ALTER_TABLE' 
                            AND ObjectName = @TableName 
                            AND EventDate >= @FilterDate",
                        new { TableName = tableName, FilterDate = filterDate }
                    );
                    return alterScript;
                }
            }
            else
            {
                using (var conn = new SqlConnection(_connectionString))
                {
                    var createScript = await conn.QueryFirstOrDefaultAsync<string>(
                        @"SELECT 'CREATE TABLE ' + QUOTENAME(s.name) + '.' + QUOTENAME(t.name) + ' (' + CHAR(13) + 
                        STUFF((SELECT ',' + CHAR(13) + ' ' + QUOTENAME(c.name) + ' ' + 
                        CASE WHEN c.system_type_id IN (231, 167) THEN UPPER(ty.name) + '(' + 
                        CASE WHEN c.max_length = -1 THEN 'MAX' ELSE CAST(c.max_length AS NVARCHAR) END + ')' 
                        WHEN c.system_type_id = 106 THEN UPPER(ty.name) + '(' + CAST(c.precision AS NVARCHAR) + ',' + CAST(c.scale AS NVARCHAR) + ')' 
                        ELSE UPPER(ty.name) 
                        END + 
                        CASE WHEN c.is_identity = 1 THEN ' IDENTITY(1,1) ' ELSE '' END + 
                        CASE WHEN c.is_nullable = 1 THEN ' NULL' ELSE ' NOT NULL' END + 
                        CASE WHEN c.default_object_id <> 0 THEN ' DEFAULT ' + OBJECT_DEFINITION(c.default_object_id) ELSE '' END 
                        FROM sys.columns AS c 
                        INNER JOIN sys.types AS ty ON c.user_type_id = ty.user_type_id 
                        WHERE c.object_id = t.object_id 
                        FOR XML PATH(''), TYPE).value('.', 'NVARCHAR(MAX)'), 1, 2, '') + CHAR(13) + 
                        ISNULL((SELECT ',' + CHAR(13) + ' CONSTRAINT ' + QUOTENAME(k.name) + ' PRIMARY KEY (' + 
                        STUFF((SELECT ',' + QUOTENAME(c.name) 
                        FROM sys.index_columns AS ic 
                        INNER JOIN sys.columns AS c ON ic.object_id = c.object_id AND ic.column_id = c.column_id 
                        WHERE ic.object_id = k.parent_object_id AND ic.index_id = k.unique_index_id 
                        ORDER BY ic.key_ordinal 
                        FOR XML PATH(''), TYPE).value('.', 'NVARCHAR(MAX)'), 1, 1, '') + ') ' 
                        FROM sys.key_constraints AS k 
                        WHERE k.parent_object_id = t.object_id AND k.type = 'PK'), '') + CHAR(13) + ')' 
                        FROM sys.tables AS t 
                        INNER JOIN sys.schemas AS s ON t.schema_id = s.schema_id 
                        WHERE t.name = @TableName",
                        new { TableName = tableName }
                    );
                    return createScript;
                }
            }
        }

        private async Task<string> GenerateProcedureScript(SqlConnection connection, string procedureName, bool isAlter)
        {
            using (var conn = new SqlConnection(_connectionString))
            {
                var script = await conn.QueryFirstOrDefaultAsync<string>(
                    "SELECT OBJECT_DEFINITION(OBJECT_ID(@ProcedureName))",
                    new { ProcedureName = procedureName }
                );

                if (isAlter && script != null)
                {
                    script = script.Replace("CREATE PROCEDURE", "ALTER PROCEDURE");
                }

                return script;
            }
        }

        private async Task<string> GenerateTriggerScript(SqlConnection connection, string triggerName, bool isAlter)
        {
            using (var conn = new SqlConnection(_connectionString))
            {
                var script = await conn.QueryFirstOrDefaultAsync<string>(
                    "SELECT OBJECT_DEFINITION(OBJECT_ID(@TriggerName))",
                    new { TriggerName = triggerName }
                );

                if (isAlter && script != null)
                {
                    script = script.Replace("CREATE TRIGGER", "ALTER TRIGGER");
                }

                return script;
            }
        }

        private async Task UploadScriptToBlobAsync(string scriptContent, string fileName = "Fluxion_DB.sql")
        {
            try
            {
                var blobServiceClient = new BlobServiceClient(_blobConnectionString);
                var containerClient = blobServiceClient.GetBlobContainerClient(_blobContainerName);

                // Point to exact blob file
                var blobClient = containerClient.GetBlobClient($"{_blobFolderName}/{fileName}");

                using (var stream = new MemoryStream(Encoding.UTF8.GetBytes(scriptContent)))
                {
                    await blobClient.UploadAsync(stream, overwrite: true); // <-- Force overwrite
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Blob upload failed: " + ex.Message);
            }
        }

        // Simplified request class with only FilterDate
        public class DDLRequest
        {
            public DateTime FilterDate { get; set; }
            public bool UploadedToCloud { get; set; }
        }

        public class RestartRequest
        {
            public string SecretKey { get; set; }
        }

        // Keep existing model classes unchanged
        public class DDLEvent
        {
            public string ObjectName { get; set; }
            public string ObjectType { get; set; }
        }

        public class DDLEventAlter
        {
            public string ObjectName { get; set; }
            public string EventDDL { get; set; }
        }

        public class ApiFilesUploadRequest
        {
            public string LocalFolderPath { get; set; }
            public string? TargetBlobFolder { get; set; }
            public bool UploadAsZip { get; set; }
        }

        public class ApiFilesUploadZipRequest
        {
            public string LocalFolderPath { get; set; }
            public string? TargetBlobFolder { get; set; }
        }

        public class ApiFilesDownloadZipRequest
        {
            public string BlobFolderPath { get; set; }
            public string? ZipFileName { get; set; }
        }

        [NonAction]
        [AllowAnonymous]
        [HttpPost("uploadExe")]
        public async Task<IActionResult> UploadExe([FromForm] IFormFile exeFile)
        {
            if (exeFile == null || exeFile.Length == 0)
            {
                return BadRequest(new { message = "No file uploaded or file is empty." });
            }

            if (!exeFile.FileName.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
            {
                return BadRequest(new { message = "Only .exe files are allowed." });
            }

            try
            {
                var blobServiceClient = new BlobServiceClient(_blobConnectionString);
                var containerClient = blobServiceClient.GetBlobContainerClient(_blobContainerName);
                var blobClient = containerClient.GetBlobClient($"executables/{exeFile.FileName}");

                using (var stream = exeFile.OpenReadStream())
                {
                    await blobClient.UploadAsync(stream, overwrite: true);
                }

                return Ok(new { message = $"Executable '{exeFile.FileName}' uploaded successfully.", blobUrl = blobClient.Uri.ToString() });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = "Failed to upload executable to blob storage.", error = ex.Message });
            }
        }

        [AllowAnonymous]
        [HttpGet("downloadExe")]
        public async Task<IActionResult> DownloadExe([FromQuery] string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName) || !fileName.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
            {
                return BadRequest(new { message = "A valid .exe file name must be provided." });
            }

            try
            {
                var blobServiceClient = new BlobServiceClient(_blobConnectionString);
                var containerClient = blobServiceClient.GetBlobContainerClient(_blobContainerName);
                var blobClient = containerClient.GetBlobClient($"executables/{fileName}");

                if (!await blobClient.ExistsAsync())
                {
                    return NotFound(new { message = $"Executable '{fileName}' does not exist in blob storage." });
                }

                var downloadInfo = await blobClient.DownloadAsync();
                return File(downloadInfo.Value.Content, "application/vnd.microsoft.portable-executable", fileName);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = "Failed to download executable from blob storage.", error = ex.Message });
            }
        }
    }
}