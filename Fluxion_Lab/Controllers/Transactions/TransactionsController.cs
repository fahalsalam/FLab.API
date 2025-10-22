using Dapper;
using Fluxion_Lab.Classes.DBOperations;
using Fluxion_Lab.Models.General;
using Fluxion_Lab.Models.Masters.PatientMaster;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Razorpay.Api;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Text.Json;
using static Fluxion_Lab.Controllers.Authentication.AuthenticationController;
using static Fluxion_Lab.Models.DataSync.DataSync;
using static Fluxion_Lab.Models.General.BodyParams;
using static Fluxion_Lab.Models.Masters.Machine_Analyzer.MachineAnalyzer;
using static Fluxion_Lab.Models.Transactions.Payments.PaymentPOST;
using static Fluxion_Lab.Models.Transactions.Purchase.PurchaseDto;
using static Fluxion_Lab.Models.Transactions.TestEntries.TestEntries;
using Microsoft.Extensions.Logging;

namespace Fluxion_Lab.Controllers.Transactions
{
    [Route("api/0203")]

    public class TransactionsController : ControllerBase
    {
        private readonly JwtKey _key;
        private readonly IDbConnection _dbcontext;
        protected APIResponse _response;
        private readonly string onlineConnectionString;
        private readonly ILogger<TransactionsController> _logger;

        public TransactionsController(IOptions<JwtKey> options, IDbConnection dbcontext, APIResponse response, IConfiguration configuration, ILogger<TransactionsController> logger)
        {
            this._key = options.Value;
            _dbcontext = dbcontext;
            _response = response;
            onlineConnectionString = "Server=localhost;Database=db_Fluxion_Prod;User Id=FS;Password=Fluxion@FS@987;Encrypt=True;TrustServerCertificate=True;";
            _logger = logger;
        }

        #region Purchase Section

        #region Purchase Page Load
        [HttpGet("getPurchasePageLoadDataset")]
        public IActionResult GetPurchasePageLoadDataset()
        {
            try
            {
                string token = Request.Headers["Authorization"];
                token = token.Substring(7);

                var tokenClaims = Fluxion_Handler.GetJWTTokenClaims(token, _key._jwtKey, true);

                var parameters = new DynamicParameters();
                parameters.Add("@Flag", 100);
                parameters.Add("@ClientID", tokenClaims.ClientId);

                var data = _dbcontext.QueryMultiple("SP_Purchase", parameters, commandType: CommandType.StoredProcedure);
                var _nextNum = data.Read<dynamic>().ToList();
                var _itemList = data.Read<dynamic>().ToList();
                var _supplierList = data.Read<dynamic>().ToList();

                var Response = new
                {
                    isSucess = true,
                    message = "Success",
                    data = new
                    {
                        nextNum = _nextNum,
                        itemList = _itemList,
                        supplierList = _supplierList,
                    }
                };
                return Ok(Response);
            }
            catch (Exception ex)
            {
                _response.isSucess = false;
                _response.message = ex.Message;

                return StatusCode(500, _response);
            }
        }
        #endregion

        #region Purchase POST Transaction
        [HttpPost("postPurchaseTransaction")]
        public IActionResult PostPurchaseTransaction([FromHeader] long InvoiceNo, [FromHeader] long Sequence, [FromHeader] int EditNo, [FromHeader] string DocStatus, [FromBody] PurchaseData _purchase)
        {
            try
            {
                string token = Request.Headers["Authorization"];
                token = token.Substring(7);

                var tokenClaims = Fluxion_Handler.GetJWTTokenClaims(token, _key._jwtKey, true);

                var parameters = new DynamicParameters();
                parameters.Add("@ClientID", tokenClaims.ClientId);
                parameters.Add("@InvoiceNo", InvoiceNo);
                parameters.Add("@Sequence", Sequence);
                parameters.Add("@EditNo", EditNo);
                parameters.Add("@DocStatus", DocStatus);

                parameters.Add("@JsonData", JsonConvert.SerializeObject(_purchase));
                parameters.Add("@UserID", tokenClaims.UserId);

                var data = _dbcontext.Query("SP_PurchasePOST", parameters, commandType: CommandType.StoredProcedure);

                _response.isSucess = true;
                _response.message = "Success";
                _response.data = data;


                return Ok(_response);

            }
            catch (Exception ex)
            {
                _response.isSucess = false;
                _response.message = ex.Message;
                return StatusCode(500, _response);
            }
        }
        #endregion

        #region Purchase List Page Combo
        [HttpGet("getPurchaseListCombo")]
        public IActionResult getPurchaseListCombo()
        {
            try
            {
                string token = Request.Headers["Authorization"];
                token = token.Substring(7);

                var tokenClaims = Fluxion_Handler.GetJWTTokenClaims(token, _key._jwtKey, true);

                var parameters = new DynamicParameters();
                parameters.Add("@Flag", 105);
                parameters.Add("@ClientID", tokenClaims.ClientId);

                var data = _dbcontext.QueryMultiple("SP_Purchase", parameters, commandType: CommandType.StoredProcedure);

                var _supplierList = data.Read<dynamic>().ToList();
                var _itemList = data.Read<dynamic>().ToList();

                var Response = new
                {
                    isSucess = true,
                    message = "Success",
                    data = new
                    {
                        supplierList = _supplierList,
                        itemList = _itemList
                    }
                };
                return Ok(Response);
            }
            catch (Exception ex)
            {
                _response.isSucess = false;
                _response.message = ex.Message;

                return StatusCode(500, _response);
            }
        }
        #endregion

        #region List Of Purchases
        [HttpPost("getPurchaseList")]
        public IActionResult GetPurchaseList([FromBody] PurchaseList _pr)
        {
            try
            {
                int? flag = 0;
                string token = Request.Headers["Authorization"];
                token = token.Substring(7);

                var tokenClaims = Fluxion_Handler.GetJWTTokenClaims(token, _key._jwtKey, true);

                flag = string.IsNullOrEmpty(_pr.ItemNo.ToString()) ? 103 : 102;

                var parameters = new DynamicParameters();
                parameters.Add("@Flag", flag);
                parameters.Add("@ClientID", tokenClaims.ClientId);
                parameters.Add("@PageNo", 1);
                parameters.Add("@PageSize", 10);
                parameters.Add("@FromDate", _pr.FromDate);
                parameters.Add("@ToDate", _pr.ToDate);
                parameters.Add("@SupplierID", _pr.SupplierID);
                parameters.Add("@ItemNo", _pr.ItemNo);

                var data = _dbcontext.Query("SP_Purchase", parameters, commandType: CommandType.StoredProcedure);

                _response.isSucess = true;
                _response.message = "Sucess";
                _response.data = data;

                return Ok(_response);
            }
            catch (Exception ex)
            {
                _response.isSucess = false;
                _response.message = ex.Message;

                return StatusCode(500, _response);
            }
        }
        #endregion

        #region Get Single Purchase Entry Details
        [HttpGet("getPurchaseDetailsByID")]
        public IActionResult GetPurchaseDetailsByID([FromHeader] int? sequence, [FromHeader] long? invoiceNo, [FromHeader] long? editNo)
        {
            try
            {
                string token = Request.Headers["Authorization"];
                token = token.Substring(7);

                var tokenClaims = Fluxion_Handler.GetJWTTokenClaims(token, _key._jwtKey, true);

                var parameters = new DynamicParameters();
                parameters.Add("@Flag", 104);
                parameters.Add("@ClientID", tokenClaims.ClientId);
                parameters.Add("@Sequence", sequence);
                parameters.Add("@InvoiceNo", invoiceNo);
                parameters.Add("@EditNo", editNo);

                var data = _dbcontext.QueryMultiple("SP_Purchase", parameters, commandType: CommandType.StoredProcedure);

                var _prHeader = data.Read<dynamic>().ToList();
                var _prDetails = data.Read<dynamic>().ToList();

                var Response = new
                {
                    isSucess = true,
                    message = "Success",
                    data = new
                    {
                        purchaseHd = _prHeader,
                        purchaseLn = _prDetails
                    }
                };
                return Ok(Response);

                return Ok(_response);
            }
            catch (Exception ex)
            {
                _response.isSucess = false;
                _response.message = ex.Message;

                return StatusCode(500, _response);
            }
        }
        #endregion

        #region GET Supplier Purchase Amount Details
        [HttpGet("getSupplierAmountDetails")]
        public IActionResult getSupplierAmountDetails([FromHeader] long supplierID)
        {
            try
            {
                string token = Request.Headers["Authorization"];
                token = token.Substring(7);

                var tokenClaims = Fluxion_Handler.GetJWTTokenClaims(token, _key._jwtKey, true);

                var parameters = new DynamicParameters();
                parameters.Add("@Flag", 106);
                parameters.Add("@ClientID", tokenClaims.ClientId);
                parameters.Add("@SupplierID", supplierID);
                var data = _dbcontext.Query("SP_Purchase", parameters, commandType: CommandType.StoredProcedure);

                _response.isSucess = true;
                _response.message = "Sucess";
                _response.data = data;

                return Ok(_response);
            }
            catch (Exception ex)
            {
                _response.isSucess = false;
                _response.message = ex.Message;

                return StatusCode(500, _response);
            }
        }
        #endregion

        #region Purchase Invoice Details By ID
        [HttpGet("getPurchaseInvoiceDetails")]
        public IActionResult getPurchaseInvoiceDetails([FromHeader] int _sequence, [FromHeader] long invoiceNo, [FromHeader] int editNo)
        {
            try
            {
                string token = Request.Headers["Authorization"];
                token = token.Substring(7);

                var tokenClaims = Fluxion_Handler.GetJWTTokenClaims(token, _key._jwtKey, true);

                var parameters = new DynamicParameters();
                parameters.Add("@Flag", 107);
                parameters.Add("@ClientID", tokenClaims.ClientId);
                parameters.Add("@Sequence", _sequence);
                parameters.Add("@InvoiceNo", invoiceNo);
                parameters.Add("@EditNo", editNo);

                var data = _dbcontext.QueryMultiple("SP_Purchase", parameters, commandType: CommandType.StoredProcedure);


                var _invHdrDetails = data.Read<dynamic>().ToList();
                var _invLnDetails = data.Read<dynamic>().ToList();
                var _invBatchDetails = data.Read<dynamic>().ToList();


                var Response = new
                {
                    isSucess = true,
                    message = "Success",
                    data = new
                    {
                        invHeader = _invHdrDetails,
                        invLineDetail = _invLnDetails,
                        invBatchDetails = _invBatchDetails
                    }
                };
                return Ok(Response);

            }
            catch (Exception ex)
            {
                _response.isSucess = false;
                _response.message = ex.Message;

                return StatusCode(500, _response);
            }
        }

