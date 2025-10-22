using Fluxion_Lab.Models.General;

namespace Fluxion_Lab.Classes.DatabaseManager
{
    public class TenantContext
    {
        private TenantInfo _tenant;

        public void SetTenant(TenantInfo tenant) => _tenant = tenant;
        public TenantInfo GetTenant() => _tenant;
        public bool IsTenantSet => _tenant != null;

        public string GetConnectionString()
        {
            if (_tenant == null)
                throw new InvalidOperationException("Tenant not initialized.");

            return $"Server={_tenant.DbServer};Database={_tenant.DbName};User Id={_tenant.DbUser};Password={_tenant.DbPassword};TrustServerCertificate=True;";
        }
    }
}
