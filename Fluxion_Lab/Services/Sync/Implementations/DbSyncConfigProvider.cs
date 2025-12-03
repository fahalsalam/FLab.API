using Dapper;
using Fluxion_Lab.Services.Sync.Interfaces;
using Fluxion_Lab.Services.Sync.Models;
using System.Data;
using System.Threading.Tasks;
using Fluxion_Lab.Services.Sync.Models;

namespace Fluxion_Lab.Services.Sync.Implementations
{
    public class DbSyncConfigProvider : ISyncConfigProvider
    {
        private readonly IDbConnection _dbConnection;

        public DbSyncConfigProvider(IDbConnection dbConnection)
        {
            _dbConnection = dbConnection;
        }

        private async Task<SyncConfig> GetConfigAsyncImpl()
        {
            var config = new SyncConfig
            {
                Enabled = true,
                IntervalMinutes = 5,
                OnlineServer = "localhost",
                LogPath = "SyncLogs",
                ApiUrl = "https://api.fluxionsolution.com/api/7990/syncClientTestData"
            };

            try
            {
                // Attempt to read config columns from mtbl_ClientMaster (single client row expected)
                var sql = "SELECT TOP 1 SyncEnabled, SyncIntervalMinutes, SyncOnlineServer, SyncLogPath, SyncApiUrl FROM mtbl_ClientMaster";
                var row = await _dbConnection.QueryFirstOrDefaultAsync<dynamic>(sql);
                if (row != null)
                {
                    try { config.Enabled = row.SyncEnabled ?? config.Enabled; } catch { }
                    try { config.IntervalMinutes = row.SyncIntervalMinutes ?? config.IntervalMinutes; } catch { }
                    try { config.OnlineServer = row.SyncOnlineServer ?? config.OnlineServer; } catch { }
                    try { config.LogPath = row.SyncLogPath ?? config.LogPath; } catch { }
                    try { config.ApiUrl = row.SyncApiUrl ?? config.ApiUrl; } catch { }
                }
            }
            catch
            {
                // ignore and fallback to defaults
            }

            return config;
        }

        async Task<Fluxion_Lab.Services.Sync.Models.SyncConfig> ISyncConfigProvider.GetConfigAsync()
        {
            return await GetConfigAsyncImpl();
        }
    }
}
