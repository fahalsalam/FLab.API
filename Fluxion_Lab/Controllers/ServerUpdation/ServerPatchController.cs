using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Fluxion_Lab.Models.General;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Net.NetworkInformation;
using System.Text.Json;
using System.Threading;

namespace Fluxion_Lab.Controllers.ServerUpdation
{
    //[ApiController]
    //[Route("api/[controller]")]
    public class ServerPatchController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly IDbConnection _dbcontext;
        private readonly IDbConnection _dbcontext1;
        private readonly string _blobConnectionString;
        private readonly string _containerName;
        private readonly string _sqlFolderPrefix = "sql-patches";
        private readonly string _apiFolderPrefix = "api-builds";


        public ServerPatchController(IOptions<JwtKey> options, IDbConnection dbcontext, IConfiguration configuration)
        {
            _dbcontext = dbcontext;
            _configuration = configuration;
            _blobConnectionString = configuration["AzureStorage:ConnectionString"];
            _containerName = configuration["AzureStorage:ContainerName"];
            var connectionString = $"Server={GetServerString()};Database=db_Fluxion_Prod;User Id=FS;Password=Fluxion@FS@987;Encrypt=True;TrustServerCertificate=True;";
            _dbcontext1 = new SqlConnection(connectionString);
        }

        private string GetServerString()
        {
            var mode = _configuration["SaaSOptions:Mode"];
            return string.Equals(mode, "OnPrem", StringComparison.OrdinalIgnoreCase)
                ? "103.177.182.183,5734"
                : "localhost";
        }

        [HttpPost("start")]
        public async Task<IActionResult> StartSite([FromHeader] string siteName)
        {
            var sw = Stopwatch.StartNew();
            await InsertCommand(siteName, "start");
            sw.Stop();
            await LogClientActivity(null, siteName, "Info", "Start command inserted.", sw.ElapsedMilliseconds);
            return Ok();
        }

        [HttpPost("stop")]
        public async Task<IActionResult> StopSite([FromHeader] string siteName, [FromHeader] int? clientId, [FromHeader] string iisPath)
        {
            var sw = Stopwatch.StartNew();
            await InsertCommand(siteName, "stop");
            sw.Stop();
            await LogClientActivity(clientId, siteName, "Info", "Stop command inserted.", sw.ElapsedMilliseconds);

            Thread.Sleep(6000);

            if (!HasInternetConnection())
            {
                await LogClientActivity(clientId, siteName, "Error", "No internet connection during patching process.", 0);
                return StatusCode(503, "No internet connection. Aborting patch.");
            }

            sw.Restart();
            bool result = await UpdateClientToLatestVersionAsync(clientId, iisPath);
            sw.Stop();

            if (result)
            {
                await InsertCommand(siteName, "start");
                await LogClientActivity(clientId, siteName, "Info", "Patch process completed successfully.", sw.ElapsedMilliseconds);
                return Ok("Patch applied and site restarted successfully.");
            }

            await LogClientActivity(clientId, siteName, "Error", "Patch process failed.", sw.ElapsedMilliseconds);
            return StatusCode(500, "Patch failed and rollback executed.");
        }

        private async Task InsertCommand(string siteName, string command)
        {
            var sw = Stopwatch.StartNew();
            if (_dbcontext.State != ConnectionState.Open)
                await ((SqlConnection)_dbcontext).OpenAsync();

            using (var cmd = _dbcontext.CreateCommand())
            {
                cmd.CommandText = "INSERT INTO SiteCommands (SiteName, Command) VALUES (@SiteName, @Command)";
                var siteNameParam = cmd.CreateParameter();
                siteNameParam.ParameterName = "@SiteName";
                siteNameParam.Value = siteName;
                cmd.Parameters.Add(siteNameParam);

                var commandParam = cmd.CreateParameter();
                commandParam.ParameterName = "@Command";
                commandParam.Value = command;
                cmd.Parameters.Add(commandParam);

                await ((SqlCommand)cmd).ExecuteNonQueryAsync();
            }
            sw.Stop();
            await LogClientActivity(null, siteName, "Info", $"Inserted command '{command}' to SiteCommands.", sw.ElapsedMilliseconds);
        }