        #endregion

        #region Test Entry Transaction

        #region POST Test Entry
        [HttpPost("postTestEntryTransaction")]
        public async Task<IActionResult> PostTestEntryTransaction([FromHeader] long InvoiceNo, [FromHeader] int Sequence, [FromHeader] int EditNo,
        [FromHeader] string DocStatus, [FromBody] TestEntriesData _trn)
        {
            try
            {
                string token = Request.Headers["Authorization"];
                token = token.Substring(7);

                var tokenClaims = Fluxion_Handler.GetJWTTokenClaims(token, _key._jwtKey, true);

                var parameters = new DynamicParameters();
                
                parameters.Add("@ClientID", tokenClaims.ClientId);
                parameters.Add("@InvoiceNo", InvoiceNo);
                parameters.Add("@Sequece", Sequence);
                parameters.Add("@EditNo", EditNo);
                parameters.Add("@JsonData", JsonConvert.SerializeObject(_trn));
                parameters.Add("@UserID", tokenClaims.UserId);
                parameters.Add("@DocStatus", DocStatus);

                var data = await _dbcontext.QueryAsync("SP_TestEntriesPOST", parameters, commandType: CommandType.StoredProcedure); 
               
                _response.isSucess = true;
                _response.message = "Success";
                _response.data = data;

                return Ok(_response);

            }
            catch (Exception ex)
            {
                _response.isSucess = false;
                _response.message = ex.Message;
                return StatusCode(500, _response);
            }
        }

        #endregion

        #region Update Test Entry Lines Order
        [HttpPost("updateTestEntryLineOrder")]
        public IActionResult GetPatientRecords([FromHeader] int sequence, [FromHeader] long invoiceNo, [FromHeader] int editNo, [FromBody] EntryOrderChangeRequest _od)
        {
            try
            {
                string token = Request.Headers["Authorization"];
                token = token.Substring(7);

                var tokenClaims = Fluxion_Handler.GetJWTTokenClaims(token, _key._jwtKey, true);

                var parameters = new DynamicParameters();
                parameters.Add("@Flag", 112);
                parameters.Add("@ClientID", tokenClaims.ClientId);
                parameters.Add("@InvoiceNo", invoiceNo);
                parameters.Add("@Sequence", sequence);
                parameters.Add("@EditNo", editNo);
                parameters.Add("@JsonData", JsonConvert.SerializeObject(_od)); 

                var data = _dbcontext.Query("SP_TestEntry", parameters, commandType: CommandType.StoredProcedure);

                _response.isSucess = true;
                _response.message = "Sucess";
                _response.data = data;

                return Ok(_response);
            }
            catch (Exception ex)
            {
                _response.isSucess = false;
                _response.message = ex.Message;

                return StatusCode(500, _response);
            }
        }
        #endregion

        #region Test Entry Page Load
        [HttpGet("getTestEtryPageLoadDataset")]
        public IActionResult getTestEtryPageLoadDataset()
        {
            try
            {
                string token = Request.Headers["Authorization"];
                token = token.Substring(7);

                var tokenClaims = Fluxion_Handler.GetJWTTokenClaims(token, _key._jwtKey, true);

                var parameters = new DynamicParameters();
                parameters.Add("@Flag", 100);
                parameters.Add("@ClientID", tokenClaims.ClientId);

                var data = _dbcontext.QueryMultiple("SP_TestEntry", parameters, commandType: CommandType.StoredProcedure);
                
                var _nextNum = data.Read<dynamic>().ToList();
                var _itemsList = data.Read<dynamic>().ToList();
                var _doctor = data.Read<dynamic>().ToList();
                var _lab = data.Read<dynamic>().ToList();
                var _syringe = data.Read<dynamic>().ToList();
                var _place = data.Read<dynamic>().ToList();
                var _clientMasterDetails = data.Read<dynamic>().ToList();
                var _cardDetails = data.Read<dynamic>().ToList();


                var Response = new
                {
                    isSucess = true,
                    message = "Success",
                    data = new
                    {
                        nextNum = _nextNum,
                        itemsList = _itemsList,
                        doctors = _doctor,
                        labs = _lab,
                        syringe = _syringe,
                        place = _place,
                        clientMasterDetails = _clientMasterDetails,
                        cardDetails = _cardDetails
                    }
                };
                return Ok(Response);
            }
            catch (Exception ex)
            {
                _response.isSucess = false;
                _response.message = ex.Message;

                return StatusCode(500, _response);
            }
        }
        #endregion

        #region Get Patient Last 10 Records
        [HttpGet("getPatientRecords")]
        public IActionResult GetPatientRecords([FromHeader] long? patientID)
        {
            try
            {
                string token = Request.Headers["Authorization"];
                token = token.Substring(7);

                var tokenClaims = Fluxion_Handler.GetJWTTokenClaims(token, _key._jwtKey, true);

                var parameters = new DynamicParameters();
                parameters.Add("@Flag", 101);
                parameters.Add("@ClientID", tokenClaims.ClientId);
                parameters.Add("@PatientID", patientID);
                var data = _dbcontext.Query("SP_TestEntry", parameters, commandType: CommandType.StoredProcedure);

                _response.isSucess = true;
                _response.message = "Sucess";
                _response.data = data;

                return Ok(_response);
            }
            catch (Exception ex)
            {
                _response.isSucess = false;
                _response.message = ex.Message;

                return StatusCode(500, _response);
            }
        }
        #endregion

        #region Patient Records By Date
        [HttpGet("getPatientRecordsByDate")]
        public IActionResult GetPatientRecordsByDate([FromHeader] string? _date)
        {
            try
            {
                string token = Request.Headers["Authorization"];
                token = token.Substring(7);

                var tokenClaims = Fluxion_Handler.GetJWTTokenClaims(token, _key._jwtKey, true);

                var parameters = new DynamicParameters();
                parameters.Add("@Flag", 102);
                parameters.Add("@ClientID", tokenClaims.ClientId);
                parameters.Add("@Date", _date);
                var data = _dbcontext.Query("SP_TestEntry", parameters, commandType: CommandType.StoredProcedure);

                _response.isSucess = true;
                _response.message = "Sucess";
                _response.data = data;

                return Ok(_response);
            }
            catch (Exception ex)
            {
                _response.isSucess = false;
                _response.message = ex.Message;

                return StatusCode(500, _response);
            }
        }
        #endregion

        #region Invoice Details By ID
        [HttpGet("getInvoiceDetailsByID")]
        public IActionResult GetInvoiceDetailsByID([FromHeader] int _sequence, [FromHeader] long invoiceNo, [FromHeader] int editNo)
        {
            try
            {
                string token = Request.Headers["Authorization"];
                token = token.Substring(7);

                var tokenClaims = Fluxion_Handler.GetJWTTokenClaims(token, _key._jwtKey, true);

                var parameters = new DynamicParameters();
                parameters.Add("@Flag", 103);
                parameters.Add("@ClientID", tokenClaims.ClientId);
                parameters.Add("@Sequence", _sequence);
                parameters.Add("@InvoiceNo", invoiceNo);
                parameters.Add("@EditNo", editNo);

                var data = _dbcontext.QueryMultiple("SP_TestEntry", parameters, commandType: CommandType.StoredProcedure);


                var _invDetails = data.Read<dynamic>().ToList();
                var _receiptHistory = data.Read<dynamic>().ToList();

                var Response = new
                {
                    isSucess = true,
                    message = "Success",
                    data = new
                    {
                        invDetails = _invDetails,
                        receiptHistory = _receiptHistory
                    }
                };
                return Ok(Response);

            }
            catch (Exception ex)
            {
                _response.isSucess = false;
                _response.message = ex.Message;

                return StatusCode(500, _response);
            }
        }
        #endregion

        #region Invoice Details By ID
        [HttpGet("getInvoiceDetails")]
        public IActionResult GetInvoiceDetails([FromHeader] int _sequence, [FromHeader] long invoiceNo, [FromHeader] int editNo)
        {
            try
            {
                string token = Request.Headers["Authorization"];
                token = token.Substring(7);

                var tokenClaims = Fluxion_Handler.GetJWTTokenClaims(token, _key._jwtKey, true);

                var parameters = new DynamicParameters();
                parameters.Add("@Flag", 106);
                parameters.Add("@ClientID", tokenClaims.ClientId);
                parameters.Add("@Sequence", _sequence);
                parameters.Add("@InvoiceNo", invoiceNo);
                parameters.Add("@EditNo", editNo);

                var data = _dbcontext.QueryMultiple("SP_TestEntry", parameters, commandType: CommandType.StoredProcedure);


                var _invHdrDetails = data.Read<dynamic>().ToList();
                var _invLnDetails = data.Read<dynamic>().ToList();

                var Response = new
                {
                    isSucess = true,
                    message = "Success",
                    data = new
                    {
                        invHeader = _invHdrDetails,
                        invLineDetail = _invLnDetails
                    }
                };
                return Ok(Response);

            }
            catch (Exception ex)
            {
                _response.isSucess = false;
                _response.message = ex.Message;

                return StatusCode(500, _response);
            }
        }

        #endregion

