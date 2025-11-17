using Fluxion_Lab.Models.General;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System.Data;
using System.Data.SqlClient;
using Dapper;
using Newtonsoft.Json;
using static Fluxion_Lab.Controllers.Authentication.AuthenticationController;
using Fluxion_Lab.Classes.DBOperations;
using Fluxion_Lab.Models.Pharmacy;
using System.Linq;

namespace Fluxion_Lab.Controllers.Transactions
{
    [Route("api/0303")] 
    public class PharmacyController : ControllerBase
    {
        private readonly JwtKey _key;
        private readonly IDbConnection _dbcontext;
        protected APIResponse _response;
        private readonly ILogger<PharmacyController> _logger;

        public PharmacyController(IOptions<JwtKey> options, IDbConnection dbcontext, APIResponse response, ILogger<PharmacyController> logger)
        {
            this._key = options.Value;
            _dbcontext = dbcontext;
            _response = response;
            _logger = logger;
        }

        #region PostOpeningStock
        [HttpPost("postOpeningStock")]
        public IActionResult PostOpeningStock([FromBody] Pharmacy.OpeningStock _openingStock)
        {
            try
            {
                string token = Request.Headers["Authorization"];
                token = token.Substring(7);

                var tokenClaims = Fluxion_Handler.GetJWTTokenClaims(token, _key._jwtKey, true);

                var parameters = new DynamicParameters();
                parameters.Add("@Flag", 100); 
                parameters.Add("@ClientID", tokenClaims.ClientId);
                parameters.Add("@InvoiceNo", _openingStock.InvNo);
                parameters.Add("@Sequence", _openingStock.sequence); 
                parameters.Add("@JsonData", JsonConvert.SerializeObject(_openingStock));
                parameters.Add("@UserID", tokenClaims.UserId); 

                var result = _dbcontext.QueryFirstOrDefault<dynamic>("SP_OpeningStock", parameters, commandType: CommandType.StoredProcedure);

                if (result != null)
                {
                    _response.isSucess = true;
                    _response.message = "Opening stock entry added successfully";
                    _response.data = result;
                }
                else
                {
                    _response.isSucess = false;
                    _response.message = "Failed to add opening stock entry";
                }

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

        #region GetOpeningStockList
        [HttpGet("getOpeningStockList")]
        public IActionResult GetOpeningStockList([FromHeader] string FromDate, [FromHeader] string ToDate)
        {
            try
            {
                string token = Request.Headers["Authorization"];
                token = token.Substring(7);

                var tokenClaims = Fluxion_Handler.GetJWTTokenClaims(token, _key._jwtKey, true);

                var parameters = new DynamicParameters();
                parameters.Add("@Flag", 101);
                parameters.Add("@ClientID", tokenClaims.ClientId);
                parameters.Add("@FromDate", FromDate);
                parameters.Add("@ToDate", ToDate);

                var rawData = _dbcontext.Query<dynamic>("SP_OpeningStock", parameters, commandType: CommandType.StoredProcedure);

                var data = rawData.Select(d => 
                {
                    var openingStock = JsonConvert.DeserializeObject<Pharmacy.OpeningStock>(d.LineItems);
                    return new Pharmacy.OpeningStockListItem
                    {
                        Sequence = openingStock.sequence,
                        InvoiceNo = openingStock.InvNo,
                        EntryDate = openingStock.EntryDate,
                        Remarks = openingStock.Notes,
                        Items = openingStock.Items
                    };
                }).ToList();

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

        #region CancelOpeningStock
        [HttpPost("cancelOpeningStock")]
        public IActionResult CancelOpeningStock([FromHeader] int Sequence, [FromHeader] long InvoiceNo, [FromBody] string CancelReason)
        {
            try
            {
                string token = Request.Headers["Authorization"];
                token = token.Substring(7);

                var tokenClaims = Fluxion_Handler.GetJWTTokenClaims(token, _key._jwtKey, true);

                var parameters = new DynamicParameters();
                parameters.Add("@Flag", 102);
                parameters.Add("@ClientID", tokenClaims.ClientId);
                parameters.Add("@Sequence", Sequence);
                parameters.Add("@InvoiceNo", InvoiceNo);
                parameters.Add("@UserID", tokenClaims.UserId);
                parameters.Add("@Cancelreason", CancelReason);

                _dbcontext.Execute("SP_OpeningStock", parameters, commandType: CommandType.StoredProcedure);

                // Verify if the cancellation was successful by checking the DocStatus
                var verifyParameters = new DynamicParameters();
                verifyParameters.Add("@ClientID", tokenClaims.ClientId);
                verifyParameters.Add("@Sequence", Sequence);
                verifyParameters.Add("@InvoiceNo", InvoiceNo);

                var cancelledRecord = _dbcontext.QueryFirstOrDefault<string>(
                    "SELECT DocStatus FROM dbo.trntbl_OpeningStocks WHERE ClientID = @ClientID AND Sequence = @Sequence AND InvoiceNo = @InvoiceNo",
                    verifyParameters);

                if (cancelledRecord == "R")
                {
                    _response.isSucess = true;
                    _response.message = "Opening stock entry cancelled successfully";
                }
                else
                {
                    _response.isSucess = false;
                    _response.message = "Failed to cancel opening stock entry";
                }

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

        #region postStockAdjustment
        [HttpPost("postStockAdjustment")]
        public IActionResult postStockAdjustment([FromBody] Pharmacy.StockAdjustment stock)
        {
            try
            {
                string token = Request.Headers["Authorization"];
                token = token.Substring(7);

                var tokenClaims = Fluxion_Handler.GetJWTTokenClaims(token, _key._jwtKey, true);

                var parameters = new DynamicParameters();
                parameters.Add("@Flag", 100);
                parameters.Add("@ClientID", tokenClaims.ClientId);
                parameters.Add("@ProductName", stock.ProductName); 
                parameters.Add("@JsonData", JsonConvert.SerializeObject(stock.Items));
                parameters.Add("@UserID", tokenClaims.UserId);

                var result = _dbcontext.QueryFirstOrDefault<dynamic>("SP_StockAdjustment", parameters, commandType: CommandType.StoredProcedure);

                if (result != null)
                {
                    _response.isSucess = true;
                    _response.message = "stock entry added successfully";
                    _response.data = result;
                }
                else
                {
                    _response.isSucess = false;
                    _response.message = "Failed to add stock entry";
                }

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

        #region GetItemBatches
        [HttpGet("getItemBatches")]
        public IActionResult GetItemBatches([FromQuery] int ItemNo)
        {
            try
            {
                string token = Request.Headers["Authorization"];
                token = token.Substring(7);

                var tokenClaims = Fluxion_Handler.GetJWTTokenClaims(token, _key._jwtKey, true);

                var parameters = new DynamicParameters();
                parameters.Add("@Flag", 103);
                parameters.Add("@ClientID", tokenClaims.ClientId);
                parameters.Add("@ItemNo", ItemNo);

                var data = _dbcontext.Query<dynamic>("SP_StockAdjustment", parameters, commandType: CommandType.StoredProcedure);

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

        #region SalesInvoiceLoad
        [HttpGet("salesInvoiceLoad")]
        public IActionResult SalesInvoiceLoad()
        {
            try
            {
                string token = Request.Headers["Authorization"];
                token = token.Substring(7);

                var tokenClaims = Fluxion_Handler.GetJWTTokenClaims(token, _key._jwtKey, true);

                var parameters = new DynamicParameters();
                parameters.Add("@Flag", 100);
                parameters.Add("@ClientID", tokenClaims.ClientId);
                parameters.Add("@UserID", tokenClaims.UserId);

                using (var multi = _dbcontext.QueryMultiple("SP_Pharmacy", parameters, commandType: CommandType.StoredProcedure))
                {
                    var nextNum = multi.ReadFirstOrDefault<dynamic>();
                    var doctors = multi.Read<dynamic>().ToList();

                    var data = new
                    {
                        NextNum = nextNum?.NextNum,
                        Doctors = doctors
                    };

                    _response.isSucess = true;
                    _response.message = "Success";
                    _response.data = data;

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
        #endregion

        #region GetSalesInvoiceItems
        [HttpGet("getSalesInvoiceItems")]
        public IActionResult GetSalesInvoiceItems([FromQuery] string FilterText = null)
        {
            try
            {
                string token = Request.Headers["Authorization"];
                token = token.Substring(7);

                var tokenClaims = Fluxion_Handler.GetJWTTokenClaims(token, _key._jwtKey, true);

                var sqlConn = _dbcontext as SqlConnection;
                string connStr = sqlConn != null ? sqlConn.ConnectionString : null;
                if (string.IsNullOrEmpty(connStr))
                {
                    throw new Exception("Unable to resolve SQL connection string. Ensure the DB context is a SqlConnection or provide a valid connection string.");
                }

                DataTable dt = new DataTable();
                using (SqlConnection conn = new SqlConnection(connStr))
                {
                    using (SqlCommand cmd = new SqlCommand("SP_Pharmacy", conn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@Flag", 101);
                        cmd.Parameters.AddWithValue("@ClientID", tokenClaims.ClientId);
                        cmd.Parameters.AddWithValue("@FilterText", (object?)FilterText ?? DBNull.Value);

                        using (SqlDataAdapter da = new SqlDataAdapter(cmd))
                        {
                            da.Fill(dt);
                        }
                    }
                }

                var data = new List<Pharmacy.SalesInvoiceItem>();
                foreach (DataRow row in dt.Rows)
                {
                    var salesInvoiceItem = new Pharmacy.SalesInvoiceItem
                    {
                        Item_No = row["Item_No"] != DBNull.Value ? Convert.ToInt64(row["Item_No"]) : 0,
                        Item_Name = row["Item_Name"] != DBNull.Value ? row["Item_Name"].ToString() : string.Empty,
                        Company = row["Company"] != DBNull.Value ? row["Company"].ToString() : string.Empty,
                        TaxCode = row["TaxCode"] != DBNull.Value ? row["TaxCode"].ToString() : string.Empty,
                        Discount1 = row["Discount1"] != DBNull.Value ? Convert.ToDecimal(row["Discount1"]) : (decimal?)null,
                        Discount2 = row["Discount2"] != DBNull.Value ? Convert.ToDecimal(row["Discount2"]) : (decimal?)null,
                        Discount3 = row["Discount3"] != DBNull.Value ? Convert.ToDecimal(row["Discount3"]) : (decimal?)null,
                        MRP = row["MRP"] != DBNull.Value ? Convert.ToDecimal(row["MRP"]) : (decimal?)null,
                        SalesPrice = row["SalesPrice"] != DBNull.Value ? Convert.ToDecimal(row["SalesPrice"]) : (decimal?)null,
                        LastPurchasePrice = row["LastPurchasePrice"] != DBNull.Value ? Convert.ToDecimal(row["LastPurchasePrice"]) : (decimal?)null,
                        Section = row["Section"] != DBNull.Value ? row["Section"].ToString() : string.Empty,
                        Chemical = row["Chemical"] != DBNull.Value ? row["Chemical"].ToString() : string.Empty,
                        HSNCode = row["HSNCode"] != DBNull.Value ? row["HSNCode"].ToString() : string.Empty,
                        Shelf = row["Shelf"] != DBNull.Value ? row["Shelf"].ToString() : string.Empty,
                        ItemStock = row["ItemStock"] != DBNull.Value ? Convert.ToDecimal(row["ItemStock"]) : (decimal?)null,
                        ItemBatch = row["ItemBatch"] != DBNull.Value && !string.IsNullOrEmpty(row["ItemBatch"].ToString())
                            ? JsonConvert.DeserializeObject<List<Pharmacy.ItemBatch>>(row["ItemBatch"].ToString())
                            : new List<Pharmacy.ItemBatch>()
                    };
                    data.Add(salesInvoiceItem);
                }

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

        #region GetSalesInvoiceDetails
        [HttpGet("getSalesInvoiceDetails")]
        public IActionResult GetSalesInvoiceDetails([FromQuery] int Sequence, [FromQuery] int InvoiceNo, [FromQuery] string FilterText = null)
        {
            try
            {
                string token = Request.Headers["Authorization"];
                token = token.Substring(7);

                var tokenClaims = Fluxion_Handler.GetJWTTokenClaims(token, _key._jwtKey, true);

                var parameters = new DynamicParameters();
                parameters.Add("@Flag", 102, DbType.Int32);
                parameters.Add("@ClientID", tokenClaims.ClientId, DbType.Int64);
                parameters.Add("@Sequence", Sequence, DbType.Int32);
                parameters.Add("@InvoiceNo", InvoiceNo, DbType.Int32);
                if (!string.IsNullOrEmpty(FilterText))
                {
                    parameters.Add("@FilterText", FilterText, DbType.String);
                }
                else
                {
                    parameters.Add("@FilterText", null, DbType.String);
                }

                using (var multi = _dbcontext.QueryMultiple("SP_Pharmacy", parameters, commandType: CommandType.StoredProcedure))
                {
                    var header = multi.ReadFirstOrDefault<dynamic>();
                    var details = multi.Read<dynamic>().ToList();
                    var itemList = multi.Read<dynamic>().ToList();

                    if (header == null)
                    {
                        _response.isSucess = false;
                        _response.message = "Sales invoice not found";
                        return NotFound(_response);
                    }

                    var data = new
                    {
                        Header = header,
                        Details = details,
                        ItemList = itemList
                    };

                    _response.isSucess = true;
                    _response.message = "Success";
                    _response.data = data;

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
        #endregion

        #region PostSalesInvoice
        [HttpPost("postSalesInvoice")]
        public IActionResult PostSalesInvoice([FromHeader] int? sequence, [FromHeader] long? invoiceNo, [FromHeader] int? editNo
            , [FromHeader] string? docStatus , [FromBody] Pharmacy.SalesInvoice salesInvoice)
        {
            try
            {
                string token = Request.Headers["Authorization"];
                token = token.Substring(7);

                var tokenClaims = Fluxion_Handler.GetJWTTokenClaims(token, _key._jwtKey, true);

                // Stock Validation
                if (salesInvoice?.Items != null && salesInvoice.Items.Any())
                {
                    var sqlConn = _dbcontext as SqlConnection;
                    string connStr = sqlConn != null ? sqlConn.ConnectionString : null;
                    if (string.IsNullOrEmpty(connStr))
                    {
                        throw new Exception("Unable to resolve SQL connection string. Ensure the DB context is a SqlConnection or provide a valid connection string.");
                    }

                    var stockValidationErrors = new List<string>();

                    using (SqlConnection conn = new SqlConnection(connStr))
                    {
                        conn.Open();
                        foreach (var item in salesInvoice.Items)
                        {
                            if (item.ItemNo == null || item.Qty == null || item.Qty <= 0)
                                continue;

                            decimal requestedQty = item.Qty.Value;
                            decimal? availableStock = null;
                            string itemName = item.ItemName ?? "Unknown Item";
                            string batchCode = item.BatchCode ?? string.Empty;

                            // Check if batch exists and get available stock
                            string query = @"SELECT Onhand FROM mtbl_ItemsBatches 
                                           WHERE ClientID = @ClientID 
                                           AND Item_No = @ItemNo 
                                           AND BatchCode = @BatchCode";

                            using (SqlCommand cmd = new SqlCommand(query, conn))
                            {
                                cmd.Parameters.AddWithValue("@ClientID", tokenClaims.ClientId);
                                cmd.Parameters.AddWithValue("@ItemNo", item.ItemNo);
                                cmd.Parameters.AddWithValue("@BatchCode", batchCode);

                                var stockResult = cmd.ExecuteScalar();
                                if (stockResult != null && stockResult != DBNull.Value)
                                {
                                    availableStock = Convert.ToDecimal(stockResult);
                                }
                            }

                            // Validate stock availability
                            if (availableStock == null)
                            {
                                stockValidationErrors.Add($"Item '{itemName}' (Batch: {batchCode}) - Batch not found in inventory.");
                            }
                            else if (availableStock.Value < requestedQty)
                            {
                                stockValidationErrors.Add($"Item '{itemName}' (Batch: {batchCode}) - Insufficient stock. Available: {availableStock.Value}, Requested: {requestedQty}.");
                            }
                        }
                    }

                    // Return error if stock validation fails
                    if (stockValidationErrors.Any())
                    {
                        _response.isSucess = false;
                        _response.message = "Stock validation failed: " + string.Join(" ", stockValidationErrors);
                        _response.data = stockValidationErrors;
                        return BadRequest(_response);
                    }
                }

                // Proceed with invoice creation if stock validation passes
                var parameters = new DynamicParameters();
                parameters.Add("@ClientID", tokenClaims.ClientId, DbType.Int64);
                parameters.Add("@Sequece", sequence);
                parameters.Add("@InvoiceNo", invoiceNo);
                parameters.Add("@EditNo", editNo);
                parameters.Add("@DocStatus", docStatus); 
                parameters.Add("@JsonData", JsonConvert.SerializeObject(salesInvoice), DbType.String);
                parameters.Add("@UserID", tokenClaims.UserId, DbType.Int32);

                var result = _dbcontext.QueryFirstOrDefault<dynamic>("SP_SalesEntryPOST", parameters, commandType: CommandType.StoredProcedure);

                if (result != null)
                {
                    _response.isSucess = true;
                    _response.message = result.TransMessage?.ToString() ?? "Sales invoice created successfully";
                    _response.data = result;
                }
                else
                {
                    _response.isSucess = false;
                    _response.message = "Failed to create sales invoice";
                }

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

        #region PostSalesInvoice
        [HttpPost("postSalesReturn")]
        public IActionResult PostSalesReturn([FromHeader] int? sequence, [FromHeader] long? invoiceNo, [FromHeader] int? editNo
            , [FromHeader] string? docStatus, [FromBody] Pharmacy.SalesInvoice salesInvoice)
        {
            try
            {
                string token = Request.Headers["Authorization"];
                token = token.Substring(7);

                var tokenClaims = Fluxion_Handler.GetJWTTokenClaims(token, _key._jwtKey, true); 

                // Proceed with invoice creation if stock validation passes
                var parameters = new DynamicParameters();
                parameters.Add("@ClientID", tokenClaims.ClientId, DbType.Int64);
                parameters.Add("@Sequece", sequence);
                parameters.Add("@InvoiceNo", invoiceNo);
                parameters.Add("@EditNo", editNo);
                parameters.Add("@DocStatus", docStatus);
                parameters.Add("@JsonData", JsonConvert.SerializeObject(salesInvoice), DbType.String);
                parameters.Add("@UserID", tokenClaims.UserId, DbType.Int32);

                var result = _dbcontext.QueryFirstOrDefault<dynamic>("SP_SalesReturnPOST", parameters, commandType: CommandType.StoredProcedure);

                if (result != null)
                {
                    _response.isSucess = true;
                    _response.message = result.TransMessage?.ToString() ?? "Sales Return created successfully";
                    _response.data = result;
                }
                else
                {
                    _response.isSucess = false;
                    _response.message = "Failed to create Sales Return";
                }

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
