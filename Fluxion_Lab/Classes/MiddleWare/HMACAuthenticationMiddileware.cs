using Microsoft.AspNetCore.DataProtection.KeyManagement;
using System.Security.Cryptography;
using System.Text;

namespace Fluxion_Lab.Classes.MiddleWare
{
    public class HMACAuthenticationMiddileware
    {
        private readonly RequestDelegate _next;
        private readonly string _apiSecret;
        private const string _secret = "XSecret";


        public HMACAuthenticationMiddileware(RequestDelegate next)
        {
            _next = next; 
        }

        public async Task Invoke(HttpContext context)
        {
            string apiKey = context.Request.Headers["X-Api-Key"];
            string timestamp = context.Request.Headers["X-Timestamp"];
            string nonce = context.Request.Headers["X-Nonce"];
            string signature = context.Request.Headers["X-Signature"];

            var appSettings = context.RequestServices.GetRequiredService<IConfiguration>();
            var _apiSecret = appSettings.GetValue<string>(_secret);

             
            // Validate nonce (optional, based on your requirements)
            // You can use a nonce cache to prevent replay attacks

            // Recreate the signature
            var requestBody = await GetRequestBody(context.Request);
            var message = $"{context.Request.Method}\n{timestamp}\n{context.Request.Path}\n{nonce}\n{requestBody}";
            var computedSignature = ComputeHMAC(message);

            // Compare computed signature with the signature from the request
            if (signature != computedSignature)
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                await context.Response.WriteAsync("Unauthorized - Invalid signature");
                return;
            }

            await _next(context);
        }

        private async Task<string> GetRequestBody(HttpRequest request)
        {
            request.EnableBuffering();
            using (var reader = new StreamReader(request.Body, Encoding.UTF8, true, leaveOpen: true))
            {
                var body = await reader.ReadToEndAsync();
                request.Body.Position = 0;
                return body;
            }
        }

        private string ComputeHMAC(string message)
        { 
            using (var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(_apiSecret)))
            {
                var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(message));
                return Convert.ToBase64String(hash);
            }
        }

    }
}
