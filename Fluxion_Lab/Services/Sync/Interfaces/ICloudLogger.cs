using System.Threading.Tasks;

namespace Fluxion_Lab.Services.Sync.Interfaces
{
    public interface ICloudLogger
    {
        Task LogAsync(string message);
        void EnsureDirectory();
        void SetLogDirectory(string path);
    }
}
