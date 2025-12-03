using System.Data.SqlClient;
using System.Data;
using Dapper;
using System.Text;
using Newtonsoft.Json;
using System.Net.Http;
using Fluxion_Lab.Services.Sync.Interfaces;
using Microsoft.Extensions.Logging;
using Fluxion_Lab.Helper;
using System.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Configuration;
using System.IO;

namespace Fluxion_Lab.Services.Sync.Implementations
{
    public class DataSyncServiceImplementation : IDataSyncService
    {

        private readonly ILogger<DataSyncServiceImplementation> _logger;
        private readonly SqlConnection _dbcontext;
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly string _localConnectionString;
        private string _onlineConnectionString;
        private readonly ICloudLogger _cloudLogger;
        private readonly ISyncConfigProvider _configProvider;
        private static int _syncIterationCount = 0;

        public DataSyncServiceImplementation(ILogger<DataSyncServiceImplementation> logger, IDbConnection dbcontext, HttpClient httpClient, IConfiguration configuration, ICloudLogger cloudLogger, ISyncConfigProvider configProvider)
        {
            _logger = logger;
            _dbcontext = (SqlConnection)dbcontext;
            _httpClient = httpClient;
            _configuration = configuration;
            _localConnectionString = _dbcontext.ConnectionString;
            _configProvider = configProvider;
            _cloudLogger = cloudLogger;

            // load config from DB (mtbl_ClientMaster) if available, fallback to appsettings
            try
            {
                var cfg = _configProvider.GetConfigAsync().GetAwaiter().GetResult();
                var server = cfg?.OnlineServer ?? configuration["Sync:OnlineServer"] ?? "localhost";
                _onlineConnectionString = $"Server={server};Database=db_Fluxion_Prod;User Id=FS;Password=Fluxion@FS@987;Encrypt=True;TrustServerCertificate=True;";

                var logPath = cfg?.LogPath ?? configuration["Sync:LogPath"] ?? Path.Combine(AppContext.BaseDirectory, "SyncLogs");
                // if relative path, make it absolute under app base
                if (!Path.IsPathRooted(logPath))
                    logPath = Path.Combine(AppContext.BaseDirectory, logPath);

                try { _cloudLogger.SetLogDirectory(logPath); _cloudLogger.EnsureDirectory(); } catch { }
            }
            catch
            {
                var server = configuration["Sync:OnlineServer"] ?? "localhost";
                _onlineConnectionString = $"Server={server};Database=db_Fluxion_Prod;User Id=FS;Password=Fluxion@FS@987;Encrypt=True;TrustServerCertificate=True;";
                try {
                    var logPath = configuration["Sync:LogPath"] ?? Path.Combine(AppContext.BaseDirectory, "SyncLogs");
                    if (!Path.IsPathRooted(logPath)) logPath = Path.Combine(AppContext.BaseDirectory, logPath);
                    _cloudLogger.SetLogDirectory(logPath); _cloudLogger.EnsureDirectory();
                } catch { }
            }
        }

        public async Task SyncTestDataSummary()
        {
            // basic placeholder that logs start/stop and uses DB-based config for the online connection and log path
            _syncIterationCount++;
            var iter = _syncIterationCount;
            await _cloudLogger.LogAsync($"[INFO] Starting sync iteration #{iter}");
            _logger.LogInformation("DataSyncServiceImplementation SyncTestDataSummary called. Iteration: {iter}", iter);

            try
            {
                await SyncTestDataFromLocalMachine();
            }
            catch (Exception ex)
            {
                await _cloudLogger.LogAsync($"[ERROR] Iteration #{iter}: {ex.Message}");
                _logger.LogError(ex, "SyncTestDataSummary failed on iteration {iter}", iter);
            }
        }

