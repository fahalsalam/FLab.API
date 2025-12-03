using Fluxion_Lab.Models.General;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System.Data;
using Dapper;
using Newtonsoft.Json;
using Fluxion_Lab.Models.Pharmacy;
using System.Linq;
using Newtonsoft.Json.Linq;
using static Fluxion_Lab.Controllers.Authentication.AuthenticationController;
using Fluxion_Lab.Classes.DBOperations;

namespace Fluxion_Lab.Controllers.Reports
{
    [Route("api/0303/reports/pharmacy")]
    [ApiController]
    public class PharmacyReportsController : ControllerBase
    {
        private readonly JwtKey _key;
        private readonly IDbConnection _dbcontext;
        protected APIResponse _response;
        private readonly ILogger<PharmacyReportsController> _logger;

        public PharmacyReportsController(IOptions<JwtKey> options, IDbConnection dbcontext, APIResponse response, ILogger<PharmacyReportsController> logger)
        {
            _key = options.Value;
            _dbcontext = dbcontext;
            _response = response;
            _logger = logger;
        }

        // GET: api/0303/reports/pharmacy/purchaseReturnDrafts
        [HttpGet("purchaseReturnDrafts")]
        public IActionResult GetPurchaseReturnDrafts()
        {
            try
            {
                string token = Request.Headers["Authorization"];
                token = token?.Substring(7);

                var tokenClaims = Fluxion_Handler.GetJWTTokenClaims(token, _key._jwtKey, true);

                var parameters = new DynamicParameters();
                parameters.Add("@Flag", 101);
                parameters.Add("@ClientID", tokenClaims.ClientId, DbType.Int64);

                using (var multi = _dbcontext.QueryMultiple("SP_PurchaseReturnDarft", parameters, commandType: CommandType.StoredProcedure))
                {
                    var headers = multi.Read<dynamic>().ToList();
                    var details = multi.Read<dynamic>().ToList();

                    _response.isSucess = true;
                    _response.message = "Success";
                    _response.data = new { Headers = headers, Details = details };

                    return Ok(_response);
                }
            }
            catch (Exception ex)
            {
                _response.isSucess = false;
                _response.message = ex.Message;
                return StatusCode(500, _response);
            }
        }

        // GET: api/0303/reports/pharmacy/salesReturnReport
        [HttpGet("salesReturnReport")]
        public IActionResult GetSalesReturnReport([FromQuery] string? rpType, [FromQuery] DateTime? fromDate,
            [FromQuery] DateTime? toDate, [FromQuery] long? searchKey, [FromQuery] int? patientId, [FromQuery] int? itemNo)
        {
            try
            {
                string token = Request.Headers["Authorization"];
                token = token?.Substring(7);

                var tokenClaims = Fluxion_Handler.GetJWTTokenClaims(token, _key._jwtKey, true);

                var parameters = new DynamicParameters();
                parameters.Add("@Flag", 102);
                parameters.Add("@ClientID", tokenClaims.ClientId, DbType.Int64);
                if (fromDate.HasValue) parameters.Add("@FromDate", fromDate.Value.Date, DbType.Date);
                if (toDate.HasValue) parameters.Add("@ToDate", toDate.Value.Date, DbType.Date);
                if (!string.IsNullOrEmpty(rpType)) parameters.Add("@RpType", rpType, DbType.String);
                if (searchKey.HasValue) parameters.Add("@SearchKey", searchKey.Value, DbType.Int64);
                if (patientId.HasValue) parameters.Add("@PatientID", patientId.Value, DbType.Int64);
                if (itemNo.HasValue) parameters.Add("@ItemNo", itemNo.Value, DbType.Int32);

                var rawData = _dbcontext.Query<dynamic>("SP_PharmacyReports", parameters, commandType: CommandType.StoredProcedure).ToList();

                // Transform rows: deserialize Items JSON into model list when present
                var transformed = rawData.Select(r =>
                {
                    var jo = JsonConvert.DeserializeObject<JObject>(JsonConvert.SerializeObject(r));
                    var items = new List<Pharmacy.SalesReturnItem>();
                    JToken itemsToken = null;
                    if (jo != null && jo.TryGetValue("Items", out itemsToken) && itemsToken != null)
                    {
                        var itemsJson = itemsToken.Type == JTokenType.String ? itemsToken.ToString() : itemsToken.ToString(Formatting.None);
                        if (!string.IsNullOrEmpty(itemsJson) && itemsJson != "null")
                        {
                            try
                            {
                                items = JsonConvert.DeserializeObject<List<Pharmacy.SalesReturnItem>>(itemsJson) ?? new List<Pharmacy.SalesReturnItem>();
                            }
                            catch
                            {
                                items = new List<Pharmacy.SalesReturnItem>();
                            }
                        }
                    }

                    var dict = jo?.ToObject<Dictionary<string, object>>() ?? new Dictionary<string, object>();
                    dict["Items"] = items;
                    return dict;
                }).ToList();

                _response.isSucess = true;
                _response.message = "Success";
                _response.data = transformed;
                return Ok(_response);
            }
            catch (Exception ex)
            {
                _response.isSucess = false;
                _response.message = ex.Message;
                return StatusCode(500, _response);
            }
        }

