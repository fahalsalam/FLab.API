using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using MimeKit;
using Org.BouncyCastle.Asn1.Ocsp;
using MailKit.Net.Smtp;
 

namespace Fluxion_Lab.Controllers.Email
{
    [Route("api/email")] 
    public class EmailController : ControllerBase
    {
        // Your Gmail SMTP settings (could be from appsettings.json)
        private readonly string SmtpServer = "smtp.gmail.com";
        private readonly int SmtpPort = 587;
        private readonly string SenderName = "ATHYAB MAKKA";
        private readonly string SenderEmail = "fahalsalamn44@gmail.com";  // Your Gmail email
        private readonly string SenderPassword = "qabmmtfjfykwxgxe";


        [HttpPost("send-email")]
        public async Task<IActionResult> SendOtp([FromBody] OtpRequest request)
        {
            // Generate OTP
            var otp = new Random().Next(100000, 999999).ToString();

            // Create the email message
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(SenderName, SenderEmail));
            message.To.Add(new MailboxAddress(request.UserName, request.Email));
            message.Subject = "Your OTP Code";

            // Build the HTML email body
            var bodyBuilder = new BodyBuilder
            {
                HtmlBody = $@"
                    <html>
                    <body>
                        <h2>Hello {request.UserName},</h2>
                        <p>Your OTP is <strong>{otp}</strong></p>
                        <p>Enter this code to verify your request.</p>
                        <p>Best regards,<br/>Your Company</p>
                    </body>
                    </html>"
            };
            message.Body = bodyBuilder.ToMessageBody();

            // Send the email via Gmail SMTP
            using (var client = new SmtpClient())
            {
                try
                {
                    await client.ConnectAsync(SmtpServer, SmtpPort, false);
                    await client.AuthenticateAsync(SenderEmail, SenderPassword);
                    await client.SendAsync(message);
                    await client.DisconnectAsync(true);

                    return Ok(new { Message = "OTP sent successfully!" });
                }
                catch (Exception ex)
                {
                    return StatusCode(500, new { Message = "Failed to send OTP", Error = ex.Message });
                }
            }
        } 
        public class OtpRequest
        {
            public string Email { get; set; }
            public string UserName { get; set; }
        } 
    }
}
