using Fluxion_Lab.Classes.DependencyInjection;
using Fluxion_Lab.Classes.MiddleWare;
using Fluxion_Lab.Services.Masters;
using Azure.Storage.Blobs;
using Azure.Storage.Queues;
using System.Data;
using System.Data.SqlClient;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using System.IO;

var logPath = Path.Combine(AppContext.BaseDirectory, "Logs");
if (!Directory.Exists(logPath))
{
    Directory.CreateDirectory(logPath);
} 
// Configure Serilog for file logging with absolute path
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .Enrich.FromLogContext()
    .WriteTo.Async(a => a.File(
        path: Path.Combine(AppContext.BaseDirectory, "Logs", "api-.log"),
        rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: 30,
        shared: true,
        outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss} [{Level:u3}] {Message:lj} {Properties:j}{NewLine}{Exception}"
    ))
    .CreateLogger();

var builder = WebApplication.CreateBuilder(args);

// Use Serilog as the logging provider
builder.Host.UseSerilog();

// 1) Core services: JWT, tenant‐connection helper, scoped TenantContext
builder.Services.AddJWT(builder.Configuration);
builder.Services.ConfigureTenantConnection(builder.Configuration);
builder.Services.AddScoped<TenantContext>();

// 2) Bring in your "local" and "background task" dependencies
builder.Services.AddLocalServiceDependencies();
if (builder.Configuration["SaaSOptions:Mode"] == "OnPrem")
{
    builder.Services.AddBackgroundTaskDependencies(builder.Configuration);
}

// 3) Swagger (with JWT bearer UI)
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "Fluxion_Lab API",
        Version = "v1"
    });
    c.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Example: 'Bearer {token}'",
        Name = "Authorization",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT"
    });
    c.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id   = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

var env = builder.Environment;

// 4) Multi‑tenant IDbConnection + DataSync service + Azure clients (SaaS only)
var mode = builder.Configuration["SaaSOptions:Mode"];
string serverString = mode == "OnPrem" ? "103.177.182.183,5734" : "localhost";
if (mode == "SaaS")
{
    // a) Need access to HttpContext inside factories
    builder.Services.AddHttpContextAccessor();

    // b) IDbConnection factory
    builder.Services.AddScoped<IDbConnection>(sp =>
    {
        var tenantContext = sp.GetRequiredService<TenantContext>();
        var httpContext = sp.GetRequiredService<IHttpContextAccessor>()?.HttpContext;
        var path = httpContext?.Request?.Path.Value?.ToLower() ?? "";

        // 1) Already‑set tenant via JWT + TenantMiddleware
        if (tenantContext.IsTenantSet)
            return new SqlConnection(tenantContext.GetConnectionString());

        // 2) Meta‑DB for login + version endpoints
        if (httpContext != null && (path.Contains("/api/0102/getauthenticated")
         || path.Contains("/api/0102/getappversion") || path.Contains("/api/0203/synctestdata")
         || path.Contains("/auth/register")))
        {
            string meta =
                $"Server={serverString};" +
                "Database=db_Fluxion_MetaDB;" +
                "User Id=FS;Password=Fluxion@FS@987;" +
                "Encrypt=True;TrustServerCertificate=True;";
            return new SqlConnection(meta);
        }

        if (httpContext != null && httpContext.Request.Path.StartsWithSegments(
               "/api/3343", StringComparison.OrdinalIgnoreCase))
        {
            string meta =
                $"Server={serverString};" +
                "Database=db_Fluxion_MetaDB;" +
                "User Id=FS;Password=Fluxion@FS@987;" +
                "Encrypt=True;TrustServerCertificate=True;";
            return new SqlConnection(meta);
        }

        // 3) All MobileAppController endpoints (api/7990) go straight to Prod DB
        if (httpContext != null && httpContext.Request.Path.StartsWithSegments(
                "/api/7990", StringComparison.OrdinalIgnoreCase))
        {
            string prod =
                $"Server={serverString};" +
                "Database=db_Fluxion_Prod;" +
                "User Id=FS;Password=Fluxion@FS@987;" +
                "Encrypt=True;TrustServerCertificate=True;";
            return new SqlConnection(prod);
        }

        // 4) Anything else without a tenant is an error
        if (httpContext == null)
            throw new InvalidOperationException("No HTTP context available for tenant resolution.");
        throw new InvalidOperationException(
            "Tenant connection string not set. Ensure TenantMiddleware is enabled and login succeeded.");
    });

    // c) Azure Blob & Queue clients for DataSyncServiceImplementation
    builder.Services.AddSingleton(sp =>
    {
        var cfg = sp.GetRequiredService<IConfiguration>();
        var blobConnStr = cfg.GetConnectionString("AzureStorage");
        var containerName = cfg["BlobOptions:ContainerName"];
        return new BlobContainerClient(blobConnStr, containerName);
    });
    builder.Services.AddSingleton(sp =>
    {
        var cfg = sp.GetRequiredService<IConfiguration>();
        var queueConn = cfg.GetConnectionString("AzureStorage");
        var queueName = cfg["QueueOptions:QueueName"];
        return new QueueClient(queueConn, queueName);
    });
}

// Register sync services for OnPrem mode as well
if (builder.Configuration["SaaSOptions:Mode"] == "OnPrem")
{
    // HttpClient used by DataSync service implementation
    builder.Services.AddHttpClient();
    builder.Services.AddScoped<Fluxion_Lab.Services.Sync.Interfaces.IDataSyncService, Fluxion_Lab.Services.Sync.Implementations.DataSyncServiceImplementation>();
}


//
// 6) CORS, MVC
//
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowSpecificOrigin", policy =>
        policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod());
});
builder.Services.AddControllers();

var app = builder.Build();

app.UseCors("AllowSpecificOrigin");
app.UseStaticFiles();
app.UseSwagger();
app.UseSwaggerUI();

if (builder.Configuration["SaaSOptions:Mode"] == "SaaS")
{
    app.UseAuthentication();
    app.UseMiddleware<TenantMiddleware>();
    app.UseAuthorization();
}

app.MapControllers();
app.Run();