        private async Task SyncTestDataFromLocalMachine()
        {
            var (clientId, clientName) = await GetClientIdentityAsync();
            var syncStartTime = DateTime.Now;

            // Mirror to file via cloud logger immediately
            await _cloudLogger.LogAsync($"[TRACE] Iteration #{_syncIterationCount}: Enter SyncTestDataFromLocalMachine()");

            try
            {
                await _cloudLogger.LogAsync($"SyncTestDataFromLocalMachine started - Iteration #{_syncIterationCount}");

                long InvoiceNo = 0;
                long sequence = 0;

                if (_dbcontext.State != ConnectionState.Open)
                    await _dbcontext.OpenAsync();

                _logger.LogInformation("Using offline/local connection string: {conn}", _dbcontext.ConnectionString);
                _logger.LogInformation("Using online connection string: {conn}", _onlineConnectionString);

                var _header = new List<TestEntryHeader>();
                var _clientIDList = new List<dynamic>();
                var lineItems = new List<TestEntryLine>();

                using var command = _dbcontext.CreateCommand();
                command.CommandText = "SP_ClientTestDataSync";
                command.CommandType = CommandType.StoredProcedure;

                command.Parameters.Add(new SqlParameter("@InvoiceNo", InvoiceNo));
                command.Parameters.Add(new SqlParameter("@Sequeece", sequence));

                using var reader = await command.ExecuteReaderAsync();

                // Headers
                while (await reader.ReadAsync())
                {
                    _header.Add(new TestEntryHeader
                    {
                        ClientID = GetSafeValue<long?>(reader, "ClientID"),
                        ClientName = GetSafeValue<string>(reader, "ClientName"),
                        PatientID = GetSafeValue<long?>(reader, "PatientID"),
                        PatientName = GetSafeValue<string>(reader, "PatientName"),
                        Age = GetSafeValue<int?>(reader, "Age"),
                        MobileNo = GetSafeValue<string>(reader, "MobileNo"),
                        DOB = GetSafeValue<string>(reader, "DOB"),
                        EntryDate = GetSafeValue<string>(reader, "EntryDate"),
                        InvoiceNo = GetSafeValue<long?>(reader, "InvoiceNo"),
                        Sequence = GetSafeValue<int?>(reader, "Sequence"),
                        SequenceName = GetSafeValue<string>(reader, "SequenceName"),
                        EditNo = GetSafeValue<long?>(reader, "EditNo"),
                        GrandTotal = GetSafeValue<decimal?>(reader, "GrandTotal"),
                        ResultStatus = GetSafeValue<string>(reader, "ResultStatus"),
                        PaymentStatus = GetSafeValue<string>(reader, "PaymentStatus"),
                        LastModified = GetSafeValue<string>(reader, "LastModified"),
                        HeaderImageUrl = GetSafeValue<string>(reader, "HeaderImageUrl"),
                        FooterImageUrl = GetSafeValue<string>(reader, "FooterImageUrl"),
                        LineJsonData = GetSafeValue<string>(reader, "LineJsonData"),
                        Gender = GetSafeValue<string>(reader, "Gender"),
                        DrName = GetSafeValue<string>(reader, "DrName"),
                        CreatedDateTime = GetSafeValue<DateTime?>(reader, "CreatedDateTime"),
                        ResultApprovedBy = GetSafeValue<string>(reader, "ResultApprovedBy"),
                        ResultApprovedDateTime = GetSafeValue<DateTime?>(reader, "ResultApprovedDateTime"),
                        ResultVerifiedBy = GetSafeValue<string>(reader, "ResultVerifiedBy"),
                        ResultApproveSign = GetSafeValue<string>(reader, "ResultApproveSign"),
                        LabName = GetSafeValue<string>(reader, "LabName"),
                        BalanceDue = GetSafeValue<decimal?>(reader, "BalanceDue"),
                        DiscAmount = GetSafeValue<decimal?>(reader, "DiscAmount"),
                        DocStatus = GetSafeValue<string>(reader, "DocStatus") 
                    });
                }

                // Client list
                if (await reader.NextResultAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        var clientInfo = new
                        {
                            ClientID = GetSafeValue<long>(reader, "ClientID"),
                            ClientName = GetSafeValue<string>(reader, "ClientName") ?? "UNKNOWN"
                        };
                        _clientIDList.Add(clientInfo);
                    }
                }

                // Line items
                if (await reader.NextResultAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        lineItems.Add(new TestEntryLine
                        {
                            Sequence = GetSafeValue<int?>(reader, "Sequence"),
                            InvoiceNo = GetSafeValue<long?>(reader, "InvoiceNo"),
                            EditNo = GetSafeValue<long?>(reader, "EditNo"),
                            SI_No = GetSafeValue<int?>(reader, "SI_No"),
                            ID = GetSafeValue<int?>(reader, "ID"),
                            Name = GetSafeValue<string>(reader, "Name"),
                            Type = GetSafeValue<string>(reader, "Type"),
                            LineStatus = GetSafeValue<string>(reader, "LineStatus"),
                            ClientID = GetSafeValue<long?>(reader, "ClientID")
                        });
                    }
                }

