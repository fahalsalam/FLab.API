using Dapper;
using Fluxion_Lab.Classes.DBOperations;
using Fluxion_Lab.Models.General;
using Fluxion_Lab.Models.Subscriptions;
using MailKit.Net.Smtp;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Options;
using MimeKit;
using Newtonsoft.Json;
using Razorpay.Api;
using System.Data;
using System.Data.SqlClient;
using static Fluxion_Lab.Models.Razorpay.PaymentGateway; 


namespace Fluxion_Lab.Controllers.Subscriptions
{
    [Route("api/3343")]
    public class SubscriptionController : ControllerBase
    {
        private readonly IDbConnection _onlineDbConnection;
        private readonly IConfiguration _configuration;
        protected APIResponse _response;
        private RazorpayClient _razorpayClient;
        private readonly IDbConnection _dbcontextMeta;

        // Helper to get correct server string
        private string GetServerString()
        {
            var mode = _configuration["SaaSOptions:Mode"];
            return string.Equals(mode, "OnPrem", StringComparison.OrdinalIgnoreCase)
                ? "103.177.182.183,5734"
                : "localhost";
        }
                
        public SubscriptionController(IDbConnection dbcontext, APIResponse response, IConfiguration configuration, IDbConnection _dbcontextMeta)
        {

            _onlineDbConnection = dbcontext;
            _configuration = configuration;
            _response = response;
            _razorpayClient = new RazorpayClient("rzp_test_ZRnXLrkjfBMcJf", "G9ox2tLMH2rLoH8GV2i0u252");
            var connectionString = $"Server={GetServerString()};Database=db_Fluxion_MetaDB;User Id=FS;Password=Fluxion@FS@987;Encrypt=True;TrustServerCertificate=True;";
            _dbcontextMeta = new SqlConnection(connectionString);
        }

        #region Get Plan Details
        [HttpGet("getPlans")]
        public IActionResult GetPlans()
        {
            try
            {
                var constr = $"Server={GetServerString()};Database=db_Fluxion_Prod;User Id=FS;Password=Fluxion@FS@987;Encrypt=True;TrustServerCertificate=True;";

                using (var connection = new SqlConnection(constr))
                {
                    connection.Open();

                    var data = connection.QueryMultiple("FluxionInternal.SP_SubscriptionPlans", commandType: CommandType.StoredProcedure);

                    var plans = data.Read<dynamic>().ToList();
                    var planFeatures = data.Read<dynamic>().ToList();

                    // Group the features by PlanID
                    var featuresByPlan = planFeatures.GroupBy(f => f.PlanID).ToDictionary(g => g.Key, g => g.ToList());

                    // Add the features to the respective plans
                    var plansWithFeatures = plans.Select(plan => new
                    {
                        PlanID = plan.PlanID,
                        PlanName = plan.PlanName,
                        Price = plan.Price,
                        IsDefault = plan.IsDefault,
                        Features = featuresByPlan.ContainsKey(plan.PlanID) ? featuresByPlan[plan.PlanID] : new List<dynamic>()
                    }).ToList();

                    // Create the response
                    var response = new
                    {
                        isSucess = true,
                        message = "Success",
                        data = new
                        {
                            plans = plansWithFeatures
                        }
                    };

                    return Ok(response);
                }
            }
            catch (Exception ex)
            {
                var errorResponse = new
                {
                    isSucess = false,
                    message = ex.Message
                };

                return StatusCode(500, errorResponse);
            }
        }

        #endregion