        #region Test Details BY ID
        [HttpGet("getInvoiceTestDetailsByID")]
        public IActionResult GetInvoiceTestDetailsByID([FromHeader] int _sequence, [FromHeader] long invoiceNo, [FromHeader] int editNo)
        {
            try
            {
                string token = Request.Headers["Authorization"];
                token = token.Substring(7);

                var tokenClaims = Fluxion_Handler.GetJWTTokenClaims(token, _key._jwtKey, true);

                var parameters = new DynamicParameters();
                parameters.Add("@Flag", 105);
                parameters.Add("@ClientID", tokenClaims.ClientId);
                parameters.Add("@Sequence", _sequence);
                parameters.Add("@InvoiceNo", invoiceNo);
                parameters.Add("@EditNo", editNo);

                var data = _dbcontext.QueryMultiple("SP_TestEntry", parameters, commandType: CommandType.StoredProcedure);

                var _testData = data.Read<dynamic>().ToList();
                var _patientData = data.Read<dynamic>().ToList();

                // Attempt to read `_prevHistory` directly as `TestHistory` objects
                var _prevHistory = data.Read<TestHistory>().ToList();

                var groupedData = new List<GroupChild>();

                foreach (var group in _testData)
                {
                    var testHistory = new List<TestHistory>();

                    // Filter `_prevHistory` items by group ID and add to `testHistory`
                    foreach (var child in _prevHistory)
                    {
                        if (child.ID == group.ID)
                        {
                            testHistory.Add(child);
                        }
                    }

                    // Create a new `GroupChild` object and assign tests to it
                    var TestsWithHistory = new GroupChild
                    {
                        SI_No = group.SI_No,
                        ID = group.ID,
                        Name = group.Name,
                        LableType = group.LableType,
                        Value = group.Value,
                        Action = group.Action,
                        Comments = group.Comments,
                        TestSection = group.TestSection,
                        HighValue = group.HighValue,
                        LowValue = group.LowValue,
                        MachineName = group.MachineName,
                        Methord = group.Methord,
                        Unit = group.Unit,
                        NormalRange = group.NormalRange,
                        TestCode = group.TestCode,
                        Specimen = group.Specimen,
                        TestHistory = testHistory
                    };

                    groupedData.Add(TestsWithHistory);
                }

                var pascalCaseOptions = new JsonSerializerOptions
                {
                    PropertyNamingPolicy = null, // Use PascalCase (default C# naming convention)
                    WriteIndented = true
                };

                var serializedData = System.Text.Json.JsonSerializer.Serialize(groupedData, pascalCaseOptions);

                var Response = new
                {
                    isSucess = true,
                    message = "Success",
                    data = new
                    {
                        TestData = JsonDocument.Parse(serializedData),
                        PatientData = _patientData,
                    }
                };

                return Ok(Response);
            }
            catch (Exception ex)
            {
                _response.isSucess = false;
                _response.message = ex.Message;
                return StatusCode(500, _response);
            }
        }

        #endregion

        #region Receipt Aginst TestEntry Invoice 
        [HttpPost("postReceiptTestEntry")]
        public IActionResult postReceiptTestEntry([FromBody] ReceiptTestEntryRequest request)
        {
            try
            {
                string token = Request.Headers["Authorization"];
                token = token.Substring(7);

                var tokenClaims = Fluxion_Handler.GetJWTTokenClaims(token, _key._jwtKey, true);

                var parameters = new DynamicParameters();
                
                parameters.Add("@Flag", 104);
                parameters.Add("@ClientID", tokenClaims.ClientId);
                parameters.Add("@InvoiceNo", request.InvoiceNo);
                parameters.Add("@Sequence", request.Sequence);
                parameters.Add("@EditNo", request.EditNo);
                parameters.Add("@CashAmount", request.CashAmount);
                parameters.Add("@BankAmount", request.BankAmount);
                parameters.Add("@PaymentMode", request.PayMode);
                parameters.Add("@UserID", tokenClaims.UserId);
                parameters.Add("@DrAmount", request.DrAmount);
                parameters.Add("@CrAmount", request.CrAmount);
                parameters.Add("@TransType", request.TransType); 

                var data = _dbcontext.Query("SP_TestEntry", parameters, commandType: CommandType.StoredProcedure);

                _response.isSucess = true;
                _response.message = "Success";
                _response.data = data;

                return Ok(_response);

            }
            catch (Exception ex)
            {
                _response.isSucess = false;
                _response.message = ex.Message;
                return StatusCode(500, _response);
            }
        }
        #endregion

        #region  POST Result Entry Transaction
        [HttpPost("postTestEntryResult")]
        public async Task<IActionResult> postTestEntryResult([FromHeader] long InvoiceNo, [FromHeader] int Sequence, [FromHeader] int EditNo, [FromBody] ResultEntry _trn)
        {
            try
            {
                string token = Request.Headers["Authorization"];
                token = token.Substring(7);

                var tokenClaims = Fluxion_Handler.GetJWTTokenClaims(token, _key._jwtKey, true);

                var parameters = new DynamicParameters();
                parameters.Add("@Flag", 100);
                parameters.Add("@ClientID", tokenClaims.ClientId);
                parameters.Add("@InvoiceNo", InvoiceNo);
                parameters.Add("@Sequence", Sequence);
                parameters.Add("@EditNo", EditNo);
                parameters.Add("@JsonData", JsonConvert.SerializeObject(_trn));
                parameters.Add("@UserID", tokenClaims.UserId);

                var data = _dbcontext.Query("SP_TestResultEntry", parameters, commandType: CommandType.StoredProcedure);

                List<HierarchyEntry> resultData = new List<HierarchyEntry>();

                var result = await GetHirarachy(tokenClaims.ClientId.ToString(), Sequence, InvoiceNo, EditNo);
                resultData.AddRange(result);

                var parameters1 = new DynamicParameters();
                parameters1.Add("@Flag", 111);
                parameters1.Add("@ClientID", tokenClaims.ClientId);
                parameters1.Add("@InvoiceNo", InvoiceNo);
                parameters1.Add("@Sequence", Sequence);
                parameters1.Add("@EditNo", EditNo);
                parameters1.Add("@JsonData", JsonConvert.SerializeObject(resultData));

                var data1 = _dbcontext.Query("SP_TestEntry", parameters1, commandType: CommandType.StoredProcedure); 

                _response.isSucess = true;
                _response.message = "Success";
                _response.data = data;

                return Ok(_response);

            }
            catch (Exception ex)
            {
                _response.isSucess = false;
                _response.message = ex.Message;
                return StatusCode(500, _response);
            }
        }
        #endregion

        #region Discount Details By Card Number
        [HttpPost("getDiscountByCardNumber")]
        public IActionResult GetDiscountByCardNumber([FromHeader] long patientID, [FromHeader] long cardNumber, [FromBody] TestdIds _test)
        {
            try
            {
                string token = Request.Headers["Authorization"];
                token = token.Substring(7);

                var tokenClaims = Fluxion_Handler.GetJWTTokenClaims(token, _key._jwtKey, true);

                var parameters = new DynamicParameters();
                parameters.Add("@Flag", 107);
                parameters.Add("@ClientID", tokenClaims.ClientId);
                parameters.Add("@CardNumber", cardNumber);
                parameters.Add("@PatientID", patientID);
                parameters.Add("@JsonData", JsonConvert.SerializeObject(_test));

                var data = _dbcontext.Query("SP_TestEntry", parameters, commandType: CommandType.StoredProcedure);

                _response.isSucess = true;
                _response.message = "Success";
                _response.data = data;


                return Ok(_response);

            }
            catch (Exception ex)
            {
                _response.isSucess = false;
                _response.message = ex.Message;

                return StatusCode(500, _response);
            }
        }
        #endregion

        #region  TestEntry Invoice Cancel
        [HttpPost("cancelTestEtry")]
        public IActionResult CancelTestEtry([FromHeader] int _sequence, [FromHeader] long invoiceNo, [FromHeader] int editNo)
        {
            try
            {
                string token = Request.Headers["Authorization"];
                token = token.Substring(7);

                var tokenClaims = Fluxion_Handler.GetJWTTokenClaims(token, _key._jwtKey, true);

                var parameters = new DynamicParameters();
                parameters.Add("@ClientID", tokenClaims.ClientId);
                parameters.Add("@Sequence", _sequence);
                parameters.Add("@InvoiceNo", invoiceNo);
                parameters.Add("@EditNo", editNo);
                parameters.Add("@UserID", tokenClaims.UserId);

                var data = _dbcontext.Execute("SP_TestEntryCancel", parameters, commandType: CommandType.StoredProcedure);

                var Response = new
                {
                    isSucess = true,
                    message = "Invoice successfully canceled",
                };
                return Ok(Response);

            }
            catch (Exception ex)
            {
                _response.isSucess = false;
                _response.message = ex.Message;

                return StatusCode(500, _response);
            }
        }
        #endregion

        #region  Result Entry Verification
        [HttpPost("putResultEntryVerification")]
        public IActionResult PutResultEntryVerification([FromBody] ResultEntryParams _res)
        {
            try
            {
                string token = Request.Headers["Authorization"];
                token = token.Substring(7);
                var tokenClaims = Fluxion_Handler.GetJWTTokenClaims(token, _key._jwtKey, true);
                var parameters = new DynamicParameters();
                parameters.Add("@Flag", 108);
                parameters.Add("@ClientID", tokenClaims.ClientId);
                parameters.Add("@Sequence", _res._sequence);
                parameters.Add("@InvoiceNo", _res.invoiceNo);
                parameters.Add("@EditNo", _res.editNo);
                parameters.Add("@UserID", tokenClaims.UserId);

                var data = _dbcontext.Query("SP_TestEntry", parameters, commandType: CommandType.StoredProcedure);

                var Response = new
                {
                    isSucess = true,
                    message = "Success",
                    data = data
                };

                return Ok(Response);
            }
            catch (Exception ex)
            {
                _response.isSucess = false;
                _response.message = ex.Message;
                return StatusCode(500, _response);
            }
        }
        #endregion

        #region  Result Entry Approval
        [HttpPost("putResultEntryApproval")]
        public IActionResult PutResultEntryApproval([FromBody] ResultEntryParams _res)
        {
            try
            {
                string token = Request.Headers["Authorization"];
                token = token.Substring(7);
                var tokenClaims = Fluxion_Handler.GetJWTTokenClaims(token, _key._jwtKey, true);
                var parameters = new DynamicParameters();
                parameters.Add("@Flag", 109);
                parameters.Add("@ClientID", tokenClaims.ClientId);
                parameters.Add("@Sequence", _res._sequence);
                parameters.Add("@InvoiceNo", _res.invoiceNo);
                parameters.Add("@EditNo", _res.editNo);
                parameters.Add("@UserID", tokenClaims.UserId);

                var data = _dbcontext.Query("SP_TestEntry", parameters, commandType: CommandType.StoredProcedure);

                var Response = new
                {
                    isSucess = true,
                    message = "Success",
                    data = data
                };

                return Ok(Response);
            }
            catch (Exception ex)
            {
                _response.isSucess = false;
                _response.message = ex.Message;
                return StatusCode(500, _response);
            }
        }
        #endregion