                await _cloudLogger.LogAsync($"Iteration #{_syncIterationCount}: Fetched {_header.Count} headers and {lineItems.Count} lines for sync.");

                if (_clientIDList.Count > 0)
                {
                    clientId = ((dynamic)_clientIDList[0]).ClientID;
                    clientName = ((dynamic)_clientIDList[0]).ClientName;
                }

                // Post each bill (header + lines) one by one to API
                var apiUrl = "https://api.fluxionsolution.com/api/7990/syncClientTestData";
                int successCount = 0, failCount = 0;
                List<string> failedBills = [];

                foreach (var header in _header)
                {
                    var billLines = lineItems.Where(l => l.Sequence == header.Sequence && l.InvoiceNo == header.InvoiceNo && l.EditNo == header.EditNo).ToList();
                    var payload = new
                    {
                        headers = new[] { header },
                        lines = billLines
                    };

                    var jsonPayload = JsonConvert.SerializeObject(payload);
                    await _cloudLogger.LogAsync($"Iteration #{_syncIterationCount}: Posting header JSON for InvoiceNo={header.InvoiceNo}, EditNo={header.EditNo}");

                    var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

                    try
                    {
                        var response = await _httpClient.PostAsync(apiUrl, content);
                        string responseContent = await response.Content.ReadAsStringAsync();

                        if (response.IsSuccessStatusCode)
                        {
                            await _cloudLogger.LogAsync($"Iteration #{_syncIterationCount}: API sync successful for Bill: InvoiceNo={header.InvoiceNo}, EditNo={header.EditNo}");

                            //// Log successful bill sync to database
                            //await LogBillSync(clientId, clientName, header.InvoiceNo, header.EditNo, header.Sequence,
                            //    $"Iteration #{_syncIterationCount}: API sync successful for Bill: InvoiceNo={header.InvoiceNo}, EditNo={header.EditNo}",
                            //    false, true, response.StatusCode.ToString(), _syncIterationCount);
                            //await LogBillSyncToLocalDb(clientId, clientName, header.InvoiceNo, header.EditNo, header.Sequence,
                            //    $"Iteration #{_syncIterationCount}: API sync successful for Bill: InvoiceNo={header.InvoiceNo}, EditNo={header.EditNo}",
                            //    false, true, response.StatusCode.ToString(), _syncIterationCount);

                            // Parse API response and update local sync flags using response data
                            try
                            {
                                var apiResult = JsonConvert.DeserializeObject<ApiSyncResponse>(responseContent);
                                if (apiResult?.data != null)
                                {
                                    foreach (var syncData in apiResult.data)
                                    {
                                        if (syncData.inserted != null)
                                        {
                                            foreach (var item in syncData.inserted)
                                            {
                                                await UpdateSyncFlags(item.Sequence, item.InvoiceNo, item.EditNo);
                                            }
                                        }
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                await _cloudLogger.LogAsync($"[ERROR] Iteration #{_syncIterationCount}: Failed to parse API response or update local sync flags: {ex.Message}");
                            }
                            successCount++;
                        }
                        else
                        {
                            var errorMsg = $"Iteration #{_syncIterationCount}: API sync failed for Bill:InvoiceNo={header.InvoiceNo}, EditNo={header.EditNo} - {response.StatusCode} - {responseContent}";
                           
                            await _cloudLogger.LogAsync(errorMsg);
                            
                            failedBills.Add($"InvoiceNo={header.InvoiceNo}, EditNo={header.EditNo}");

                            // Log failed bill sync to database
                            
                            await LogBillSync(clientId, clientName, header.InvoiceNo, header.EditNo, header.Sequence,
                                errorMsg, true, false, response.StatusCode.ToString(), _syncIterationCount);
                            
                            await LogBillSyncToLocalDb(clientId, clientName, header.InvoiceNo, header.EditNo, header.Sequence,
                                errorMsg, true, false, response.StatusCode.ToString(), _syncIterationCount);

                            failCount++;
                        }
                    }
                    catch (Exception apiEx)
                    {
                        var errorMsg = $"Iteration #{_syncIterationCount}: API call failed for Bill:InvoiceNo={header.InvoiceNo}, EditNo={header.EditNo}: {apiEx.Message}";
                        await _cloudLogger.LogAsync($"[ERROR] {errorMsg}");
                        failedBills.Add($"InvoiceNo={header.InvoiceNo}, EditNo={header.EditNo}");

                        // Log API exception to database
                        await LogBillSync(clientId, clientName, header.InvoiceNo, header.EditNo, header.Sequence,
                            errorMsg, true, false, "EXCEPTION", _syncIterationCount);
                        await LogBillSyncToLocalDb(clientId, clientName, header.InvoiceNo, header.EditNo, header.Sequence,
                            errorMsg, true, false, "EXCEPTION", _syncIterationCount);

                        failCount++;
                    }
                }

                // Calculate sync duration
                var syncDuration = (long)(DateTime.Now - syncStartTime).TotalMilliseconds;

                //// Log sync summary to database with iteration tracking
                //await LogSyncSummary(clientId, clientName, _header.Count, successCount, failCount,
                //    syncDuration, failedBills.Count > 0 ? string.Join(", ", failedBills) : null, _syncIterationCount);
               
                await LogSyncSummaryToLocalDb(clientId, clientName, _header.Count, successCount, failCount,
                    syncDuration, failedBills.Count > 0 ? string.Join(", ", failedBills) : null, _syncIterationCount);

                // Log sync summary to cloud
                await _cloudLogger.LogAsync($"Iteration #{_syncIterationCount}: Sync Summary: Total={_header.Count}, Success={successCount}, Failed={failCount}");
                if (failedBills.Count > 0)
                {
                    await _cloudLogger.LogAsync($"Iteration #{_syncIterationCount}: Failed Bills: {string.Join(", ", failedBills)}");
                }

                await _cloudLogger.LogAsync($"Iteration #{_syncIterationCount}: Data sync completed successfully for ClientID={clientId}, ClientName={clientName}.");

            }
            catch (Exception ex)
            {
                var syncDuration = (long)(DateTime.Now - syncStartTime).TotalMilliseconds;

                try
                {
                    using var connection = new SqlConnection(_onlineConnectionString);
                    await connection.OpenAsync();
                    await LogDataSync(connection, clientId, clientName,
                        $"Iteration #{_syncIterationCount}: {ex.Message}", true, false);
                }
                catch (Exception logEx)
                {
                    _logger.LogError(logEx, "Failed to log sync error to database for ClientID={ClientID} on iteration #{Iteration}",
                        clientId, _syncIterationCount);
                }

                await _cloudLogger.LogAsync($"[ERROR] Iteration #{_syncIterationCount}: Sync failed: {ex.Message}");
                throw;
            }
        }

