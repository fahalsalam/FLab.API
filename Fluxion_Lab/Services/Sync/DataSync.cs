using Dapper;
using FastMember;
using Fluxion_Lab.Classes.DBOperations;
using Fluxion_Lab.Helper;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Data.SqlTypes;
using System.Linq;
using System.Net.NetworkInformation;
using System.Threading;
using System.Threading.Tasks;

namespace Fluxion_Lab.Services.Sync
{
    public class DataSync : IHostedService, IDisposable
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<DataSync> _logger;
        private Timer _timer;
        private readonly IDbConnection _localDbConnection;
        private readonly string _onlineConnectionString;
        private const int BatchSize = 1000;

        public DataSync(IServiceProvider serviceProvider, IConfiguration configuration, ILogger<DataSync> logger, IDbConnection localDbConnection)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
            _onlineConnectionString = "Server=localhost;Database=db_Fluxion_Dev;User Id=FS;Password=Fluxion@FS@987;Encrypt=True;TrustServerCertificate=True;";
            _localDbConnection = localDbConnection;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            //_timer = new Timer(DoWork, null, TimeSpan.Zero, TimeSpan.FromSeconds(15)); // Set to 15 seconds for debugging
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _timer?.Change(Timeout.Infinite, 0);
            return Task.CompletedTask;
        }

        public void Dispose()
        {
            _timer?.Dispose();
        }

        private async void DoWork(object state)
        {
            if (!NetworkHelper.IsInternetAvailable())
            {
                _logger.LogWarning("Internet connection is not available. Sync process stopped.");
                return;
            }

            using (var scope = _serviceProvider.CreateScope())
            {
                long clientID = 1001;
                int flag = 100;
                var lastSyncTime = await GetLastSyncTime(clientID);

                try
                {
                    _logger.LogInformation("Starting sync process...");
                    await SyncTable("[Sync].[SP_MergeSectionMasters]", "[Sync].[SP_MasterDataSync]", lastSyncTime, clientID, flag);
                    await UpdateLastSyncTime(clientID);
                    _logger.LogInformation("Sync process completed successfully.");
                }
                catch (Exception ex)
                {
                    await LogErrorAsync(ex, "DoWork", clientID);
                    _logger.LogError($"Error during sync process: {ex.Message}");
                }
            }
        }

        private async Task<string> GetChangesAsJson(string sourceProc,DateTime lastSyncTime, long clientID, int flag)
        {
            var changes = await _localDbConnection.QueryAsync<dynamic>(
                sourceProc,
                new { LastSyncTime = lastSyncTime, Flag = flag },
                commandType: CommandType.StoredProcedure);

            return JsonConvert.SerializeObject(changes);
        }

        private async Task SyncTable(string mergeProc,string sourceProc, DateTime lastSyncTime, long clientID, int flag)
        {
            try
            {
                string jsonChanges = await GetChangesAsJson(sourceProc, lastSyncTime, clientID, flag);

                string decryptConString = Fluxion_Handler.DecryptString(_onlineConnectionString, Fluxion_Handler.APIString);

                using (var onlineConnection = new SqlConnection(decryptConString))
                {
                    await onlineConnection.ExecuteAsync(mergeProc, new { JsonData = jsonChanges }, commandType: CommandType.StoredProcedure);
                }
            }
            catch (Exception ex)
            {
                await LogErrorAsync(ex, "SyncTable", clientID);
                throw;
            }
        }

        private async Task UpdateLastSyncTime(long clientId)
        {
            string decryptConString = Fluxion_Handler.DecryptString(_onlineConnectionString, Fluxion_Handler.APIString);
            using (var connection = new SqlConnection(decryptConString))
            {
                string sql = @"
                    MERGE [Sync].[SyncLog] AS target
                    USING (SELECT @ClientID AS ClientID, @LastSyncTime AS LastSyncTime) AS source (ClientID, LastSyncTime)
                    ON (target.ClientID = source.ClientID)
                    WHEN MATCHED THEN 
                        UPDATE SET LastSyncTime = source.LastSyncTime
                    WHEN NOT MATCHED THEN
                        INSERT (ClientID, LastSyncTime) VALUES (source.ClientID, source.LastSyncTime);";

                await connection.ExecuteAsync(sql, new { ClientID = clientId, LastSyncTime = DateTime.Now });
            }
        }

        private static readonly DateTime SqlMinDateTime = new DateTime(1753, 1, 1);

        private async Task<DateTime> GetLastSyncTime(long clientId)
        {
            string decryptConString = Fluxion_Handler.DecryptString(_onlineConnectionString, Fluxion_Handler.APIString);
            using (var connection = new SqlConnection(decryptConString))
            {
                var result = await connection.QueryFirstOrDefaultAsync<DateTime?>("SELECT LastSyncTime FROM [Sync].[SyncLog] WHERE ClientID = @ClientID", new { ClientID = clientId });
                return result ?? SqlMinDateTime;
            }
        }

        private async Task LogErrorAsync(Exception ex, string functionName, long clientId)
        {
            string decryptConString = Fluxion_Handler.DecryptString(_onlineConnectionString, Fluxion_Handler.APIString);
            using (var connection = new SqlConnection(decryptConString))
            {
                string sql = @"
                    INSERT INTO SyncErrorLog (ClientID, ErrorMessage, StackTrace, ErrorFunction, ErrorLine, ErrorArea, ErrorDateTime)
                    VALUES (@ClientID, @ErrorMessage, @StackTrace, @ErrorFunction, @ErrorLine, @ErrorArea, @ErrorDateTime)";

                var errorDetails = new
                {
                    ClientID = clientId,
                    ErrorMessage = ex.Message,
                    StackTrace = ex.StackTrace,
                    ErrorFunction = functionName,
                    ErrorLine = GetLineNumber(ex),
                    ErrorArea = "DataSync",
                    ErrorDateTime = DateTime.Now
                };

                await connection.ExecuteAsync(sql, errorDetails);
            }
        }

        private int GetLineNumber(Exception ex)
        {
            var stackTrace = new System.Diagnostics.StackTrace(ex, true);
            var frame = stackTrace.GetFrame(0);
            return frame.GetFileLineNumber();
        }
    }

}