        #region TestEntry Sample Collection Update  
        [HttpPost("putSampleCollectionUpdate")]
        public IActionResult putSampleCollectionUpdate([FromHeader] int _sequence, [FromHeader] long invoiceNo, [FromHeader] int editNo)
        {
            try
            {
                string token = Request.Headers["Authorization"];
                token = token.Substring(7);

                var tokenClaims = Fluxion_Handler.GetJWTTokenClaims(token, _key._jwtKey, true);

                var parameters = new DynamicParameters();
                parameters.Add("@Flag", 113);
                parameters.Add("@ClientID", tokenClaims.ClientId); 
                parameters.Add("@Sequence", _sequence);
                parameters.Add("@InvoiceNo", invoiceNo);
                parameters.Add("@EditNo", editNo);
                parameters.Add("@UserID", tokenClaims.UserId);

                var data = _dbcontext.Execute("SP_TestEntry", parameters, commandType: CommandType.StoredProcedure);

                var Response = new
                {
                    isSucess = true,
                    message = "Sucess",
                };
                return Ok(Response);

            }
            catch (Exception ex)
            {
                _response.isSucess = false;
                _response.message = ex.Message;

                return StatusCode(500, _response);
            }
        }
        #endregion

        #region Test Entry Report (Flag 118)
        [HttpGet("getTestEntryDetailsByDoctor")]
        public IActionResult GetTestEntryDetailsByDoctor([FromHeader] string FromDate, [FromHeader] string ToDate, [FromHeader] int? TestID, [FromHeader] int? DoctorID)
        {
            try
            {
                string token = Request.Headers["Authorization"];
                token = token.Substring(7);

                var tokenClaims = Fluxion_Handler.GetJWTTokenClaims(token, _key._jwtKey, true);

                var parameters = new DynamicParameters();
                parameters.Add("@Flag", 118);
                parameters.Add("@ClientID", tokenClaims.ClientId);
                parameters.Add("@FromDate", FromDate);
                parameters.Add("@ToDate", ToDate);
                parameters.Add("@TestID", TestID);
                parameters.Add("@DoctorID", DoctorID);

                var data = _dbcontext.Query("SP_TestEntry", parameters, commandType: CommandType.StoredProcedure);

                _response.isSucess = true;
                _response.message = "Success";
                _response.data = data;
                return Ok(_response);
            }
            catch (Exception ex)
            {
                _response.isSucess = false;
                _response.message = ex.Message;
                return StatusCode(500, _response);
            }
        }
        #endregion

        #endregion

        #region Payment to Supplier

        #region Get Supplier OverDue Bills
        [HttpGet("getSupplierOverDueBills")]
        public IActionResult getSupplierOverDueBills([FromHeader] long supplierID)
        {
            try
            {
                string token = Request.Headers["Authorization"];
                token = token.Substring(7);

                var tokenClaims = Fluxion_Handler.GetJWTTokenClaims(token, _key._jwtKey, true);

                var parameters = new DynamicParameters();
                parameters.Add("@Flag", 100);
                parameters.Add("@ClientID", tokenClaims.ClientId);
                parameters.Add("@SupplierID", supplierID);
                var data = _dbcontext.Query("SP_Payments", parameters, commandType: CommandType.StoredProcedure);



                _response.isSucess = true;
                _response.message = "Sucess";
                _response.data = data;

                return Ok(_response);
            }
            catch (Exception ex)
            {
                _response.isSucess = false;
                _response.message = ex.Message;

                return StatusCode(500, _response);
            }
        }
        #endregion

        #region Payment POST
        [HttpPost("postPaymentEntry")]
        public IActionResult PostTestEntryTransaction([FromBody] PaymentDetails _py)
        {
            try
            {
                string token = Request.Headers["Authorization"];
                token = token.Substring(7);

                var tokenClaims = Fluxion_Handler.GetJWTTokenClaims(token, _key._jwtKey, true);

                var parameters = new DynamicParameters();
                parameters.Add("@ClientID", tokenClaims.ClientId);
                parameters.Add("@SupplierID", _py.SupplierID);
                parameters.Add("@SupplierName", _py.SupplierName);
                parameters.Add("@PaymentDate", _py.PaymentDate);
                parameters.Add("@CashAmount", _py.CashAmount);
                parameters.Add("@BankAmount", _py.BankAmount);
                parameters.Add("@Remarks", _py.Remarks);
                parameters.Add("@CreatedBy", tokenClaims.UserId);
                parameters.Add("@JsonData", JsonConvert.SerializeObject(_py.InvoiceDetails));


                var data = _dbcontext.Query("SP_PaymentPOST", parameters, commandType: CommandType.StoredProcedure);

                _response.isSucess = true;
                _response.message = "Success";
                _response.data = data;

                return Ok(_response);

            }
            catch (Exception ex)
            {
                _response.isSucess = false;
                _response.message = ex.Message;
                return StatusCode(500, _response);
            }
        }
        #endregion

        #endregion

        #region TEST
 
        [NonAction]
        public async Task<List<HierarchyEntry>> GetGroupHierarchy(int groupId, string clientId, int sequence, long invoiceNo, int editNo)
        {
            try
            {
                string hierarchyQuery = @"
                WITH CTE_Hierarchy AS (
                    SELECT 
                        A.ClientID, A.GroupID, A.ChildGroupID, A.TestID, A.Type, A.LastModified,
                        CAST(A.GroupID AS NVARCHAR(MAX)) AS Path,CAST(A.UniqueID AS NVARCHAR(100)) UniqueID,
                        1 AS Level,A.SortOrder
                    FROM mtbl_TestGroupMappings A
                    WHERE A.GroupID = @GroupID
                    UNION ALL
                    SELECT 
                        mt.ClientID, mt.GroupID, mt.ChildGroupID, mt.TestID, mt.Type, mt.LastModified,
                        CAST(ch.Path + '->' + CAST(mt.GroupID AS NVARCHAR(MAX)) AS NVARCHAR(MAX)) AS Path,CAST(mt.UniqueID AS NVARCHAR(100)),
                        ch.Level + 1 AS Level,mt.SortOrder
                    FROM mtbl_TestGroupMappings mt
                    INNER JOIN CTE_Hierarchy ch ON mt.GroupID = ch.ChildGroupID
                )
                SELECT 
                    GroupID AS ID,
                    '' AS Name, 
                    ChildGroupID AS ChildID,
                    A.TestID AS TestID,
                    Type,SortOrder,UniqueID
                FROM 
                    CTE_Hierarchy A
                    
                Order by
                    SortOrder";


                var flatHierarchy = await _dbcontext.QueryAsync<HierarchyEntry>(hierarchyQuery, new
                {
                    GroupID = groupId
                });

                var sortedHierarchy = flatHierarchy
                                    .OrderBy(x => x.SortOrder)
                                    .ToList();

                return BuildHierarchy(sortedHierarchy, groupId, clientId, sequence, invoiceNo, editNo);
            }
            catch (Exception ex)
            {

                throw ex;
            }
        }