        private async Task<(long, string)> GetClientIdentityAsync()
        {
            try
            {
                if (_dbcontext.State != ConnectionState.Open)
                    await _dbcontext.OpenAsync();

                var row = await _dbcontext.QueryFirstOrDefaultAsync<dynamic>("SELECT TOP 1 ClientID, ClientName FROM mtbl_ClientMaster");
                if (row != null)
                {
                    long id = row.ClientID != null ? (long)row.ClientID : 0;
                    string name = row.ClientName ?? "";
                    return (id, name);
                }
            }
            catch { }
            return (0, "");
        }

        private async Task LogSyncSummaryToOnlineDb(long clientId, string clientName, int totalRecords, int successCount, int failureCount, long syncDurationMs, string failedBills, int iterationCount, long? invoiceNo = null, int? sequence = null, int? editNo = null)
        {
            try
            {
                using var conn = new SqlConnection(_onlineConnectionString);
                await conn.OpenAsync();

                var sql = @"
                    INSERT INTO dbo.DataSyncLogs
                    (ClientID, ClientName, InvoiceNo, Sequence, EditNo, SyncMessage, ErrorTime, IsSyncWithError, IsDataSyncSucess, TotalRecords, SuccessCount, FailureCount, SyncDuration, MachineName, ServiceVersion, IterationCount, SyncCycleNumber)
                    VALUES
                    (@ClientID, @ClientName, @InvoiceNo, @Sequence, @EditNo, @SyncMessage, GETDATE(), @IsSyncWithError, @IsDataSyncSucess, @TotalRecords, @SuccessCount, @FailureCount, @SyncDuration, @MachineName, @ServiceVersion, @IterationCount, @SyncCycleNumber)
                    ";

                var message = $"Iteration #{iterationCount}: Total={totalRecords}, Success={successCount}, Failed={failureCount}" + (string.IsNullOrEmpty(failedBills) ? "" : ", Failed: " + failedBills);

                await conn.ExecuteAsync(sql, new
                {
                    ClientID = clientId,
                    ClientName = clientName ?? string.Empty,
                    InvoiceNo = invoiceNo,
                    Sequence = sequence,
                    EditNo = editNo,
                    SyncMessage = message,
                    IsSyncWithError = failureCount > 0,
                    IsDataSyncSucess = failureCount == 0 && successCount > 0,
                    TotalRecords = totalRecords,
                    SuccessCount = successCount,
                    FailureCount = failureCount,
                    SyncDuration = syncDurationMs,
                    MachineName = Environment.MachineName,
                    ServiceVersion = "API DataSync v1",
                    IterationCount = iterationCount,
                    SyncCycleNumber = iterationCount
                });
            }
            catch (Exception ex)
            {
                try { await _cloudLogger.LogAsync($"[WARN] Failed to write DataSyncLogs to online DB: {ex.Message}"); } catch { }
            }
        }

