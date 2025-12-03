using System.Threading.Tasks;
using Fluxion_Lab.Services.Sync.Models;

namespace Fluxion_Lab.Services.Sync.Interfaces
{
    public interface ISyncConfigProvider
    {
        Task<SyncConfig> GetConfigAsync();
    }
}
