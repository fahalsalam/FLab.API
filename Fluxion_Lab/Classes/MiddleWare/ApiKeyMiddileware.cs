using Microsoft.AspNetCore.Http;
using System.Text.Json;

namespace Fluxion_Lab.Classes.MiddleWare
{
    public class ApiKeyMiddileware
    {
        private readonly RequestDelegate _next;
        private const string APIKEY = "XApiKey";

        public ApiKeyMiddileware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            if (!context.Request.Headers.TryGetValue(APIKEY, out var extractedApiKey))
            {
                context.Response.StatusCode = 401;
                await context.Response.WriteAsync("Key was not provided"); 
                return;
            }

            var appSettings = context.RequestServices.GetRequiredService<IConfiguration>();
            var apiKey = appSettings.GetValue<string>(APIKEY);
            if (!apiKey.Equals(extractedApiKey))
            {
                context.Response.StatusCode = 401;
                await context.Response.WriteAsync("Unauthorized client");
                return;
            }

            await _next(context);
        }  
    }
}