        private async Task LogSyncSummaryToLocalDb(long clientId, string clientName, int totalRecords, int successCount, int failureCount, long syncDurationMs, string failedBills, int iterationCount, long? invoiceNo = null, int? sequence = null, int? editNo = null)
        {
            try
            {
                // Use the injected local DB context to write a local copy
                if (_dbcontext.State != ConnectionState.Open)
                    await _dbcontext.OpenAsync();

                var sql = @"
                    INSERT INTO dbo.DataSyncLogs
                    (ClientID, ClientName, InvoiceNo, Sequence, EditNo, SyncMessage, ErrorTime, IsSyncWithError, IsDataSyncSucess, TotalRecords, SuccessCount, FailureCount, SyncDuration, MachineName, ServiceVersion, IterationCount, SyncCycleNumber)
                    VALUES
                    (@ClientID, @ClientName, @InvoiceNo, @Sequence, @EditNo, @SyncMessage, GETDATE(), @IsSyncWithError, @IsDataSyncSucess, @TotalRecords, @SuccessCount, @FailureCount, @SyncDuration, @MachineName, @ServiceVersion, @IterationCount, @SyncCycleNumber)
                    ";

                var message = $"Iteration #{iterationCount}: Total={totalRecords}, Success={successCount}, Failed={failureCount}" + (string.IsNullOrEmpty(failedBills) ? "" : ", Failed: " + failedBills);

                await _dbcontext.ExecuteAsync(sql, new
                {
                    ClientID = clientId,
                    ClientName = clientName ?? string.Empty,
                    InvoiceNo = invoiceNo,
                    Sequence = sequence,
                    EditNo = editNo,
                    SyncMessage = message,
                    IsSyncWithError = failureCount > 0,
                    IsDataSyncSucess = failureCount == 0 && successCount > 0,
                    TotalRecords = totalRecords,
                    SuccessCount = successCount,
                    FailureCount = failureCount,
                    SyncDuration = syncDurationMs,
                    MachineName = Environment.MachineName,
                    ServiceVersion = "API DataSync v1",
                    IterationCount = iterationCount,
                    SyncCycleNumber = iterationCount
                });
            }
            catch (Exception ex)
            {
                try { await _cloudLogger.LogAsync($"[WARN] Failed to write DataSyncLogs to local DB: {ex.Message}"); } catch { }
            }
        }

