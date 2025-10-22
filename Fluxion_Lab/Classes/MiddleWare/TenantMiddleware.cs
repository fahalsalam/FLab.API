using Fluxion_Lab.Classes.DBOperations;
using Fluxion_Lab.Models.General;

namespace Fluxion_Lab.Classes.MiddleWare
{
    public class TenantContext
    {
        private TenantInfo _tenant;
        public void SetTenant(TenantInfo tenant)
            => _tenant = tenant;    
        public TenantInfo GetTenant() => _tenant;
        public bool IsTenantSet => _tenant != null;
        public string GetConnectionString()
        {
            if (_tenant == null) return null;
            return $"Server={_tenant.DbServer};Database={_tenant.DbName};User Id={_tenant.DbUser};Password={_tenant.DbPassword};Encrypt=True;TrustServerCertificate=True;";
        }
    }

    public class TenantMiddleware
    {
        private readonly RequestDelegate _next;
        public TenantMiddleware(RequestDelegate next)
            => _next = next;

        public async Task InvokeAsync(HttpContext context,
                                      TenantContext tenantContext,
                                      IConfiguration config)
        {
            // 1) On‑prem mode ⇒ skip entirely
            if (config["SaaSOptions:Mode"] == "OnPrem")
            {
                await _next(context);
                return;
            }

            var path = context.Request.Path.Value?.ToLower();

            // 2) Skip your login/register/DDL *and* every /api/7990/*  
            if (path != null && (
                 path.Contains("/api/0102/getauthenticated") ||
                 path.Contains("/api/0102/getappversion") ||
                 path.Contains("/api/0203/synctestdata") ||
                 context.Request.Path.StartsWithSegments(
                     "/api/7990", StringComparison.OrdinalIgnoreCase) || 
                 context.Request.Path.StartsWithSegments(
                     "/api/3343", StringComparison.OrdinalIgnoreCase)|| 
                 context.Request.Path.StartsWithSegments(
                     "/api/7890", StringComparison.OrdinalIgnoreCase)||
                 context.Request.Path.StartsWithSegments(
                     "/api/0303", StringComparison.OrdinalIgnoreCase) ||
                 context.Request.Path.StartsWithSegments(
                     "/api/0208", StringComparison.OrdinalIgnoreCase)
            ))
            {
                await _next(context);
                return;
            }

            // 3) Pull encrypted connection info from the JWT
            var encryptedConnInfo = context.User?.FindFirst("xF")?.Value;
            if (string.IsNullOrEmpty(encryptedConnInfo))
            {
                context.Response.StatusCode = 401;
                await context.Response.WriteAsync(
                    "Missing database connection information.");
                return;
            }

            try
            {
                // 4) Decrypt, split into Server|DB|User|Pwd
                var decrypted = Fluxion_Handler.DecryptString(
                    encryptedConnInfo, Fluxion_Handler.APIString);
                var parts = decrypted.Split('|');
                if (parts.Length != 4)
                {
                    context.Response.StatusCode = 401;
                    await context.Response.WriteAsync(
                        "Invalid connection information format.");
                    return;
                }

                // 5) Build TenantInfo and set in context
                var tenant = new TenantInfo
                {
                    DbServer = parts[0],
                    DbName = parts[1],
                    DbUser = parts[2],
                    DbPassword = parts[3]
                };
                tenantContext.SetTenant(tenant);

                // 6) Continue down the pipeline
                await _next(context);
            }
            catch
            {
                context.Response.StatusCode = 401;
                await context.Response.WriteAsync("Invalid connection information.");
                return;
            }
        }
    }
}