        // GET: api/0303/reports/pharmacy/salesReport
        [HttpGet("salesReport")]
        public IActionResult GetSalesReport([FromQuery] string? rpType, [FromQuery] DateTime? fromDate,
            [FromQuery] DateTime? toDate, [FromQuery] long? searchKey, [FromQuery] int? patientId, [FromQuery] int? itemNo)
        {
            try
            {
                string token = Request.Headers["Authorization"];
                token = token?.Substring(7);

                var tokenClaims = Fluxion_Handler.GetJWTTokenClaims(token, _key._jwtKey, true);

                var parameters = new DynamicParameters();
                parameters.Add("@Flag", 100);
                parameters.Add("@ClientID", tokenClaims.ClientId, DbType.Int64);
                if (fromDate.HasValue) parameters.Add("@FromDate", fromDate.Value.Date, DbType.Date);
                if (toDate.HasValue) parameters.Add("@ToDate", toDate.Value.Date, DbType.Date);
                if (!string.IsNullOrEmpty(rpType)) parameters.Add("@RpType", rpType, DbType.String);
                if (searchKey.HasValue) parameters.Add("@SearchKey", searchKey.Value, DbType.Int64);
                if (patientId.HasValue) parameters.Add("@PatientID", patientId.Value, DbType.Int64);
                if (itemNo.HasValue) parameters.Add("@ItemNo", itemNo.Value, DbType.Int32);

                var data = _dbcontext.Query<dynamic>("SP_PharmacyReports", parameters, commandType: CommandType.StoredProcedure);

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

        // GET: api/0303/reports/pharmacy/purchaseReport
        [HttpGet("purchaseReport")]
        public IActionResult GetPurchaseReport([FromQuery] string? rpType, [FromQuery] DateTime? fromDate, [FromQuery] DateTime? toDate, [FromQuery] long? searchKey, [FromQuery] int? itemNo)
        {
            try
            {
                string token = Request.Headers["Authorization"];
                token = token?.Substring(7);

                var tokenClaims = Fluxion_Handler.GetJWTTokenClaims(token, _key._jwtKey, true);

                var parameters = new DynamicParameters();
                parameters.Add("@Flag", 101);
                parameters.Add("@ClientID", tokenClaims.ClientId, DbType.Int64);
                if (fromDate.HasValue) parameters.Add("@FromDate", fromDate.Value.Date, DbType.Date);
                if (toDate.HasValue) parameters.Add("@ToDate", toDate.Value.Date, DbType.Date);
                if (!string.IsNullOrEmpty(rpType)) parameters.Add("@RpType", rpType, DbType.String);
                if (searchKey.HasValue) parameters.Add("@SearchKey", searchKey.Value, DbType.Int64);
                if (itemNo.HasValue) parameters.Add("@ItemNo", itemNo.Value, DbType.Int32);

                var data = _dbcontext.Query<dynamic>("SP_PharmacyReports", parameters, commandType: CommandType.StoredProcedure);

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

        // GET: api/0303/reports/pharmacy/openingStockReport
        [HttpGet("openingStockReport")]
        public IActionResult GetOpeningStockReport([FromQuery] DateTime? fromDate, [FromQuery] DateTime? toDate)
        {
            try
            {
                string token = Request.Headers["Authorization"];
                token = token?.Substring(7);

                var tokenClaims = Fluxion_Handler.GetJWTTokenClaims(token, _key._jwtKey, true);

                var parameters = new DynamicParameters();
                parameters.Add("@Flag", 103);
                parameters.Add("@ClientID", tokenClaims.ClientId, DbType.Int64);
                if (fromDate.HasValue) parameters.Add("@FromDate", fromDate.Value.Date, DbType.Date);
                if (toDate.HasValue) parameters.Add("@ToDate", toDate.Value.Date, DbType.Date);

                var data = _dbcontext.Query<dynamic>("SP_PharmacyReports", parameters, commandType: CommandType.StoredProcedure);

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

        // GET: api/0303/reports/pharmacy/stockAdjustmentReport
        [HttpGet("stockAdjustmentReport")]
        public IActionResult GetStockAdjustmentReport([FromQuery] DateTime? fromDate, [FromQuery] DateTime? toDate)
        {
            try
            {
                string token = Request.Headers["Authorization"];
                token = token?.Substring(7);

                var tokenClaims = Fluxion_Handler.GetJWTTokenClaims(token, _key._jwtKey, true);

                var parameters = new DynamicParameters();
                parameters.Add("@Flag", 104);
                parameters.Add("@ClientID", tokenClaims.ClientId, DbType.Int64);
                if (fromDate.HasValue) parameters.Add("@FromDate", fromDate.Value.Date, DbType.Date);
                if (toDate.HasValue) parameters.Add("@ToDate", toDate.Value.Date, DbType.Date);

                var data = _dbcontext.Query<dynamic>("SP_PharmacyReports", parameters, commandType: CommandType.StoredProcedure);

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

        // GET: api/0303/reports/pharmacy/reorderLevelReport
        [HttpGet("reorderLevelReport")]
        public IActionResult GetReorderLevelReport([FromQuery] int pageNo = 1, [FromQuery] int pageSize = 10)
        {
            try
            {
                string token = Request.Headers["Authorization"];
                token = token?.Substring(7);

                var tokenClaims = Fluxion_Handler.GetJWTTokenClaims(token, _key._jwtKey, true);

                var parameters = new DynamicParameters();
                parameters.Add("@Flag", 105);
                parameters.Add("@ClientID", tokenClaims.ClientId, DbType.Int64);
                parameters.Add("@PageNo", pageNo, DbType.Int32);
                parameters.Add("@PageSize", pageSize, DbType.Int32);

                var data = _dbcontext.Query<dynamic>("SP_PharmacyReports", parameters, commandType: CommandType.StoredProcedure);

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

        // GET: api/0303/reports/pharmacy/itemPurchaseHistory
        [HttpGet("itemPurchaseHistory")]
        public IActionResult GetItemPurchaseHistory([FromQuery] int itemNo)
        {
            try
            {
                string token = Request.Headers["Authorization"];
                token = token?.Substring(7);

                var tokenClaims = Fluxion_Handler.GetJWTTokenClaims(token, _key._jwtKey, true);

                var parameters = new DynamicParameters();
                parameters.Add("@Flag", 106);
                parameters.Add("@ClientID", tokenClaims.ClientId, DbType.Int64);
                parameters.Add("@ItemNo", itemNo, DbType.Int32);

                var data = _dbcontext.Query<dynamic>("SP_PharmacyReports", parameters, commandType: CommandType.StoredProcedure);

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
    }
}