        private async Task LogSyncSummary(long clientID, string clientName, int totalRecords, int successCount, int failureCount,
            long syncDurationMs, string? failedBills = null, int iterationCount = 0)
        {
            try
            {
                var summaryMessage = $"Iteration #{iterationCount}: Sync Summary: Total={totalRecords}, Success={successCount}, Failed={failureCount}";
                if (!string.IsNullOrEmpty(failedBills))
                    summaryMessage += $", Failed Bills: {failedBills}";

                using var connection = new SqlConnection(_onlineConnectionString);
                await connection.OpenAsync();
                using var command = connection.CreateCommand();
                command.CommandText = @"
                    INSERT INTO DataSyncLogs (
                        ClientID, ClientName, SyncMessage, ErrorTime, 
                        IsSyncWithError, IsDataSyncSucess, TotalRecords, SuccessCount, FailureCount,
                        SyncDuration, MachineName, ServiceVersion, IterationCount, SyncCycleNumber
                    )
                    VALUES (
                        @ClientID, @ClientName, @SyncMessage, @ErrorTime, 
                        @IsSyncWithError, @IsDataSyncSucess, @TotalRecords, @SuccessCount, @FailureCount,
                        @SyncDuration, @MachineName, @ServiceVersion, @IterationCount, @SyncCycleNumber
                    )";

                var hasErrors = failureCount > 0;
                var isSuccess = failureCount == 0 && successCount > 0;

                command.Parameters.Add(new SqlParameter("@ClientID", clientID));
                command.Parameters.Add(new SqlParameter("@ClientName", clientName ?? ""));
                command.Parameters.Add(new SqlParameter("@SyncMessage", summaryMessage));
                command.Parameters.Add(new SqlParameter("@ErrorTime", DateTime.Now));
                command.Parameters.Add(new SqlParameter("@IsSyncWithError", hasErrors));
                command.Parameters.Add(new SqlParameter("@IsDataSyncSucess", isSuccess));
                command.Parameters.Add(new SqlParameter("@TotalRecords", totalRecords));
                command.Parameters.Add(new SqlParameter("@SuccessCount", successCount));
                command.Parameters.Add(new SqlParameter("@FailureCount", failureCount));
                command.Parameters.Add(new SqlParameter("@SyncDuration", syncDurationMs));
                command.Parameters.Add(new SqlParameter("@MachineName", Environment.MachineName));
                command.Parameters.Add(new SqlParameter("@ServiceVersion", "API DataSync v1"));
                command.Parameters.Add(new SqlParameter("@IterationCount", iterationCount));
                command.Parameters.Add(new SqlParameter("@SyncCycleNumber", iterationCount));

                await command.ExecuteNonQueryAsync();
                await _cloudLogger.LogAsync($"[DATABASE] Iteration #{iterationCount}: Sync summary logged to cloud database: {summaryMessage}");
            }
            catch (Exception ex)
            {
                await _cloudLogger.LogAsync($"[ERROR] Iteration #{iterationCount}: Failed to log sync summary to cloud database: {ex.Message}");
            }
        }

        private async Task LogBillSync(long clientID, string clientName, long? invoiceNo, long? editNo, int? sequence,
            string message, bool isError, bool isSuccess, string? apiResponseCode = null, int iterationCount = 0)
        {
            try
            {
                using var connection = new SqlConnection(_onlineConnectionString);
                await connection.OpenAsync();
                using var command = connection.CreateCommand();
                command.CommandText = @"
                    INSERT INTO DataSyncLogs (
                        ClientID, ClientName, SyncMessage, ErrorTime, 
                        IsSyncWithError, IsDataSyncSucess, InvoiceNo, EditNo, Sequence,
                        APIResponseCode, MachineName, ServiceVersion, IterationCount, SyncCycleNumber
                    )
                    VALUES (
                        @ClientID, @ClientName, @SyncMessage, @ErrorTime, 
                        @IsSyncWithError, @IsDataSyncSucess, @InvoiceNo, @EditNo, @Sequence,
                        @APIResponseCode, @MachineName, @ServiceVersion, @IterationCount, @SyncCycleNumber
                    )";

                command.Parameters.Add(new SqlParameter("@ClientID", clientID));
                command.Parameters.Add(new SqlParameter("@ClientName", clientName ?? ""));
                command.Parameters.Add(new SqlParameter("@SyncMessage", message ?? ""));
                command.Parameters.Add(new SqlParameter("@ErrorTime", DateTime.Now));
                command.Parameters.Add(new SqlParameter("@IsSyncWithError", isError));
                command.Parameters.Add(new SqlParameter("@IsDataSyncSucess", isSuccess));
                command.Parameters.Add(new SqlParameter("@InvoiceNo", (object?)invoiceNo ?? DBNull.Value));
                command.Parameters.Add(new SqlParameter("@EditNo", (object?)editNo ?? DBNull.Value));
                command.Parameters.Add(new SqlParameter("@Sequence", (object?)sequence ?? DBNull.Value));
                command.Parameters.Add(new SqlParameter("@APIResponseCode", (object?)apiResponseCode ?? DBNull.Value));
                command.Parameters.Add(new SqlParameter("@MachineName", Environment.MachineName));
                command.Parameters.Add(new SqlParameter("@ServiceVersion", "API DataSync v1"));
                command.Parameters.Add(new SqlParameter("@IterationCount", iterationCount));
                command.Parameters.Add(new SqlParameter("@SyncCycleNumber", iterationCount));

                await command.ExecuteNonQueryAsync();
            }
            catch (Exception ex)
            {
                await _cloudLogger.LogAsync($"[ERROR] Iteration #{iterationCount}: Failed to log bill sync to cloud database: {ex.Message}");
            }
        }