        #region Razorpay Payment Initialize
            [HttpPost("initialize")]
            public async Task<IActionResult> InitializePayment([FromBody] RazorpayInitilize _am)
            {
                try
                {
                    Random _random = new Random();
                    string _transactionId = _random.Next(0, 10000).ToString();

                    var options = new Dictionary<string, object>
                    {
                        { "amount", Convert.ToDecimal(_am.amount) * 100 }, // Convert to paise
                        { "currency", "INR" },
                        { "receipt", _transactionId },
                        { "payment_capture", true } // Auto capture
                    };

                    var order = _razorpayClient.Order.Create(options);
                    var orderId = order["id"].ToString();

                    var response = new
                    {
                        IsSuccess = true,
                        Message = "Success",
                        Data = orderId
                    };

                    return Ok(response);
                }
                catch (Exception ex)
                {
                    var errorResponse = new
                    {
                        IsSuccess = false,
                        Message = ex.Message,
                        Data = (string)null
                    };

                    return StatusCode(500, errorResponse);
                }
            }

            #endregion

        #region Razorpay Paymet Verification
        [HttpPost("confirm")]
        public async Task<IActionResult> ConfirmPayment([FromHeader] bool? isDemo, [FromBody] ConfirmPaymentPayload confirmPayment)
        {
            try
            {
                string queryTenantMaster = "";
                string queryUserMaster = "";
                string tenantMasterJson = "";
                string userMasterJson = "";

                // Only validate payment signature if not demo
                if (isDemo != true)
                {
                    var attributes = new Dictionary<string, string>
                    {
                        { "razorpay_payment_id", confirmPayment.razorpay_payment_id },
                        { "razorpay_order_id", confirmPayment.razorpay_order_id },
                        { "razorpay_signature", confirmPayment.razorpay_signature }
                    };

                    if (!Utils.ValidatePaymentSignature(attributes))
                        return BadRequest(new { IsSuccess = false, Message = "Invalid payment signature." });
                }

                PaymentDetails paymentData;
                if (isDemo == false)
                {
                    var payment = _razorpayClient.Payment.Fetch(confirmPayment.razorpay_payment_id);
                    if (payment["status"].ToString() != "captured")
                        return BadRequest(new { IsSuccess = false, Message = "Payment status is not captured." });

                    var createdAtIST = ConvertUnixTimeToIST(long.Parse(payment["created_at"].ToString()));
                    paymentData = new PaymentDetails
                    {
                        Amount = (Convert.ToDecimal(payment["amount"]) / 100).ToString("F2"),
                        Method = payment["method"].ToString(),
                        Currency = payment["currency"].ToString(),
                        Status = payment["status"].ToString(),
                        OrderId = payment["order_id"].ToString(),
                        CreatedAt = createdAtIST.ToString("yyyy-MM-dd HH:mm:ss"),
                        Payment_Id = confirmPayment.razorpay_payment_id
                    };
                }
                else
                {
                    paymentData = new PaymentDetails
                    {
                        Amount = "100.00",
                        Method = "test_method",
                        Currency = "INR",
                        Status = "captured",
                        OrderId = "test_order_id",
                        CreatedAt = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss"),
                        Payment_Id = confirmPayment.razorpay_payment_id
                    };
                }

                var tempData = new
                {
                    ClientUniqueID = Guid.NewGuid(),
                    UserCode = GenerateUniqueUserCode(),
                    Password = GenerateSecurePassword()
                };

                var constr = $"Server={GetServerString()};Database=db_Fluxion_MetaDB;User Id=FS;Password=Fluxion@FS@987;Encrypt=True;TrustServerCertificate=True;";
                string clientID;
                using (var connection = new SqlConnection(constr))
                {
                    var parameters = new DynamicParameters();
                    parameters.Add("@TempAID", confirmPayment.TempAID);
                    parameters.Add("@PaymentJson", JsonConvert.SerializeObject(paymentData));
                    parameters.Add("@UserDataJson", JsonConvert.SerializeObject(tempData));
                    clientID = connection.Query<getTenatDetails>("SP_TenantMasterPOST", parameters, commandType: CommandType.StoredProcedure)
                                         .FirstOrDefault()?.ClientID;
                    if (string.IsNullOrEmpty(clientID))
                        return BadRequest(new { IsSuccess = false, Message = "Client ID not found." });

                    queryTenantMaster = $"SELECT * FROM [dbo].[mtbl_Tenant_Master] WHERE ClientID = '{clientID}'";
                    queryUserMaster = $@"SELECT A.* FROM [dbo].[mtbl_Tenant_User_Master] A 
                                                 JOIN [dbo].[mtbl_Tenant_Master] B ON A.ClientID = B.ClientID 
                                                   WHERE B.ClientID = '{clientID}'";

                    using (var detailsConnection = new SqlConnection(constr))
                    {
                        detailsConnection.Open();
                        var tenantMasterData = detailsConnection.Query(queryTenantMaster, new { ClientID = clientID });
                        var userMasterData = detailsConnection.Query(queryUserMaster, new { ClientID = clientID });
                        tenantMasterJson = JsonConvert.SerializeObject(tenantMasterData);
                        userMasterJson = JsonConvert.SerializeObject(userMasterData);
                    }
                }

                if (isDemo == true)
                {

                    var cloudConnectionString = $"Server={GetServerString()};Database=db_Fluxion_ProdV1;User Id=FS;Password=Fluxion@FS@987;Encrypt=True;TrustServerCertificate=True;";

                    using (var cloudConnection = new SqlConnection(cloudConnectionString))
                    {
                        var parameters1 = new DynamicParameters();
                        parameters1.Add("@ClientID", clientID);
                        parameters1.Add("@ClientMaster", tenantMasterJson);
                        parameters1.Add("@UserMaster", userMasterJson);
                        cloudConnection.Execute("[dbo].[SP_InsertDefaultData]", parameters1, commandType: CommandType.StoredProcedure);
                    }
                } 
              
                Task.Run(() => SendCredentialsEmailAsync("swalihmp438368@gmail.com", "swalih", tempData.UserCode, tempData.Password));
               

                return Ok(new
                {
                    isSucess = true,
                    message = "Success",
                    data = new { paymentData, clientDetails = tempData }
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    IsSuccess = false,
                    Message = "An error occurred during payment confirmation.",
                    Error = ex.Message
                });
            }
        } 