        private async Task LogClientActivity(int? clientId, string context, string level, string message, long durationMs)
        {
            try
            {
                if (_dbcontext1.State != ConnectionState.Open)
                    await ((SqlConnection)_dbcontext1).OpenAsync();

                string ip = HttpContext.Connection?.RemoteIpAddress?.ToString();
                string host = Environment.MachineName;

                using var cmd = _dbcontext1.CreateCommand();
                cmd.CommandText = @"INSERT INTO PatchLogHistory (ClientId, Context, Level, Message, IpAddress, HostName, DurationMs, LoggedAt)
                                   VALUES (@ClientId, @Context, @Level, @Message, @IpAddress, @HostName, @DurationMs, GETDATE())";

                cmd.Parameters.Add(new SqlParameter("@ClientId", clientId ?? 0));
                cmd.Parameters.Add(new SqlParameter("@Context", context));
                cmd.Parameters.Add(new SqlParameter("@Level", level));
                cmd.Parameters.Add(new SqlParameter("@Message", message));
                cmd.Parameters.Add(new SqlParameter("@IpAddress", ip ?? "Unknown"));
                cmd.Parameters.Add(new SqlParameter("@HostName", host));
                cmd.Parameters.Add(new SqlParameter("@DurationMs", durationMs));

                await ((SqlCommand)cmd).ExecuteNonQueryAsync();
            }
            catch { /* Logging failure is non-fatal */ }
        }

        private bool HasInternetConnection()
        {
            try { return new Ping().Send("8.8.8.8", 1000).Status == IPStatus.Success; }
            catch { return false; }
        }

        private async Task<bool> UpdateClientToLatestVersionAsync(int? clientId, string iisPath)
        {
            var sw = Stopwatch.StartNew();
            var allVersions = await GetAllVersionFoldersAsync();
            var currentVersion = await GetLatestAppliedVersionFromDb(clientId);
            var toApply = allVersions.Where(v => string.Compare(v, currentVersion, StringComparison.OrdinalIgnoreCase) > 0)
                                     .OrderBy(v => v);

            foreach (var version in toApply)
            {
                var patchSw = Stopwatch.StartNew();
                try
                {
                    var manifest = await LoadManifest(version);
                    await LogClientActivity(clientId, version, "Info", "Loaded manifest.", 0);

                    var dbBackupPath = await BackupDatabase("db_fluxion");
                    await LogClientActivity(clientId, version, "Info", $"Database backup created: {dbBackupPath}", 0);

                    var apiBackupPath = await BackupIISFolder(iisPath);
                    await LogClientActivity(clientId, version, "Info", $"API backup created: {apiBackupPath}", 0);

                    await ApplySqlScriptsFromBlob(version, manifest);
                    await LogClientActivity(clientId, version, "Info", "SQL scripts executed.", 0);

                    await ReplaceAPIWithNewBuildFromBlob(version, iisPath, manifest);
                    await LogClientActivity(clientId, version, "Info", "API files replaced.", 0);

                    await LogAppliedVersionToDb(clientId, version, "Success", "Applied successfully");
                    patchSw.Stop();
                    await LogClientActivity(clientId, version, "Success", "Patch applied successfully.", patchSw.ElapsedMilliseconds);
                }
                catch (Exception ex)
                {
                    patchSw.Stop();
                    await RollbackDatabase();
                    await RollbackAPI(iisPath);
                    await LogAppliedVersionToDb(clientId, version, "Failed", ex.Message);
                    await LogClientActivity(clientId, version, "Error", ex.Message, patchSw.ElapsedMilliseconds);
                    return false;
                }
            }
            sw.Stop();
            await LogClientActivity(clientId, "System", "Info", "Completed version update sequence.", sw.ElapsedMilliseconds);
            return true;
        }