        private async Task LogBillSyncToLocalDb(long clientID, string clientName, long? invoiceNo, long? editNo, int? sequence,
            string message, bool isError, bool isSuccess, string? apiResponseCode = null, int iterationCount = 0)
        {
            try
            {
                using var connection = new SqlConnection(_localConnectionString);
                await connection.OpenAsync();
                using var command = connection.CreateCommand();
                command.CommandText = @"
                    INSERT INTO dbo.DataSyncLogs (
                        ClientID, ClientName, SyncMessage, ErrorTime, 
                         IsSyncWithError, IsDataSyncSucess, InvoiceNo, EditNo, Sequence,
                         MachineName, ServiceVersion, IterationCount, SyncCycleNumber
                    )
                    VALUES (
                        @ClientID, @ClientName, @SyncMessage, @ErrorTime, 
                        @IsSyncWithError, @IsDataSyncSucess, @InvoiceNo, @EditNo, @Sequence,
                        @MachineName, @ServiceVersion, @IterationCount, @SyncCycleNumber
                    )";

                command.Parameters.Add(new SqlParameter("@ClientID", clientID));
                command.Parameters.Add(new SqlParameter("@ClientName", clientName ?? ""));
                command.Parameters.Add(new SqlParameter("@SyncMessage", message ?? ""));
                command.Parameters.Add(new SqlParameter("@ErrorTime", DateTime.Now));
                command.Parameters.Add(new SqlParameter("@IsSyncWithError", isError));
                command.Parameters.Add(new SqlParameter("@IsDataSyncSucess", isSuccess));
                command.Parameters.Add(new SqlParameter("@InvoiceNo", (object?)invoiceNo ?? DBNull.Value));
                command.Parameters.Add(new SqlParameter("@EditNo", (object?)editNo ?? DBNull.Value));
                command.Parameters.Add(new SqlParameter("@Sequence", (object?)sequence ?? DBNull.Value));
                //command.Parameters.Add(new SqlParameter("@APIResponseCode", (object?)apiResponseCode ?? DBNull.Value));
                command.Parameters.Add(new SqlParameter("@MachineName", Environment.MachineName));
                command.Parameters.Add(new SqlParameter("@ServiceVersion", "API DataSync v1"));
                command.Parameters.Add(new SqlParameter("@IterationCount", iterationCount));
                command.Parameters.Add(new SqlParameter("@SyncCycleNumber", iterationCount));

                await command.ExecuteNonQueryAsync();
            }
            catch (Exception ex)
            {
                await _cloudLogger.LogAsync($"[ERROR] Iteration #{iterationCount}: Failed to log bill sync to local DB: {ex.Message}");
            }
        }

        private async Task UpdateSyncFlags(int sequence, long invoiceNo, int editNo)
        {
            try
            {
                using var localConn = new SqlConnection(_localConnectionString);
                await localConn.OpenAsync();
                using var command = localConn.CreateCommand();

                command.CommandText = @"
                    UPDATE trntbl_TestEntriesHdr
                    SET IsDataSynced = 1, LastSyncIteration = @IterationCount, LastSyncDate = GETDATE()
                    WHERE Sequence = @Sequence AND InvoiceNo = @InvoiceNo AND EditNo = @EditNo;
        
                    UPDATE trntbl_TestEntriesLine
                    SET IsDataSynced = 1, LastSyncIteration = @IterationCount, LastSyncDate = GETDATE()
                    WHERE Sequence = @Sequence AND InvoiceNo = @InvoiceNo AND EditNo = @EditNo;
                ";

                command.Parameters.Add(new SqlParameter("@Sequence", sequence));
                command.Parameters.Add(new SqlParameter("@InvoiceNo", invoiceNo));
                command.Parameters.Add(new SqlParameter("@EditNo", editNo));
                command.Parameters.Add(new SqlParameter("@IterationCount", _syncIterationCount));

                await command.ExecuteNonQueryAsync();
            }
            catch (Exception ex)
            {
                await _cloudLogger.LogAsync($"[ERROR] Iteration #{_syncIterationCount}: Failed to update sync flags: {ex.Message}");
            }
        }

