using Azure.Storage.Blobs;
using Fluxion_Lab.Classes.DatabaseManager;
using Fluxion_Lab.Classes.DBOperations;
using Fluxion_Lab.Controllers.Masters;
using Fluxion_Lab.Models.General;
using Fluxion_Lab.Services.DB_Backup;
using Fluxion_Lab.Services.Masters;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Azure.ServiceBus;
using Microsoft.IdentityModel.Tokens;
using System.Data;
using System.Data.SqlClient;
using System.Text;


namespace Fluxion_Lab.Classes.DependencyInjection
{
    public static class DependencyInjection
    {  
        public static IServiceCollection AddJWT(this IServiceCollection services, IConfiguration _configuration)
        {
            var authkey = Fluxion_Handler.JWtKey();
            services.Configure<JwtKey>(options =>
            {
                options._jwtKey = authkey;
            });

            services.AddAuthentication(item =>
            {
                item.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                item.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            }).AddJwtBearer(item =>
            {
                item.RequireHttpsMetadata = true;
                item.SaveToken = true;
                item.TokenValidationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters()
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(Fluxion_Handler.DecryptString(authkey,Fluxion_Handler.APIString))),
                    ValidateIssuer = false,
                    ValidateAudience = false,
                    ClockSkew = TimeSpan.Zero,
                };
            }); 

            return services;
        }

        public static IServiceCollection ConfigureTenantConnection(this IServiceCollection services, IConfiguration config)
        {
            var mode = config["SaaSOptions:Mode"];

            if (mode == "OnPrem")
            {
                var enc = Environment.GetEnvironmentVariable("ConStr", EnvironmentVariableTarget.Machine);
                if (string.IsNullOrWhiteSpace(enc))
                    throw new InvalidOperationException("Environment variable 'ConStr' is not set.");

                var dec = Fluxion_Handler.DecryptString(enc, Fluxion_Handler.APIString);
                services.AddScoped<IDbConnection>(_ => new SqlConnection(dec));
            }
            //else
            //{
            //    services.AddScoped<IDbConnection>(sp =>
            //    {
            //        var tenantContext = sp.GetRequiredService<TenantContext>();
            //        var config = sp.GetRequiredService<IConfiguration>();
            //        if (!tenantContext.IsTenantSet)
            //        {
            //            // Fallback to metaDB connection for login endpoints
            //            var metaDbConnStr = config.GetConnectionString("metaDB");
            //            if (string.IsNullOrWhiteSpace(metaDbConnStr))
            //            {
            //                return null; 
            //            }
            //            return new SqlConnection(metaDbConnStr);
            //        }
            //        return new SqlConnection(tenantContext.GetConnectionString());
            //    });
            //}
            return services;
        }

        //public static IServiceCollection ConnectionStrings(this IServiceCollection services, IConfiguration configuration)
        //{
        //    // Dapper connection string 

        //    var ecnCon_string = Environment.GetEnvironmentVariable("ConStr", EnvironmentVariableTarget.Machine);
        //    var _decrypt_Con_str = Fluxion_Handler.DecryptString(ecnCon_string, Fluxion_Handler.APIString);
        //    services.AddTransient<IDbConnection>(sp =>
        //    {
        //        var connectionString = _decrypt_Con_str;
        //        return new SqlConnection(connectionString);
        //    });
        //    return services;
        //}

        public static IServiceCollection AddCORS(this IServiceCollection services)
        {

            services.AddCors(options =>
            {
                options.AddDefaultPolicy(builder =>
                {
                    options.AddPolicy("AllowSpecificOrigin", policy =>
                    {
                        policy.WithOrigins("http://localhost:3000")  
                              .AllowAnyHeader()                   
                              .AllowAnyMethod();                  
                    }); 
                });
            }); 
            return services;
        }

        public static IServiceCollection AddBackgroundTaskDependencies(this IServiceCollection services, IConfiguration configuration)
        {
            // Get connection string from configuration
            string blobConnectionString = configuration["AzureStorage:ConnectionString"];
            string containerName = configuration["AzureStorage:ContainerName"];
            string queueName = configuration["AzureStorage:QueueName"];

         // Validate configuration
            if (string.IsNullOrEmpty(blobConnectionString) || blobConnectionString == "YOUR_AZURE_STORAGE_CONNECTION_STRING_HERE")
  {
throw new InvalidOperationException("Azure Storage connection string is not configured. Please set AzureStorage:ConnectionString in appsettings.json or user secrets.");
            }

            // Initialize BlobServiceClient and get BlobContainerClient for the specified container
      var blobServiceClient = new BlobServiceClient(blobConnectionString);
   var blobContainerClient = blobServiceClient.GetBlobContainerClient(containerName);
  services.AddSingleton(blobContainerClient);

       // Initialize QueueClient with the connection string and queue name
        var queueClient = new Azure.Storage.Queues.QueueClient(blobConnectionString, queueName);
     services.AddSingleton(queueClient);

            // Register your services
            //services.AddTransient<IDataSyncService, DataSyncServiceImplementation>();
          //services.AddHostedService<DataSyncBackgroundService>();
            services.AddHostedService<DbBackup>();

         return services;
  }

        public static IServiceCollection AddLocalServiceDependencies(this IServiceCollection services)
        {
            services.AddSingleton<JwtKey>();
            services.AddSingleton<APIResponse>();
            services.AddScoped<TenantContext>(); 
            services.AddControllers();
         
            return services;
        }

    }
}