        private async Task<List<string>> GetAllVersionFoldersAsync()
        {
            var sw = Stopwatch.StartNew();
            var container = new BlobContainerClient(_blobConnectionString, _containerName);
            var versionFolders = new HashSet<string>();
            await foreach (var blobItem in container.GetBlobsAsync(prefix: _sqlFolderPrefix + "/"))
            {
                var relativePath = blobItem.Name.Substring(_sqlFolderPrefix.Length + 1);
                var parts = relativePath.Split('/');
                if (parts.Length > 1)
                    versionFolders.Add(parts[0]);
            }
            sw.Stop();
            await LogClientActivity(null, "Blob", "Info", "Fetched version folders from blob.", sw.ElapsedMilliseconds);
            return versionFolders.OrderBy(v => v).ToList();
        }

        private async Task<string> GetLatestAppliedVersionFromDb(int? clientId)
        {
            var sw = Stopwatch.StartNew();
            using var cmd = _dbcontext.CreateCommand();
            cmd.CommandText = "SELECT TOP 1 Version FROM sstbl_AppliedPatches WHERE ClientId = @ClientId ORDER BY AppliedOn DESC";
            var param = cmd.CreateParameter();
            param.ParameterName = "@ClientId";
            param.Value = clientId;
            cmd.Parameters.Add(param);
            var result = await ((SqlCommand)cmd).ExecuteScalarAsync();
            sw.Stop();
            await LogClientActivity(clientId, "Database", "Info", "Fetched latest applied version.", sw.ElapsedMilliseconds);
            return result?.ToString() ?? "v1.0.0";
        }

        private async Task<ManifestModel> LoadManifest(string version)
        {
            var sw = Stopwatch.StartNew();
            var container = new BlobContainerClient(_blobConnectionString, _containerName);
            var blob = container.GetBlobClient($"{_sqlFolderPrefix}/{version}/manifest.json");
            var content = await blob.DownloadContentAsync();
            sw.Stop();
            await LogClientActivity(null, version, "Info", "Manifest file loaded.", sw.ElapsedMilliseconds);
            return JsonSerializer.Deserialize<ManifestModel>(content.Value.Content.ToString());
        }

        private async Task<string> BackupDatabase(string dbName)
        {
            var sw = Stopwatch.StartNew();
            var pathRoot = "C:\\DBBackups";
            if (!Directory.Exists(pathRoot))
                Directory.CreateDirectory(pathRoot);

            var path = Path.Combine(pathRoot, $"{dbName}_{DateTime.Now:yyyyMMddHHmmss}.bak");
            using var cmd = _dbcontext.CreateCommand();
            cmd.CommandText = $"BACKUP DATABASE [{dbName}] TO DISK = N'{path}' WITH INIT";
            await ((SqlCommand)cmd).ExecuteNonQueryAsync();
            sw.Stop();
            await LogClientActivity(null, "Database", "Info", $"Database backed up to {path}.", sw.ElapsedMilliseconds);
            return path;
        }

        private async Task<string> BackupIISFolder(string sourcePath)
        {
            var sw = Stopwatch.StartNew();
            var backupRoot = @"C:\IIS\Backup";
            var backupPath = Path.Combine(backupRoot, $"Fluxion_{DateTime.Now:yyyyMMddHHmmss}");

            if (!Directory.Exists(backupRoot))
                Directory.CreateDirectory(backupRoot);

            foreach (var file in Directory.GetFiles(sourcePath, "*", SearchOption.AllDirectories))
            {
                var dest = file.Replace(sourcePath, backupPath);
                Directory.CreateDirectory(Path.GetDirectoryName(dest));
                System.IO.File.Copy(file, dest, true);
            }
            sw.Stop();
            await LogClientActivity(null, "Backup", "Info", $"IIS folder backed up to {backupPath}.", sw.ElapsedMilliseconds);
            return backupPath;
        }

