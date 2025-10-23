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
            _connectionString = "Server=localhost;Database=db_Fluxion_Dev;User Id=FS;Password=Fluxion@FS@987;Encrypt=True;TrustServerCertificate=True;";
            _blobConnectionString = configuration["AzureStorage:ConnectionString"];
            _blobContainerName = configuration["AzureStorage:ContainerName"];
        }

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