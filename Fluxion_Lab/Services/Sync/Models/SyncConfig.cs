namespace Fluxion_Lab.Services.Sync.Models
{
    public class SyncConfig
    {
        public bool Enabled { get; set; }
        public int IntervalMinutes { get; set; }
        public string OnlineServer { get; set; }
        public string LogPath { get; set; }
        public string ApiUrl { get; set; }
    }
}