        private List<HierarchyEntry> BuildHierarchy(IEnumerable<HierarchyEntry> flatData, int groupId, string clientId, int sequence, long invoiceNo, int editNo)
        {
            try
            {
                // Group the data by parent-child relationships
                var groupDictionary = flatData.ToLookup(x => x.ID);

                // Recursive function to build the hierarchy
                List<HierarchyEntry> BuildHierarchyRecursive(int groupId)
                {
                    var children = new List<HierarchyEntry>();

                    foreach (var x in groupDictionary[groupId])
                    {
                        HierarchyEntry entry;

                        if (x.Type == "G")
                        {
                            string uniqueID = x.UniqueID;

                            // Check if the UniqueID exists in the database
                            string existingUniqueID = _dbcontext.QueryFirstOrDefault<string>(
                                @"SELECT [UniqueID] 
                              FROM [dbo].[trntbl_TestEntriesResults] B 
                              WHERE 
                                  B.ClientID = @ClientID 
                                  AND B.Sequence = @Sequence 
                                  AND B.InvoiceNo = @InvoiceNo 
                                  AND B.EditNo = @EditNo AND B.TestID = @ID
                                  AND B.[UniqueID] = @UniqueID",
                                                    new
                                                    {
                                                        ClientID = clientId,
                                                        Sequence = sequence,
                                                        InvoiceNo = invoiceNo,
                                                        EditNo = editNo,
                                                        UniqueID = uniqueID,
                                                        ID = x.ChildID ?? x.ID
                                                    });

                            // Query based on whether UniqueID exists
                            string groupQuery;

                            if (existingUniqueID != null)
                            {
                                // If the UniqueID exists, use it directly
                                groupQuery = @"
                            SELECT 
                                G.GroupID AS ID,
                                CASE WHEN B.[Values] IS NULL THEN G.GroupName ELSE B.TestName END AS Name,
                                'Group' AS LabelType,
                                ISNULL(B.[Values], '') AS [Value],
                                ISNULL(B.[Action], '') AS [Action],
                                ISNULL(B.Comments, '') AS Comments,
                                ISNULL(G.Section, '') AS TestSection,
                                0.00 AS HighValue,
                                0.00 AS LowValue,
                                ISNULL(G.MachineName, '') AS MachineName,
                                '' AS Methord,
                                '' AS Unit,
                                '' AS NormalRange,
                                ISNULL(GroupCode, '') AS TestCode,
                                '' AS Specimen,
                                GM.SortOrder AS SortOrder,
                                @uniqueID AS [UniqueID],
                                'L' AS [TransType]
                            FROM 
                                [dbo].[mtbl_TestGroupMaster] G
                            LEFT JOIN 
                                [dbo].[trntbl_TestEntriesResults] B  ON B.ClientID = @ClientID  AND B.Sequence = @Sequence AND B.InvoiceNo = @InvoiceNo AND B.EditNo = @EditNo AND G.GroupID = B.TestID
                                AND B.TransType = 'L'
                            LEFT JOIN
                                mtbl_TestGroupMappings GM   ON GM.ClientID = G.ClientID AND G.GroupID = GM.GroupID  AND GM.Type = 'G'
                            WHERE 
                                G.GroupID = @ID";
                            }
                            else
                            {
                                // If the UniqueID does not exist, pass the ID directly
                                groupQuery = @"
                                SELECT 
                                    G.GroupID AS ID,
                                    CASE WHEN B.[Values] IS NULL THEN G.GroupName ELSE B.TestName END AS Name,
                                    'Group' AS LabelType,
                                    ISNULL(B.[Values], '') AS [Value],
                                    ISNULL(B.[Action], '') AS [Action],
                                    ISNULL(B.Comments, '') AS Comments,
                                    ISNULL(G.Section, '') AS TestSection,
                                    0.00 AS HighValue,
                                    0.00 AS LowValue,
                                    ISNULL(G.MachineName, '') AS MachineName,
                                    '' AS Methord,
                                    '' AS Unit,
                                    '' AS NormalRange,
                                    ISNULL(GroupCode, '') AS TestCode,
                                    '' AS Specimen,
                                    GM.SortOrder AS SortOrder,
                                    @uniqueID AS [UniqueID], 
                                    'L' AS [TransType]
                                FROM 
                                    [dbo].[mtbl_TestGroupMaster] G
                                LEFT JOIN 
                                    [dbo].[trntbl_TestEntriesResults] B 
                                    ON B.ClientID = @ClientID 
                                    AND B.Sequence = @Sequence
                                    AND B.InvoiceNo = @InvoiceNo
                                    AND B.EditNo = @EditNo
                                    AND G.GroupID = B.TestID
                                    AND B.TransType = 'L'
                                LEFT JOIN
                                    mtbl_TestGroupMappings GM 
                                    ON GM.ClientID = G.ClientID 
                                    AND G.GroupID = GM.GroupID 
                                    AND GM.Type = 'G'
                                WHERE 
                                    G.GroupID = @ID";
                            }

                            // Execute the query
                            entry = _dbcontext.QueryFirstOrDefault<HierarchyEntry>(groupQuery, new
                            {
                                ClientID = clientId,
                                Sequence = sequence,
                                InvoiceNo = invoiceNo,
                                EditNo = editNo,
                                uniqueID = uniqueID,
                                ID = x.ChildID ?? x.ID
                            });

                            foreach (var sub in entry.SubArray)
                            {
                                sub.SortOrder = x.SortOrder; // Assuming SubArray elements have SortOrder property
                            }

                            // Recursively build the SubArray
                            entry.SubArray = BuildHierarchyRecursive(x.ChildID ?? 0);
                        }
                        else if (x.Type == "T")
                        {
                            string uniqueID = x.UniqueID;

                            string groupQuery;

                            // Check if the UniqueID exists in the database
                            string existingUniqueID = _dbcontext.QueryFirstOrDefault<string>(
                                @"SELECT [UniqueID] 
                              FROM [dbo].[trntbl_TestEntriesResults] B 
                              WHERE 
                                  B.ClientID = @ClientID 
                                  AND B.Sequence = @Sequence 
                                  AND B.InvoiceNo = @InvoiceNo 
                                  AND B.EditNo = @EditNo AND B.TestID = @ID 
                                  AND B.[UniqueID] = @UniqueID",
                                                    new
                                                    {
                                                        ClientID = clientId,
                                                        Sequence = sequence,
                                                        InvoiceNo = invoiceNo,
                                                        EditNo = editNo,
                                                        UniqueID = uniqueID,
                                                        ID = x.ChildID ?? x.TestID
                                                    });

                            if (existingUniqueID != null)
                            {
                                // Query to fetch the test details
                                groupQuery = @"
                                SELECT 
                                    T.TestID AS ID, 
                                    CASE WHEN B.[Values] IS NULL THEN T.TestName ELSE B.TestName END AS Name,
                                    'Item' AS LabelType, 
                                    ISNULL(B.[Values], '') AS [Value], 
                                    ISNULL(B.[Action], '') AS [Action], 
                                    ISNULL(B.Comments, '') AS Comments,
                                    ISNULL(T.Section, '') AS TestSection, 
                                    T.HighValue AS HighValue, 
                                    T.LowValue AS LowValue, 
                                    ISNULL(T.MachineName, '') AS MachineName,
                                    T.Methord AS Methord, 
                                    T.Unit AS Unit, 
                                    T.NormalRange AS NormalRange, 
                                    ISNULL(T.TestCode, '') AS TestCode, 
                                    T.Specimen AS Specimen,
                                    GM.SortOrder AS SortOrder,
                                    @uniqueID AS [UniqueID],
                                    'L' AS [TransType]
                                FROM 
                                    [dbo].[mtbl_TestMaster] T  
                                LEFT JOIN [dbo].[trntbl_TestEntriesResults] B ON B.ClientID = @ClientID AND B.Sequence = @Sequence AND B.InvoiceNo = @InvoiceNo AND B.EditNo = @EditNo AND T.TestID = B.TestID 
                                    AND B.UniqueID = @uniqueID
                                LEFT JOIN 
                                    mtbl_TestGroupMappings GM 
                                    ON GM.ClientID = T.ClientID 
                                    AND GM.TestID = T.TestID 
                                    AND GM.Type = 'T'
                                WHERE 
                                    T.TestID = @ID";
                            }
                            else
                            {
                                groupQuery = @"
                                SELECT 
                                    T.TestID AS ID, 
                                    CASE WHEN B.[Values] IS NULL THEN T.TestName ELSE B.TestName END AS Name,
                                    'Item' AS LabelType, 
                                    ISNULL(B.[Values], '') AS [Value], 
                                    ISNULL(B.[Action], '') AS [Action], 
                                    ISNULL(B.Comments, '') AS Comments,
                                    ISNULL(T.Section, '') AS TestSection, 
                                    T.HighValue AS HighValue, 
                                    T.LowValue AS LowValue, 
                                    ISNULL(T.MachineName, '') AS MachineName,
                                    T.Methord AS Methord, 
                                    T.Unit AS Unit, 
                                    T.NormalRange AS NormalRange, 
                                    ISNULL(T.TestCode, '') AS TestCode, 
                                    T.Specimen AS Specimen,
                                    GM.SortOrder AS SortOrder,
                                    @uniqueID AS [UniqueID],  
                                    'L' AS [TransType]
                                FROM 
                                    [dbo].[mtbl_TestMaster] T  
                                LEFT JOIN [dbo].[trntbl_TestEntriesResults] B ON B.ClientID = @ClientID AND B.Sequence = @Sequence AND B.InvoiceNo = @InvoiceNo AND B.EditNo = @EditNo AND T.TestID = B.TestID 
                                    AND B.UniqueID = @uniqueID
                                LEFT JOIN 
                                    mtbl_TestGroupMappings GM 
                                    ON GM.ClientID = T.ClientID 
                                    AND GM.TestID = T.TestID 
                                    AND GM.Type = 'T'
                                WHERE 
                                    T.TestID = @ID";
                            }


                            // Execute the query
                            entry = _dbcontext.QueryFirstOrDefault<HierarchyEntry>(groupQuery, new
                            {
                                ClientID = clientId,
                                Sequence = sequence,
                                InvoiceNo = invoiceNo,
                                EditNo = editNo,
                                uniqueID = uniqueID, // Pass the final UniqueID
                                ID = x.ChildID ?? x.TestID      // Pass the TestID
                            });

                            // Update SortOrder for SubArray if applicable
                            if (entry != null && entry.SubArray != null)
                            {
                                foreach (var sub in entry.SubArray)
                                {
                                    sub.SortOrder = x.SortOrder; // Assuming SubArray elements have SortOrder property
                                }
                            }

                            // Ensure SubArray is initialized as an empty list if null
                            entry.SubArray ??= new List<HierarchyEntry>();
                        }

                        else
                        {
                            // Fallback entry
                            entry = new HierarchyEntry
                            {
                                ID = x.ID,
                                Type = x.Type,
                                SortOrder = x.SortOrder,
                                SubArray = new List<HierarchyEntry>()
                            };
                        }

                        children.Add(entry);
                    }

                    return children;

                }

                // Start building from the specified group ID
                return BuildHierarchyRecursive(groupId);
            }
            catch (Exception ex)
            {

                throw ex;
            }
        }

        private async Task<List<HierarchyEntry>> GetHirarachy(string clientID, int sequence, long invoiceNo, int editNo)
        {

            string baseQuery = @"
                SELECT 
                     ID,Name,Type
                    ,CASE WHEN A.Type = 'G' THEN 'Group' ELSE 'Test' END AS [LabelName]
                    ,CASE WHEN A.Type = 'G' THEN '' ELSE ISNULL(B.[Values],'') END AS [Value]
                    ,ISNULL(B.[Action],'') AS [Action]
                    ,CASE WHEN A.Type = 'G' THEN '' ELSE ISNULL(B.Comments,'') END AS [Comments] 
                    ,CASE WHEN A.Type = 'G' THEN ISNULL(G.Section,'') ELSE ISNULL(T.Section,'') END AS [TestSection]
                    ,CASE WHEN A.Type = 'G' THEN 0.00 ELSE ISNULL(T.HighValue,0.00) END AS HighValue 
                    ,CASE WHEN A.Type = 'G' THEN 0.00 ELSE ISNULL(T.LowValue,0.00) END AS LowValue 
                    ,CASE WHEN A.Type = 'G' THEN ISNULL(G.MachineName,'') ELSE ISNULL(T.MachineName,'') END AS MachineName 
                    ,CASE WHEN A.Type = 'G' THEN '' ELSE ISNULL(T.Methord,'') END AS Method 
                    ,CASE WHEN A.Type = 'G' THEN '' ELSE ISNULL(T.Unit,'') END AS Unit
                    ,CASE WHEN A.Type = 'G' THEN '' ELSE ISNULL(T.NormalRange,'') END AS NormalRange
                    ,CASE WHEN A.Type = 'G' THEN ISNULL(GroupCode,'') ELSE ISNULL(T.TestCode,'') END AS TestCode 
                    ,CASE WHEN A.Type = 'G' THEN '' ELSE ISNULL(T.Specimen,'') END AS Specimen  
                    ,ISNULL(A.[TransactionUniqueID],'') AS [UniqueID]
                    ,'H' as [TransType]
                FROM 
                    [dbo].[trntbl_TestEntriesLine] A
                    LEFT JOIN [dbo].[trntbl_TestEntriesResults] B ON A.ClientID = B.ClientID and A.Sequence = B.Sequence and A.InvoiceNo = B.InvoiceNo and A.EditNo = B.EditNo and A.ID = B.TestID --and A.[TransactionUniqueID] = B.UniqueID
                    LEFT JOIN [dbo].[mtbl_TestMaster] T ON T.ClientID = A.ClientID and T.TestID = A.ID and A.Type = 'T'
                    LEFT JOIN [dbo].[mtbl_TestGroupMaster] G ON G.ClientID = A.ClientID and G.GroupID = A.ID and A.Type = 'G'                    
                WHERE
                    A.ClientID = @ClientID AND A.[Sequence] = @Sequence AND A.InvoiceNo = @InvoiceNo AND A.EditNo = @EditNo ORDER BY A.SI_No";

            var baseResults = _dbcontext.Query<BaseEntry>(baseQuery, new
            {
                ClientID = clientID,
                Sequence = sequence,
                InvoiceNo = invoiceNo,
                EditNo = editNo
            });

            // Process each entry and fetch hierarchy for groups
            var result = new List<HierarchyEntry>();

            foreach (var entry in baseResults)
            {
                var hierarchyEntry = new HierarchyEntry
                {
                    ID = entry.ID,
                    Name = entry.Name,
                    Type = entry.Type,
                    LabelType = entry.LabelName,
                    Value = entry.Value,
                    Action = entry.Action,
                    Comments = entry.Comments,
                    TestSection = entry.TestSection,
                    HighValue = entry.HighValue,
                    LowValue = entry.LowValue,
                    MachineName = entry.MachineName,
                    Method = entry.Method,
                    Unit = entry.Unit,
                    NormalRange = entry.NormalRange,
                    TestCode = entry.TestCode,
                    Specimen = entry.Specimen,
                    UniqueID = entry.UniqueID,
                    TransType = entry.TransType,
                    SubArray = entry.Type == "G" ? await GetGroupHierarchyUniqueID(entry.ID, clientID, sequence, invoiceNo, editNo) : new List<HierarchyEntry>()
                };

                result.Add(hierarchyEntry);
            }

            return result;
        }

