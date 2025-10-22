namespace Fluxion_Lab.Models.General
{
    public class TenantInfo
    {
        public long TenantID { get; set; }
        public string DbServer { get; set; }
        public string DbName { get; set; }
        public string DbUser { get; set; }
        public string DbPassword { get; set; }
    }
}