        private async Task LogDataSync(SqlConnection connection, long clientId, string clientName, string message, bool isError, bool isSuccess)
        {
            try
            {
                using var cmd = connection.CreateCommand();
                cmd.CommandText = @"
                    INSERT INTO mtbl_SyncLogs
                    (ClientID, ClientName, Message, IsError, IsSuccess, LogDate)
                    VALUES (@ClientID, @ClientName, @Message, @IsError, @IsSuccess, GETDATE())";

                cmd.Parameters.Add(new SqlParameter("@ClientID", clientId));
                cmd.Parameters.Add(new SqlParameter("@ClientName", clientName ?? "UNKNOWN"));
                cmd.Parameters.Add(new SqlParameter("@Message", message ?? ""));
                cmd.Parameters.Add(new SqlParameter("@IsError", isError));
                cmd.Parameters.Add(new SqlParameter("@IsSuccess", isSuccess));

                await cmd.ExecuteNonQueryAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to log sync operation for client {ClientID}", clientId);
            }
        }

        private T? GetSafeValue<T>(SqlDataReader reader, string columnName)
        {
            try
            {
                var ordinal = reader.GetOrdinal(columnName);
                if (reader.IsDBNull(ordinal))
                    return default;

                var value = reader.GetValue(ordinal);
                if (value == DBNull.Value)
                    return default;

                if (typeof(T) == typeof(string))
                    return (T)(object)value.ToString()!;

                var underlyingType = Nullable.GetUnderlyingType(typeof(T));
                if (underlyingType != null)
                    return (T)Convert.ChangeType(value, underlyingType);

                return (T)Convert.ChangeType(value, typeof(T));
            }
            catch
            {
                return default;
            }
        }

        public static int GetCurrentIterationCount() => _syncIterationCount;
        public static void ResetIterationCount() => _syncIterationCount = 0;
    }

    // === Data Models ===
    public class TestEntryHeader
    {
        public long? ClientID { get; set; }
        public string? ClientName { get; set; }
        public long? PatientID { get; set; }
        public string? PatientName { get; set; }
        public int? Age { get; set; }
        public string? MobileNo { get; set; }
        public string? DOB { get; set; }
        public string? EntryDate { get; set; }
        public long? InvoiceNo { get; set; }
        public int? Sequence { get; set; }
        public string? SequenceName { get; set; }
        public long? EditNo { get; set; }
        public decimal? GrandTotal { get; set; }
        public string? ResultStatus { get; set; }
        public string? PaymentStatus { get; set; }
        public string? LastModified { get; set; }
        public string? HeaderImageUrl { get; set; }
        public string? FooterImageUrl { get; set; }
        public string? LineJsonData { get; set; }
        public string? Gender { get; set; }
        public string? DrName { get; set; }
        public DateTime? CreatedDateTime { get; set; }
        public string? ResultApprovedBy { get; set; }
        public DateTime? ResultApprovedDateTime { get; set; }
        public string? ResultVerifiedBy { get; set; }
        public string? ResultApproveSign { get; set; }
        public string? LabName { get; set; }
        public decimal? BalanceDue { get; set; }
        public decimal? DiscAmount { get; set; }
        public string? DocStatus { get; set; } 
    }

    public class TestEntryLine
    {
        public int? Sequence { get; set; }
        public long? InvoiceNo { get; set; }
        public long? EditNo { get; set; }
        public int? SI_No { get; set; }
        public int? ID { get; set; }
        public string? Name { get; set; }
        public string? Type { get; set; }
        public string? LineStatus { get; set; }
        public long? ClientID { get; set; }
    }

    public class ApiSyncResponse { public List<SyncData>? data { get; set; } }
    public class SyncData { public Header? header { get; set; } public List<ApiSyncItem>? inserted { get; set; } }
    public class Header { public int sequence { get; set; } public long invoiceNo { get; set; } public int editNo { get; set; } }
    public class ApiSyncItem { public int Sequence { get; set; } public long InvoiceNo { get; set; } public int EditNo { get; set; } }
}