        [NonAction]
        public async Task<List<HierarchyEntry>> GetGroupHierarchyUniqueID(int groupId, string clientId, int sequence, long invoiceNo, int editNo)
        {
            try
            {
                string hierarchyQuery = @"
                WITH CTE_Hierarchy AS (
                    SELECT 
                        A.ClientID, A.GroupID, A.ChildGroupID, A.TestID, A.Type, A.LastModified,
                        CAST(A.GroupID AS NVARCHAR(MAX)) AS Path,CAST(A.UniqueID AS NVARCHAR(100)) UniqueID,
                        1 AS Level,A.SortOrder
                    FROM mtbl_TestGroupMappings A
                    WHERE A.GroupID = @GroupID
                    UNION ALL
                    SELECT 
                        mt.ClientID, mt.GroupID, mt.ChildGroupID, mt.TestID, mt.Type, mt.LastModified,
                        CAST(ch.Path + '->' + CAST(mt.GroupID AS NVARCHAR(MAX)) AS NVARCHAR(MAX)) AS Path,CAST(mt.UniqueID AS NVARCHAR(100)),
                        ch.Level + 1 AS Level,mt.SortOrder
                    FROM mtbl_TestGroupMappings mt
                    INNER JOIN CTE_Hierarchy ch ON mt.GroupID = ch.ChildGroupID
                )
                SELECT 
                    GroupID AS ID,
                    '' AS Name, 
                    ChildGroupID AS ChildID,
                    A.TestID AS TestID,
                    Type,SortOrder,UniqueID
                FROM 
                    CTE_Hierarchy A
                    
                Order by
                    SortOrder";


                var flatHierarchy = await _dbcontext.QueryAsync<HierarchyEntry>(hierarchyQuery, new
                {
                    GroupID = groupId
                });

                var sortedHierarchy = flatHierarchy
                                    .OrderBy(x => x.SortOrder)
                                    .ToList();

                return BuildHierarchyUniqueID(sortedHierarchy, groupId, clientId, sequence, invoiceNo, editNo);
            }
            catch (Exception ex)
            {

                throw ex;
            }
        }

        private List<HierarchyEntry> BuildHierarchyUniqueID(IEnumerable<HierarchyEntry> flatData, int groupId, string clientId, int sequence, long invoiceNo, int editNo)
        {
            try
            {
                // Group the data by parent-child relationships
                var groupDictionary = flatData.ToLookup(x => x.ID);

                // Recursive function to build the hierarchy
                List<HierarchyEntry> BuildHierarchyRecursiveUniqueID(int groupId)
                {
                    var children = new List<HierarchyEntry>();

                    foreach (var x in groupDictionary[groupId])
                    {
                        HierarchyEntry entry;

                        if (x.Type == "G")
                        {
                            string uniqueID = x.UniqueID;

                            // Check if the UniqueID exists in the database
                            string existingUniqueID = _dbcontext.QueryFirstOrDefault<string>(
                                @"SELECT [UniqueID] 
                              FROM [dbo].[trntbl_TestEntriesResults] B 
                              WHERE 
                                  B.ClientID = @ClientID 
                                  AND B.Sequence = @Sequence 
                                  AND B.InvoiceNo = @InvoiceNo 
                                  AND B.EditNo = @EditNo AND B.TestID = @ID
                                  AND B.[UniqueID] = @UniqueID",
                                                    new
                                                    {
                                                        ClientID = clientId,
                                                        Sequence = sequence,
                                                        InvoiceNo = invoiceNo,
                                                        EditNo = editNo,
                                                        UniqueID = uniqueID,
                                                        ID = x.ChildID ?? x.ID
                                                    });

                            // Query based on whether UniqueID exists
                            string groupQuery;

                            if (existingUniqueID != null)
                            {
                                // If the UniqueID exists, use it directly
                                groupQuery = @"
                            SELECT 
                                G.GroupID AS ID,
                                CASE WHEN B.[Values] IS NULL THEN G.GroupName ELSE B.TestName END AS Name,
                                'Group' AS LabelType,
                                ISNULL(B.[Values], '') AS [Value],
                                ISNULL(B.[Action], '') AS [Action],
                                ISNULL(B.Comments, '') AS Comments,
                                ISNULL(G.Section, '') AS TestSection,
                                0.00 AS HighValue,
                                0.00 AS LowValue,
                                ISNULL(G.MachineName, '') AS MachineName,
                                '' AS Methord,
                                '' AS Unit,
                                '' AS NormalRange,
                                ISNULL(GroupCode, '') AS TestCode,
                                '' AS Specimen,
                                GM.SortOrder AS SortOrder,
                                @uniqueID AS [UniqueID],
                                'L' AS [TransType]
                            FROM 
                                [dbo].[mtbl_TestGroupMaster] G
                            LEFT JOIN 
                                [dbo].[trntbl_TestEntriesResults] B  ON B.ClientID = @ClientID  AND B.Sequence = @Sequence AND B.InvoiceNo = @InvoiceNo AND B.EditNo = @EditNo AND G.GroupID = B.TestID
                                --AND B.TransType = 'L'
                            LEFT JOIN
                                mtbl_TestGroupMappings GM   ON GM.ClientID = G.ClientID AND G.GroupID = GM.GroupID  AND GM.Type = 'G'
                            WHERE 
                                G.GroupID = @ID";
                            }
                            else
                            {
                                // If the UniqueID does not exist, pass the ID directly
                                groupQuery = @"
                                SELECT 
                                    G.GroupID AS ID,
                                    CASE WHEN B.[Values] IS NULL THEN G.GroupName ELSE B.TestName END AS Name,
                                    'Group' AS LabelType,
                                    ISNULL(B.[Values], '') AS [Value],
                                    ISNULL(B.[Action], '') AS [Action],
                                    ISNULL(B.Comments, '') AS Comments,
                                    ISNULL(G.Section, '') AS TestSection,
                                    0.00 AS HighValue,
                                    0.00 AS LowValue,
                                    ISNULL(G.MachineName, '') AS MachineName,
                                    '' AS Methord,
                                    '' AS Unit,
                                    '' AS NormalRange,
                                    ISNULL(GroupCode, '') AS TestCode,
                                    '' AS Specimen,
                                    GM.SortOrder AS SortOrder,
                                    @uniqueID AS [UniqueID], 
                                    'L' AS [TransType]
                                FROM 
                                    [dbo].[mtbl_TestGroupMaster] G
                                LEFT JOIN 
                                    [dbo].[trntbl_TestEntriesResults] B 
                                    ON B.ClientID = @ClientID 
                                    AND B.Sequence = @Sequence
                                    AND B.InvoiceNo = @InvoiceNo
                                    AND B.EditNo = @EditNo
                                    AND G.GroupID = B.TestID
                                    --AND B.TransType = 'L'
                                LEFT JOIN
                                    mtbl_TestGroupMappings GM 
                                    ON GM.ClientID = G.ClientID 
                                    AND G.GroupID = GM.GroupID 
                                    AND GM.Type = 'G'
                                WHERE 
                                    G.GroupID = @ID";
                            }

                            // Execute the query
                            entry = _dbcontext.QueryFirstOrDefault<HierarchyEntry>(groupQuery, new
                            {
                                ClientID = clientId,
                                Sequence = sequence,
                                InvoiceNo = invoiceNo,
                                EditNo = editNo,
                                uniqueID = uniqueID,
                                ID = x.ChildID ?? x.ID
                            });

                            foreach (var sub in entry.SubArray)
                            {
                                sub.SortOrder = x.SortOrder; // Assuming SubArray elements have SortOrder property
                            }

                            // Recursively build the SubArray
                            entry.SubArray = BuildHierarchyRecursiveUniqueID(x.ChildID ?? 0);
                        }
                        else if (x.Type == "T")
                        {
                            string uniqueID = x.UniqueID;
                            string groupQuery;

                            // Check if the UniqueID exists in the database
                            string existingUniqueID = _dbcontext.QueryFirstOrDefault<string>(
                                @"SELECT [UniqueID] 
                              FROM [dbo].[trntbl_TestEntriesResults] B 
                              WHERE 
                                  B.ClientID = @ClientID 
                                  AND B.Sequence = @Sequence 
                                  AND B.InvoiceNo = @InvoiceNo 
                                  AND B.EditNo = @EditNo AND B.TestID = @ID 
                                  AND B.[UniqueID] = @UniqueID",
                                                    new
                                                    {
                                                        ClientID = clientId,
                                                        Sequence = sequence,
                                                        InvoiceNo = invoiceNo,
                                                        EditNo = editNo,
                                                        UniqueID = uniqueID,
                                                        ID = x.ChildID ?? x.TestID
                                                    });

                            if (existingUniqueID != null)
                            {
                                // Query to fetch the test details
                                groupQuery = @"
                                SELECT 
                                    T.TestID AS ID, 
                                    CASE WHEN B.[Values] IS NULL THEN T.TestName ELSE B.TestName END AS Name,
                                    'Item' AS LabelType, 
                                    ISNULL(B.[Values], '') AS [Value], 
                                    ISNULL(B.[Action], '') AS [Action], 
                                    ISNULL(B.Comments, '') AS Comments,
                                    ISNULL(T.Section, '') AS TestSection, 
                                    T.HighValue AS HighValue, 
                                    T.LowValue AS LowValue, 
                                    ISNULL(T.MachineName, '') AS MachineName,
                                    T.Methord AS Methord, 
                                    T.Unit AS Unit, 
                                    T.NormalRange AS NormalRange, 
                                    ISNULL(T.TestCode, '') AS TestCode, 
                                    T.Specimen AS Specimen,
                                    GM.SortOrder AS SortOrder,
                                    @uniqueID AS [UniqueID],
                                    'L' AS [TransType]
                                FROM 
                                    [dbo].[mtbl_TestMaster] T  
                                LEFT JOIN [dbo].[trntbl_TestEntriesResults] B ON B.ClientID = @ClientID AND B.Sequence = @Sequence AND B.InvoiceNo = @InvoiceNo AND B.EditNo = @EditNo AND T.TestID = B.TestID 
                                    --AND B.UniqueID = @uniqueID
                                LEFT JOIN 
                                    mtbl_TestGroupMappings GM 
                                    ON GM.ClientID = T.ClientID 
                                    AND GM.TestID = T.TestID 
                                    AND GM.Type = 'T'
                                WHERE 
                                    T.TestID = @ID";
                            }
                            else
                            {
                                groupQuery = @"
                                SELECT 
                                    T.TestID AS ID, 
                                    CASE WHEN B.[Values] IS NULL THEN T.TestName ELSE B.TestName END AS Name,
                                    'Item' AS LabelType, 
                                    ISNULL(B.[Values], '') AS [Value], 
                                    ISNULL(B.[Action], '') AS [Action], 
                                    ISNULL(B.Comments, '') AS Comments,
                                    ISNULL(T.Section, '') AS TestSection, 
                                    T.HighValue AS HighValue, 
                                    T.LowValue AS LowValue, 
                                    ISNULL(T.MachineName, '') AS MachineName,
                                    T.Methord AS Methord, 
                                    T.Unit AS Unit, 
                                    T.NormalRange AS NormalRange, 
                                    ISNULL(T.TestCode, '') AS TestCode, 
                                    T.Specimen AS Specimen,
                                    GM.SortOrder AS SortOrder,
                                    @uniqueID AS [UniqueID],  
                                    'L' AS [TransType]
                                FROM 
                                    [dbo].[mtbl_TestMaster] T  
                                LEFT JOIN [dbo].[trntbl_TestEntriesResults] B ON B.ClientID = @ClientID AND B.Sequence = @Sequence AND B.InvoiceNo = @InvoiceNo AND B.EditNo = @EditNo AND T.TestID = B.TestID 
                                    --AND B.UniqueID = @uniqueID
                                LEFT JOIN 
                                    mtbl_TestGroupMappings GM 
                                    ON GM.ClientID = T.ClientID 
                                    AND GM.TestID = T.TestID 
                                    AND GM.Type = 'T'
                                WHERE 
                                    T.TestID = @ID";
                            }


                            // Execute the query
                            entry = _dbcontext.QueryFirstOrDefault<HierarchyEntry>(groupQuery, new
                            {
                                ClientID = clientId,
                                Sequence = sequence,
                                InvoiceNo = invoiceNo,
                                EditNo = editNo,
                                uniqueID = uniqueID, // Pass the final UniqueID
                                ID = x.ChildID ?? x.TestID      // Pass the TestID
                            });

                            // Update SortOrder for SubArray if applicable
                            if (entry != null && entry.SubArray != null)
                            {
                                foreach (var sub in entry.SubArray)
                                {
                                    sub.SortOrder = x.SortOrder; // Assuming SubArray elements have SortOrder property
                                }
                            }

                            // Ensure SubArray is initialized as an empty list if null
                            entry.SubArray ??= new List<HierarchyEntry>();
                        }

                        else
                        {
                            // Fallback entry
                            entry = new HierarchyEntry
                            {
                                ID = x.ID,
                                Type = x.Type,
                                SortOrder = x.SortOrder,
                                SubArray = new List<HierarchyEntry>()
                            };
                        }

                        children.Add(entry);
                    }

                    return children;

                }

                // Start building from the specified group ID
                return BuildHierarchyRecursiveUniqueID(groupId);
            }
            catch (Exception ex)
            {

                throw ex;
            }
        }

