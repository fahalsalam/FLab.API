using Dapper;
using Fluxion_Lab.Classes.DBOperations;
using Fluxion_Lab.Models.General;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using System.Data;
using static Fluxion_Lab.Controllers.Authentication.AuthenticationController;
using static Fluxion_Lab.Models.Transactions.Expense.ExpenseEntry;

namespace Fluxion_Lab.Controllers.Transactions
{
    [Route("api/0205")]
    [Authorize]
    public class ExpenseController : ControllerBase
    {
        private readonly JwtKey _key;
        private readonly IDbConnection _dbcontext;
        protected APIResponse _response;

        public ExpenseController(IOptions<JwtKey> options, IDbConnection dbcontext, APIResponse response)
        {
            this._key = options.Value;
            _dbcontext = dbcontext;
            _response = response;
        }

        /************* Expense Entry ***************/

        #region Post Expense Entry
        [HttpPost("postExpenseEntry")]
        public IActionResult PostExpenseEntry([FromBody] ExpenseEntryRequest expenseEntry)
        {
            try
            {
                string token = Request.Headers["Authorization"];
                token = token.Substring(7);

                var tokenClaims = Fluxion_Handler.GetJWTTokenClaims(token, _key._jwtKey, true);

                var parameters = new DynamicParameters();
                parameters.Add("@Flag", 100);
                parameters.Add("@ClientID", tokenClaims.ClientId);
                parameters.Add("@JsonData", JsonConvert.SerializeObject(expenseEntry));
                parameters.Add("@UserID", tokenClaims.UserId);

                var data = _dbcontext.Query("SP_ExpenseEntry", parameters, commandType: CommandType.StoredProcedure);

                _response.isSucess = true;
                _response.message = "Expense entry created successfully";
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

        #region Get Expense Entries
        [HttpGet("getExpenseEntries")]
        public IActionResult GetExpenseEntries([FromHeader] string? FromDate, [FromHeader] string? ToDate)
        {
            try
            {
                string token = Request.Headers["Authorization"];
                token = token.Substring(7);

                var tokenClaims = Fluxion_Handler.GetJWTTokenClaims(token, _key._jwtKey, true);

                var parameters = new DynamicParameters();
                parameters.Add("@Flag", 101);
                parameters.Add("@ClientID", tokenClaims.ClientId);
                parameters.Add("@FromDate", string.IsNullOrEmpty(FromDate) ? null : FromDate);
                parameters.Add("@ToDate", string.IsNullOrEmpty(ToDate) ? null : ToDate);

                var data = _dbcontext.Query<ExpenseEntryResponse>("SP_ExpenseEntry", parameters, commandType: CommandType.StoredProcedure);

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
    }
}
