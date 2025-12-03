using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Fluxion_Lab.Services.Sync.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace Fluxion_Lab.Services.Sync
{
    public class DataSyncHostedService : IHostedService, IDisposable
    {
        private readonly ILogger<DataSyncHostedService> _logger;
        private readonly IServiceProvider _serviceProvider;
        private readonly IConfiguration _configuration;
        private Timer _timer;
        private int _intervalMinutes = 5;
        public DataSyncHostedService(ILogger<DataSyncHostedService> logger, IServiceProvider serviceProvider, IConfiguration configuration)
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
            _configuration = configuration;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            if (!bool.TryParse(_configuration["Sync:Enabled"], out var enabled) || !enabled)
            {
                // attempt to read DB-config and see if enabled
                try
                {
                    using var scope = _serviceProvider.CreateScope();
                    var cfgProvider = scope.ServiceProvider.GetService<ISyncConfigProvider>();
                    if (cfgProvider != null)
                    {
                        var cfgTask = cfgProvider.GetConfigAsync();
                        cfgTask.Wait();
                        var cfg = cfgTask.Result;
                        if (cfg == null || !cfg.Enabled)
                        {
                            _logger.LogInformation("DataSync is disabled in configuration (appsettings and DB).");
                            return Task.CompletedTask;
                        }
                        enabled = cfg.Enabled;
                    }
                }
                catch
                {
                    _logger.LogInformation("DataSync is disabled in configuration.");
                    return Task.CompletedTask;
                }
            }

            // Prefer DB-configured values if present
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var cfgProvider = scope.ServiceProvider.GetService<ISyncConfigProvider>();
                if (cfgProvider != null)
                {
                    var cfgTask = cfgProvider.GetConfigAsync();
                    cfgTask.Wait();
                    var cfg = cfgTask.Result;
                    if (cfg != null)
                    {
                        enabled = cfg.Enabled;
                        _intervalMinutes = cfg.IntervalMinutes > 0 ? cfg.IntervalMinutes : 5;
                    }
                    else if (!int.TryParse(_configuration["Sync:IntervalMinutes"], out _intervalMinutes))
                    {
                        _intervalMinutes = 5;
                    }
                }
                else if (!int.TryParse(_configuration["Sync:IntervalMinutes"], out _intervalMinutes))
                {
                    _intervalMinutes = 5;
                }
            }
            catch
            {
                if (!int.TryParse(_configuration["Sync:IntervalMinutes"], out _intervalMinutes))
                    _intervalMinutes = 5;
            }

            _logger.LogInformation("DataSyncHostedService starting with interval {minutes} minutes.", _intervalMinutes);
            _timer = new Timer(async _ => await DoWork(), null, TimeSpan.Zero, TimeSpan.FromMinutes(_intervalMinutes));
            return Task.CompletedTask;
        }

        private async Task DoWork()
        {
            try
            {
                // Check if sync is enabled before executing
                bool syncEnabled = false;
                try
                {
                    using var cfgScope = _serviceProvider.CreateScope();
                    var cfgProvider = cfgScope.ServiceProvider.GetService<ISyncConfigProvider>();
                    if (cfgProvider != null)
                    {
                        var cfg = await cfgProvider.GetConfigAsync();
                        if (cfg != null && cfg.Enabled)
                        {
                            syncEnabled = true;
                        }
                        else
                        {
                            _logger.LogInformation("DataSync is disabled in configuration. Skipping sync execution.");
                            return;
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to read sync configuration. Skipping sync execution.");
                    return;
                }

                // Only proceed if sync is enabled
                if (!syncEnabled)
                {
                    _logger.LogInformation("DataSync is not enabled. Skipping sync execution.");
                    return;
                }

                // Resolve scoped IDataSyncService per execution to avoid consuming a scoped service from singleton
                try
                {
                    using var scope = _serviceProvider.CreateScope();
                    var syncService = scope.ServiceProvider.GetRequiredService<Services.Sync.Interfaces.IDataSyncService>();
                    await syncService.SyncTestDataSummary();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to resolve or execute IDataSyncService");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing DataSync job");
            }
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("DataSyncHostedService stopping.");
            _timer?.Change(Timeout.Infinite, 0);
            return Task.CompletedTask;
        }

        public void Dispose()
        {
            _timer?.Dispose();
        }
    }
}