        public class BaseEntry
        {
            public int ID { get; set; }
            public string Name { get; set; }
            public string Type { get; set; }
            public string LabelName { get; set; }
            public string Value { get; set; }
            public string Action { get; set; }
            public string Comments { get; set; }
            public string TestSection { get; set; }
            public decimal HighValue { get; set; }
            public decimal LowValue { get; set; }
            public string MachineName { get; set; }
            public string Method { get; set; }
            public string Unit { get; set; }
            public string NormalRange { get; set; }
            public string TestCode { get; set; }
            public string Specimen { get; set; }
            public string UniqueID { get; set; }
            public string TransType { get; set; }
        }

        public class HierarchyEntry
        {
            public int ID { get; set; }
            public string Name { get; set; }
            public int TestID { get; set; }
            public string Type { get; set; }
            public int? ChildID { get; set; }
            public string LabelType { get; set; }
            public string Value { get; set; }
            public string Action { get; set; }
            public string Comments { get; set; }
            public string TestSection { get; set; }
            public decimal HighValue { get; set; }
            public decimal LowValue { get; set; }
            public string MachineName { get; set; }
            public string Method { get; set; }
            public string Unit { get; set; }
            public string NormalRange { get; set; }
            public string TestCode { get; set; }
            public string Specimen { get; set; }
            public int? SortOrder { get; set; }
            public string UniqueID { get; set; }
            public string TransType { get; set; }

            public List<HierarchyEntry> SubArray { get; set; } = new List<HierarchyEntry>();

        }


        #endregion