        private async Task ApplySqlScriptsFromBlob(string version, ManifestModel manifest)
        {
            var sw = Stopwatch.StartNew();
            var container = new BlobContainerClient(_blobConnectionString, _containerName);
            foreach (var file in manifest.Scripts)
            {
                var blob = container.GetBlobClient($"{_sqlFolderPrefix}/{version}/{file}");
                var download = await blob.DownloadContentAsync();
                var sql = download.Value.Content.ToString();
                using var cmd = _dbcontext.CreateCommand();
                cmd.CommandText = sql;
                await ((SqlCommand)cmd).ExecuteNonQueryAsync();
            }
            sw.Stop();
            await LogClientActivity(null, version, "Info", "Executed SQL scripts from blob.", sw.ElapsedMilliseconds);
        }

        private async Task ReplaceAPIWithNewBuildFromBlob(string version, string targetPath, ManifestModel manifest)
        {
            var sw = Stopwatch.StartNew();
            var container = new BlobContainerClient(_blobConnectionString, _containerName);
            foreach (var file in manifest.ApiFiles)
            {
                var blob = container.GetBlobClient($"{_apiFolderPrefix}/{version}/{file}");
                var filePath = Path.Combine(targetPath, file);
                Directory.CreateDirectory(Path.GetDirectoryName(filePath));
                await blob.DownloadToAsync(filePath);
            }
            sw.Stop();
            await LogClientActivity(null, version, "Info", "API files replaced from blob.", sw.ElapsedMilliseconds);
        }

        private async Task RollbackDatabase()
        {
            var sw = Stopwatch.StartNew();
            var pathRoot = "C:\\DBBackups";
            if (!Directory.Exists(pathRoot))
                Directory.CreateDirectory(pathRoot);

            var latestBackup = Directory.GetFiles(pathRoot).OrderByDescending(f => f).FirstOrDefault();
            if (latestBackup != null)
            {
                using var cmd = _dbcontext.CreateCommand();
                cmd.CommandText = $"RESTORE DATABASE [db_fluxion] FROM DISK = '{latestBackup}' WITH REPLACE";
                await ((SqlCommand)cmd).ExecuteNonQueryAsync();
            }
            sw.Stop();
            await LogClientActivity(null, "Rollback", "Info", "Database rollback completed.", sw.ElapsedMilliseconds);
        }

        private async Task RollbackAPI(string iisPath)
        {
            var sw = Stopwatch.StartNew();
            var latestBackup = Directory.GetDirectories("C:\\IIS\\Backup").OrderByDescending(f => f).FirstOrDefault();
            if (latestBackup != null)
            {
                foreach (var file in Directory.GetFiles(latestBackup, "*", SearchOption.AllDirectories))
                {
                    var dest = file.Replace(latestBackup, iisPath);
                    Directory.CreateDirectory(Path.GetDirectoryName(dest));
                    System.IO.File.Copy(file, dest, true);
                }
            }
            sw.Stop();
            await LogClientActivity(null, "Rollback", "Info", "API rollback completed.", sw.ElapsedMilliseconds);
        }

        private async Task LogAppliedVersionToDb(int? clientId, string version, string status, string notes)
        {
            using var cmd = _dbcontext.CreateCommand();
            cmd.CommandText = "INSERT INTO sstbl_AppliedPatches (ClientId, Version, Status, Notes) VALUES (@ClientId, @Version, @Status, @Notes)";
            var p1 = cmd.CreateParameter(); p1.ParameterName = "@ClientId"; p1.Value = clientId; cmd.Parameters.Add(p1);
            var p2 = cmd.CreateParameter(); p2.ParameterName = "@Version"; p2.Value = version; cmd.Parameters.Add(p2);
            var p3 = cmd.CreateParameter(); p3.ParameterName = "@Status"; p3.Value = status; cmd.Parameters.Add(p3);
            var p4 = cmd.CreateParameter(); p4.ParameterName = "@Notes"; p4.Value = notes; cmd.Parameters.Add(p4);
            await ((SqlCommand)cmd).ExecuteNonQueryAsync();
        }
    }

    public class ManifestModel
    {
        public string Version { get; set; }
        public string Description { get; set; }
        public List<string> Scripts { get; set; }
        public List<string> ApiFiles { get; set; }
    }
}
