using Fluxion_Lab.Models.General;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System.Data;
using Dapper;
using Newtonsoft.Json;
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
    }
}