        #region Get Result Entry
        [HttpGet("getResultEntry")]
        public async Task<IActionResult> GetHierarchy([FromHeader] int sequence, [FromHeader] long invoiceNo, [FromHeader] int editNo)
        {
            try
            {
                string token = Request.Headers["Authorization"];
                token = token.Substring(7);

                var tokenClaims = Fluxion_Handler.GetJWTTokenClaims(token, _key._jwtKey, true);
                string clientID = tokenClaims.ClientId;

                // Query to get the base data
                string baseQuery = @"
                    SELECT 
                         ID,Name,Type
	                    ,CASE WHEN A.Type = 'G' THEN 'Group' ELSE 'Test' END AS [LabelName]
	                    ,CASE WHEN A.Type = 'G' THEN '' ELSE ISNULL(B.[Values],'') END AS [Value]
	                    ,ISNULL(B.[Action],'') AS [Action]
	            	    ,CASE WHEN A.Type = 'G' THEN '' ELSE ISNULL(B.Comments,'') END AS [Comments] 
	                    ,CASE WHEN A.Type = 'G' THEN ISNULL(G.Section,'') ELSE ISNULL(T.Section,'') END AS [TestSection]
	                    ,CASE WHEN A.Type = 'G' THEN 0.00 ELSE ISNULL(T.HighValue,0.00) END AS HighValue 
	                    ,CASE WHEN A.Type = 'G' THEN 0.00 ELSE ISNULL(T.LowValue,0.00) END AS LowValue 
	                    ,CASE WHEN A.Type = 'G' THEN ISNULL(G.MachineName,'') ELSE ISNULL(T.MachineName,'') END AS MachineName 
	                    ,CASE WHEN A.Type = 'G' THEN '' ELSE ISNULL(T.Methord,'') END AS Method 
	                    ,CASE WHEN A.Type = 'G' THEN '' ELSE ISNULL(T.Unit,'') END AS Unit
	                    ,CASE WHEN A.Type = 'G' THEN '' ELSE ISNULL(T.NormalRange,'') END AS NormalRange
	                    ,CASE WHEN A.Type = 'G' THEN ISNULL(GroupCode,'') ELSE ISNULL(T.TestCode,'') END AS TestCode 
	                    ,CASE WHEN A.Type = 'G' THEN '' ELSE ISNULL(T.Specimen,'') END AS Specimen  
                        ,ISNULL(A.[TransactionUniqueID],'') AS [UniqueID]
                        ,'H' as [TransType],B.IsSampleOutSourced as IsOutSourced
                    FROM 
                        [dbo].[trntbl_TestEntriesLine] A
                        LEFT JOIN [dbo].[trntbl_TestEntriesResults] B ON A.ClientID = B.ClientID and A.Sequence = B.Sequence and A.InvoiceNo = B.InvoiceNo and A.EditNo = B.EditNo and A.ID = B.TestID and A.[TransactionUniqueID] = B.UniqueID
                        LEFT JOIN [dbo].[mtbl_TestMaster] T ON T.ClientID = A.ClientID and T.TestID = A.ID and A.Type = 'T'
                        LEFT JOIN [dbo].[mtbl_TestGroupMaster] G ON G.ClientID = A.ClientID and G.GroupID = A.ID and A.Type = 'G'                    
                    WHERE
                        A.ClientID = @ClientID AND A.[Sequence] = @Sequence AND A.InvoiceNo = @InvoiceNo AND A.EditNo = @EditNo ORDER BY A.SI_No";

                string patientQuery = @" 
                    -- Patient Summary
                    DECLARE
                        @IsVerified BIT,@VerifiedBy VARCHAR(100),@IsApproved BIT,@ApprovedBy VARCHAR(100),@Approved_User_Signature VARCHAR(1000)

                    SELECT TOP(1)
                        @IsVerified = A.IsVerified,@VerifiedBy = B.UserName,@IsApproved = A.IsApproved,@ApprovedBy = C.UserName,@Approved_User_Signature = C.SinatureUrl
                    FROM
                        [dbo].[trntbl_TestEntriesResults] A
                        LEFT JOIN [dbo].[mtbl_UserMaster] B ON B.ClientID = A.ClientID and B.UserID = A.IsVerifiedBy
                        LEFT JOIN [dbo].[mtbl_UserMaster] C ON C.ClientID = A.ClientID and C.UserID = A.IsApprovedBy 
                    WHERE
                        A.ClientID = @ClientID and A.Sequence = @Sequence and A.InvoiceNo = @InvoiceNo and A.EditNo = @EditNo  

                    SELECT
                        B.PatientID,B.PatientName,B.MobileNo,B.Gender,B.Age,B.DOB,B.Place,A.Ref_DoctorName as DoctorName,Ref_Lab as LabName,FORMAT(A.Created_at,'dd/MM/yyyy hh:mm tt') as CreatedDate,
                        @IsVerified IsVerified,@VerifiedBy as VerifiedBy,@IsApproved as IsApproved
                        ,@ApprovedBy as ApprovedBy,@Approved_User_Signature AS Approved_User_Signature,A.PaymentStatus ,B.Month,B.Days
                        ,ISNULL(A.NoOfResultPrint,0) as NoOfResultPrint
                    FROM
                        dbo.trntbl_TestEntriesHdr A
                        JOIN dbo.mtbl_PatientMaster B on A.ClientID = B.ClientID and A.PatientID = B.PatientID
 	                    Where
		                A.ClientID = @ClientID and A.Sequence = @Sequence and A.InvoiceNo = @InvoiceNo and A.EditNo = @EditNo";

                string outSourceQry = @"
                    SELECT 
                         ID,Name,Type
	                    ,CASE WHEN A.Type = 'G' THEN 'Group' ELSE 'Test' END AS [LabelName]
	                    ,CASE WHEN A.Type = 'G' THEN '' ELSE ISNULL(B.[Values],'') END AS [Value]
	                    ,ISNULL(B.[Action],'') AS [Action]
	            	    ,CASE WHEN A.Type = 'G' THEN '' ELSE ISNULL(B.Comments,'') END AS [Comments] 
	                    ,CASE WHEN A.Type = 'G' THEN ISNULL(G.Section,'') ELSE ISNULL(T.Section,'') END AS [TestSection]
	                    ,CASE WHEN A.Type = 'G' THEN 0.00 ELSE ISNULL(T.HighValue,0.00) END AS HighValue 
	                    ,CASE WHEN A.Type = 'G' THEN 0.00 ELSE ISNULL(T.LowValue,0.00) END AS LowValue 
	                    ,CASE WHEN A.Type = 'G' THEN ISNULL(G.MachineName,'') ELSE ISNULL(T.MachineName,'') END AS MachineName 
	                    ,CASE WHEN A.Type = 'G' THEN '' ELSE ISNULL(T.Methord,'') END AS Method 
	                    ,CASE WHEN A.Type = 'G' THEN '' ELSE ISNULL(T.Unit,'') END AS Unit
	                    ,CASE WHEN A.Type = 'G' THEN '' ELSE ISNULL(T.NormalRange,'') END AS NormalRange
	                    ,CASE WHEN A.Type = 'G' THEN ISNULL(GroupCode,'') ELSE ISNULL(T.TestCode,'') END AS TestCode 
	                    ,CASE WHEN A.Type = 'G' THEN '' ELSE ISNULL(T.Specimen,'') END AS Specimen  
                        ,ISNULL(A.[TransactionUniqueID],'') AS [UniqueID]
                        ,'H' as [TransType],B.IsSampleOutSourced as IsOutSourced
                    FROM 
                        [dbo].[trntbl_TestEntriesLine] A
                        LEFT JOIN [dbo].[trntbl_TestEntriesResults] B ON A.ClientID = B.ClientID and A.Sequence = B.Sequence and A.InvoiceNo = B.InvoiceNo and A.EditNo = B.EditNo and A.ID = B.TestID and A.[TransactionUniqueID] = B.UniqueID
                        LEFT JOIN [dbo].[mtbl_TestMaster] T ON T.ClientID = A.ClientID and T.TestID = A.ID and A.Type = 'T'
                        LEFT JOIN [dbo].[mtbl_TestGroupMaster] G ON G.ClientID = A.ClientID and G.GroupID = A.ID and A.Type = 'G'                    
                    WHERE
                        A.ClientID = @ClientID AND A.[Sequence] = @Sequence AND A.InvoiceNo = @InvoiceNo AND A.EditNo = @EditNo
                        AND B.IsSampleOutSourced = 1 ORDER BY A.SI_No";

                var baseResults = _dbcontext.Query<BaseEntry>(baseQuery, new
                {
                    ClientID = clientID,
                    Sequence = sequence,
                    InvoiceNo = invoiceNo,
                    EditNo = editNo
                });

                var PatientResult = _dbcontext.Query(patientQuery, new
                {
                    ClientID = clientID,
                    Sequence = sequence,
                    InvoiceNo = invoiceNo,
                    EditNo = editNo
                });

                var OutSourceDt = _dbcontext.Query(outSourceQry, new
                {
                    ClientID = clientID,
                    Sequence = sequence,
                    InvoiceNo = invoiceNo,
                    EditNo = editNo
                });

                // Process each entry and fetch hierarchy for groups
                var result = new List<HierarchyEntry>();

                foreach (var entry in baseResults)
                {
                    var hierarchyEntry = new HierarchyEntry
                    {
                        ID = entry.ID,
                        Name = entry.Name,
                        Type = entry.Type,
                        LabelType = entry.LabelName,
                        Value = entry.Value,
                        Action = entry.Action,
                        Comments = entry.Comments,
                        TestSection = entry.TestSection,
                        HighValue = entry.HighValue,
                        LowValue = entry.LowValue,
                        MachineName = entry.MachineName,
                        Method = entry.Method,
                        Unit = entry.Unit,
                        NormalRange = entry.NormalRange,
                        TestCode = entry.TestCode,
                        Specimen = entry.Specimen,
                        UniqueID = entry.UniqueID,
                        TransType = entry.TransType,
                        SubArray = entry.Type == "G" ? await GetGroupHierarchy(entry.ID, clientID, sequence, invoiceNo, editNo) : new List<HierarchyEntry>()
                    };
                    result.Add(hierarchyEntry);
                }

                var Response = new
                {
                    isSucess = true,
                    message = "Success",
                    data = new
                    {
                        ResultEntry = result,
                        PatientData = PatientResult,
                        OutSourceData = OutSourceDt
                    }
                };

                return Ok(Response);
            }
            catch (Exception ex)
            {
                var Response = new
                {
                    isSucess = false,
                    message = ex.Message
                };
                return StatusCode(500, Response);
            }
        }
        #endregion

        #region Get InvoiceNo
        [HttpGet("getTestDetailsByInvoiceNo")]
        public IActionResult getTestDetailsByInvoiceNo([FromHeader] long invoiceNo)
        {
            try
            {
                string token = Request.Headers["Authorization"];
                token = token.Substring(7);

                var tokenClaims = Fluxion_Handler.GetJWTTokenClaims(token, _key._jwtKey, true);

                var parameters = new DynamicParameters();
                parameters.Add("@Flag", 114);
                parameters.Add("@ClientID", tokenClaims.ClientId);
                parameters.Add("@InvoiceNo", invoiceNo);
                var _data = _dbcontext.Query("SP_TestEntry", parameters, commandType: CommandType.StoredProcedure);

                var Response = new
                {
                    isSucess = true,
                    message = "Success",
                    data = _data
                };
                return Ok(Response);

            }
            catch (Exception ex)
            {
                _response.isSucess = false;
                _response.message = ex.Message;

                return StatusCode(500, _response);
            }
        }
        #endregion

        #region Unique ID Generation 

        #region UniqueID Updation
        [HttpPost("updateUniqueIDs")]
        public async Task<IActionResult> UpdateUniqueIDs()
        {
            try
            {
                var testEntries = GetTestEntriesToUpdate();

                foreach (var entry in testEntries)
                {
                    var resultDataList = new List<ResultEntryLines>();

                    var hierarchy = await GetHirarachy(entry.ClientID, entry.Sequence, entry.InvoiceNo, entry.EditNo);
                    if (hierarchy == null || hierarchy.Count == 0)
                    {
                        continue;
                    }

                    foreach (var h in hierarchy)
                    {
                        ExtractHierarchyEntries(h, resultDataList);
                    }

                    var resultEntry = new ResultEntry { Results = resultDataList };
                    postTestEntryResult1(entry.ClientID, entry.InvoiceNo, entry.Sequence, entry.EditNo, resultEntry);
                }

                return Ok(new { isSuccess = true, message = "Unique IDs updated successfully." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { isSuccess = false, message = ex.Message });
            }
        }

        private void postTestEntryResult1(string ClientID, long InvoiceNo, long Sequence, int EditNo, ResultEntry _trn)
        {
            try
            {
                var parameters = new DynamicParameters();
                parameters.Add("@Flag", 101);
                parameters.Add("@ClientID", ClientID);
                parameters.Add("@InvoiceNo", InvoiceNo);
                parameters.Add("@Sequence", Sequence);
                parameters.Add("@EditNo", EditNo);
                parameters.Add("@JsonData", JsonConvert.SerializeObject(_trn));
                parameters.Add("@UserID", 1);

                _dbcontext.Query("SP_TestResultEntryCopy", parameters, commandType: CommandType.StoredProcedure);
            }
            catch
            {
                // ignore
            }
        }

        private List<TestEntryDTO> GetTestEntriesToUpdate()
        {
            string query = "SELECT ClientID, Sequence, InvoiceNo, EditNo FROM trntbl_TestEntriesHdr  Where DocStatus <> 'R' and EntryDate >= '2025-01-07'";
            return _dbcontext.Query<TestEntryDTO>(query).ToList();
        }

        private void ExtractHierarchyEntries(HierarchyEntry entry, List<ResultEntryLines> resultList)
        {
            var item = new ResultEntryLines
            {
                ID = entry.ID,
                Name = entry.Name,
                Values = entry.Value,
                Action = entry.Action,
                Comments = entry.Comments,
                UniqueID = entry.UniqueID,
                transType = entry.TransType
            };

            resultList.Add(item);

            if (entry.SubArray != null)
            {
                foreach (var subItem in entry.SubArray)
                {
                    ExtractHierarchyEntries(subItem, resultList);
                }
            }
        }
        #endregion
        #endregion
    }

    public class TestEntryDTO
    {
        public string ClientID { get; set; }
        public int Sequence { get; set; }
        public long InvoiceNo { get; set; }
        public int EditNo { get; set; }
    }
}
#endregion