        #endregion

        #region Convert UNIX DateTime
        public static DateTimeOffset ConvertUnixTimeToIST(long unixTime)
        {
            DateTimeOffset dateTimeOffset = DateTimeOffset.FromUnixTimeSeconds(unixTime);
            TimeZoneInfo istZone = TimeZoneInfo.FindSystemTimeZoneById("India Standard Time");
            DateTimeOffset istTime = TimeZoneInfo.ConvertTime(dateTimeOffset, istZone);
            return istTime;
        }
        #endregion

        #region Tenant Sign-Up
        [HttpPost("TenantRegistration")]
        public IActionResult TenantRegistration([FromBody] TenantRegistration tenant)
        {
            try
            { 
                // Convert the object to JSON
                var jsonData = JsonConvert.SerializeObject(tenant);

                // SQL query to insert data and return the last inserted ID

                var constr1 = $"Server={GetServerString()};Database=db_Fluxion_MetaDB;User Id=FS;Password=Fluxion@FS@987;Encrypt=True;TrustServerCertificate=True;";

                using (var connection = new SqlConnection(constr1))
                {
                    connection.Open();
                    string storedProcedure = "InsertClientTempRegistration";
                    var lastInsertedId = connection.ExecuteScalar<long>(storedProcedure, new { JsonData = jsonData }, commandType: CommandType.StoredProcedure);
                    // Return the last inserted ID back to the client
                    return Ok(new { Success = true, TempAID = lastInsertedId }); 
                } 
                
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        #endregion

        #region getClients
        [HttpGet("getClients")]
        public IActionResult getClients()
        {
            try
            {
                var constr1 = $"Server={GetServerString()};Database=db_Fluxion_MetaDB;User Id=FS;Password=Fluxion@FS@987;Encrypt=True;TrustServerCertificate=True;";

                using (var connection = new SqlConnection(constr1))
                {
                    connection.Open();
                    var parameters = new DynamicParameters();
                    parameters.Add("@Flag", 100);

                    var data = connection.Query("SP_GetClientDetails", parameters, commandType: CommandType.StoredProcedure);

                    _response.isSucess = true;
                    _response.message = "Sucess";
                    _response.data = data;

                    return Ok(_response);

                } 
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }
        #endregion

        #region Update AMC Amount
        [HttpPost("putClientAMCAmount")]
        public IActionResult putClientAMCAmount([FromHeader] int clientID, [FromHeader] decimal amcAmount)
        {
            try
            {
                var constr1 = $"Server={GetServerString()};Database=db_Fluxion_MetaDB;User Id=FS;Password=Fluxion@FS@987;Encrypt=True;TrustServerCertificate=True;";

                using (var connection = new SqlConnection(constr1))
                {
                    connection.Open();
                    var parameters = new DynamicParameters();
                    parameters.Add("@Flag", 101);
                    parameters.Add("@ClientID", clientID);
                    parameters.Add("@AMCAmount", amcAmount); 

                    var data = connection.Query("SP_GetClientDetails", parameters, commandType: CommandType.StoredProcedure);

                    _response.isSucess = true;
                    _response.message = "Sucess";
                    _response.data = data;

                    return Ok(_response);

                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        #endregion

        private async Task SendCredentialsEmailAsync(string email, string userName, string userId, string password)
        {
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress("Fluxion IT Solutions", "fluxionitsolutions@gmail.com"));
            message.To.Add(new MailboxAddress(userName, email));
            message.Subject = "Your Fluxion Account Credentials";
            var bodyBuilder = new BodyBuilder
            {
                HtmlBody = @"<html><head><style>body { font-family: Arial, sans-serif; background: #f4f4f4; } .container { background: #fff; padding: 30px; border-radius: 8px; box-shadow: 0 2px 8px #e0e0e0; max-width: 500px; margin: 40px auto; } h2 { color: #2d7ff9; } .credentials { background: #f0f8ff; padding: 15px; border-radius: 6px; margin: 20px 0; } .footer { font-size: 12px; color: #888; margin-top: 30px; }</style></head><body><div class='container'><h2>Welcome to Fluxion!</h2><p>Dear " + userName + @",</p><p>Your account has been created. Please find your login credentials below:</p><div class='credentials'><strong>User ID:</strong> " + userId + @"<br/><strong>Password:</strong> " + password + @"</div><p>Login at: <a href='https://fluxionitsolutions.com/login'>https://fluxionitsolutions.com/login</a></p><p>If you have any questions, feel free to contact our support team.</p><div class='footer'>&copy; 2024 Fluxion IT Solutions. All rights reserved.</div></div></body></html>"
            };
            message.Body = bodyBuilder.ToMessageBody();
            using (var client = new SmtpClient())
            {
                try
                {
                    await client.ConnectAsync("smtp.gmail.com", 587, false);
                    await client.AuthenticateAsync("fluxionitsolutions@gmail.com", "cjbr byrh jyop jzvc");
                    await client.SendAsync(message);
                    await client.DisconnectAsync(true);
                }
                catch
                {
                    // Log or handle email errors as needed
                }
            }
        }  

        private static string GenerateUniqueUserCode()
        {
            // Example: "USR" + 6 random digits
            var random = new Random();
            int number = random.Next(100000, 1000000); // Ensures 6 digits
            return $"USR{number}";
        }

        private static string GenerateSecurePassword()
        {
            // Generate a 5-digit random number as a string
            var random = new Random();
            int number = random.Next(10000, 100000); // Ensures 5 digits
            return number.ToString();
        }

        public class getTenatDetails
        { 
            public string ClientID { get; set; } 
        }
    }
}
