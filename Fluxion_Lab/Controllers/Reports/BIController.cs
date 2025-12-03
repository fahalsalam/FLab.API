using Dapper;
using Fluxion_Lab.Classes.DBOperations;
using Fluxion_Lab.Models.General;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System.Data;
using System.Data.SqlClient;
using static Fluxion_Lab.Models.General.BodyParams;
using static Fluxion_Lab.Models.Masters.Machine_Analyzer.MachineAnalyzer;
using static Fluxion_Lab.Models.Reports;

namespace Fluxion_Lab.Controllers.Reports
{
    [Route("api/8978")]
     
    public class BIController : ControllerBase
    {
        private readonly JwtKey _key;
        private readonly IDbConnection _dbcontext;
        protected APIResponse _response;

        public BIController(IOptions<JwtKey> options, IDbConnection dbcontext, APIResponse response)
        {
            this._key = options.Value;
            _dbcontext = dbcontext;
            _response = response;
        }

        #region Sales Report
        [HttpPost("getSalesReport")]
        public IActionResult getSalesReport([FromBody] salesReports _rp)
        {
            try
            {
                string token = Request.Headers["Authorization"];
                token = token.Substring(7);

                var tokenClaims = Fluxion_Handler.GetJWTTokenClaims(token, _key._jwtKey, true);

                int? _flag = 100;
                if(_rp.groupby == "Bill Wise")
                {
                    _flag = 100;
                } else if(_rp.groupby == "Test Wise")
                {
                    _flag = 101;
                }
                else if (_rp.groupby == "Group Wise")
                {
                    _flag = 102;
                }
                else if (_rp.groupby == "Doctor Wise")
                {
                    _flag = 104;
                }
                else if (_rp.groupby == "Lab Wise")
                {
                    _flag = 105;
                }
                else if(_rp.groupby == "Item Wise")
                {
                    _flag = 108;
                }


                var parameters = new DynamicParameters();
                parameters.Add("@Flag", _flag);
                parameters.Add("@ClientID", tokenClaims.ClientId);
                parameters.Add("@PageNo", _rp.pageNo);
                parameters.Add("@PageSize", _rp.pageSize);
                parameters.Add("@FromDate", _rp.fromDate);
                parameters.Add("@ToDate", _rp.toDate);
                parameters.Add("@SearchKey", _rp.searchKey);
                parameters.Add("@ItemType", _rp.itemtype); 

                var data = _dbcontext.Query("SP_Reports", parameters, commandType: CommandType.StoredProcedure); 

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

        #region Purchase Report
        [HttpPost("getPurchaseReport")]
        public IActionResult GetPurchaseList([FromBody] PurchaseReport _pr)
        {
            try
            {
                int? flag = 0;
                string token = Request.Headers["Authorization"];
                token = token.Substring(7);

                var tokenClaims = Fluxion_Handler.GetJWTTokenClaims(token, _key._jwtKey, true);

                int? _flag = 106;
                if (_pr.groupby == "Bill Wise")
                {
                    _flag = 106;
                }
                else if (_pr.groupby == "Item Wise")
                {
                    _flag = 107;
                }
                else if (_pr.groupby == "Supplier Wise")
                {
                    _flag = 108;
                }

                var parameters = new DynamicParameters();
                parameters.Add("@Flag", _flag);
                parameters.Add("@ClientID", tokenClaims.ClientId);
                parameters.Add("@PageNo", _pr.pageNo);
                parameters.Add("@PageSize", _pr.pageSize);
                parameters.Add("@FromDate", _pr.fromDate);
                parameters.Add("@ToDate", _pr.toDate);
                parameters.Add("@SearchKey", _pr.searchKey);

                var data = _dbcontext.Query("SP_Reports", parameters, commandType: CommandType.StoredProcedure);

                _response.isSucess = true;
                _response.message = "Success";
                _response.data = data;
                return Ok(_response);

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

        #region Sales Report Page Load
        [HttpGet("getsalesReportDataSets")]
        public IActionResult getsalesReportDataSets()
        {
            try
            {
                string token = Request.Headers["Authorization"];
                token = token.Substring(7);

                var tokenClaims = Fluxion_Handler.GetJWTTokenClaims(token, _key._jwtKey, true);

                var parameters = new DynamicParameters();
                parameters.Add("@Flag", 103);
                parameters.Add("@ClientID", tokenClaims.ClientId); 

                var data = _dbcontext.QueryMultiple("SP_Reports", parameters, commandType: CommandType.StoredProcedure);

                var _testMaster = data.Read<dynamic>().ToList();
                var _doctorMaster = data.Read<dynamic>().ToList();
                var _labMaster = data.Read<dynamic>().ToList();
                var _testGroup = data.Read<dynamic>().ToList();
                var _itemTypes = data.Read<dynamic>().ToList();


                var Response = new
                {
                    isSucess = true,
                    message = "Success",
                    data = new
                    {
                        testMaster = _testMaster,
                        doctorMaster = _doctorMaster,
                        labMaster = _labMaster,
                        testGroupMaster = _testGroup,
                        itemTypes = _itemTypes
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

        #region Get Expiry Product Details For DashBoard
        [HttpGet("getExpiredProductDetails")]
        public IActionResult getExpiredProductDetails()
        {
            try
            {
                string token = Request.Headers["Authorization"];
                token = token.Substring(7);

                var tokenClaims = Fluxion_Handler.GetJWTTokenClaims(token, _key._jwtKey, true);

                var parameters = new DynamicParameters();
                parameters.Add("@Flag", 100);
                parameters.Add("@ClientID", tokenClaims.ClientId);

                var data = _dbcontext.Query("SP_DashBoard", parameters, commandType: CommandType.StoredProcedure); 

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

        #region Collection Report
        [HttpGet("getCollectionReport")] 
        public IActionResult getCollectionReport([FromHeader] string? fromDate, [FromHeader] string? toDate)
        {
            try
            {
                string token = Request.Headers["Authorization"];
                token = token.Substring(7);

                var tokenClaims = Fluxion_Handler.GetJWTTokenClaims(token, _key._jwtKey, true);

                var parameters = new DynamicParameters();
                parameters.Add("@Flag", 110);
                parameters.Add("@ClientID", tokenClaims.ClientId);
                parameters.Add("@FromDate", fromDate);
                parameters.Add("@ToDate", toDate);

                var _data = _dbcontext.Query("SP_Reports", parameters, commandType: CommandType.StoredProcedure);

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


        #region Collection Report Receipts
        [HttpGet("getCollectionReportReceipts")]
        public IActionResult getCollectionReportReceipts([FromHeader] string? fromDate, [FromHeader] string? toDate)
        {
            try
            {
                string token = Request.Headers["Authorization"];
                token = token.Substring(7);

                var tokenClaims = Fluxion_Handler.GetJWTTokenClaims(token, _key._jwtKey, true);

                var parameters = new DynamicParameters();
                parameters.Add("@Flag", 111);
                parameters.Add("@ClientID", tokenClaims.ClientId);
                parameters.Add("@FromDate", fromDate);
                parameters.Add("@ToDate", toDate);

                var result = _dbcontext.QueryMultiple("SP_Reports", parameters, commandType: CommandType.StoredProcedure);

                // Fetch summary data
                var summaryData = result.Read<dynamic>().ToList();

                // Fetch receipt details
                var receiptDetails = result.Read<dynamic>().ToList();

                // Group receipt details by date
                var groupedData = summaryData.Select(summary => new
                {
                    Date = summary.Date,
                    TotalReceipts = summary.TotalReceipts,
                    TotalCollection = summary.TotalCollection,
                    TotalCash = summary.TotalCash,
                    TotalBank = summary.TotalBank,
                    WalletIn = summary.WalletIn,
                    WalletOut = summary.WalletOut,  
                    Receipts = receiptDetails
                        .Where(receipt => receipt.ReceiptDate == summary.Date)
                        .Select(receipt => new
                        {
                            ReceiptNo = receipt.ReceiptNo,
                            PatientName = receipt.PatientName,
                            ReceiptDate = receipt.ReceiptDate,
                            InvoiceNo = receipt.InvoiceNo,
                            Sequence = receipt.Sequence,
                            InvoiceEditNo = receipt.InvoiceEditNo,
                            CashAmount = receipt.CashAmount,
                            BankAmount = receipt.BankAmount,
                            WalletIn = receipt.WalletIn,
                            WalletOut = receipt.WalletOut,
                        }).ToList()
                }).ToList();

                var response = new
                {
                    isSucess = true,
                    message = "Success",
                    data = groupedData
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                _response.isSucess = false;
                _response.message = ex.Message;

                return StatusCode(500, _response);
            }
        }

        #endregion

        #region Item Batch Stock Report
        [HttpGet("getItemBatchStockReport")]
        public IActionResult getItemBatchStockReport()
        {
            try
            {
                string token = Request.Headers["Authorization"];
                token = token.Substring(7);

                var tokenClaims = Fluxion_Handler.GetJWTTokenClaims(token, _key._jwtKey, true);

                var parameters = new DynamicParameters();
                parameters.Add("@Flag", 112);
                parameters.Add("@ClientID", tokenClaims.ClientId);
                
                var _data = _dbcontext.Query("SP_Reports", parameters, commandType: CommandType.StoredProcedure);

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

        #region Wallet Transaction Report
        [HttpGet("getwalletTransaction")]
        public IActionResult getwalletTransaction([FromHeader] long patientID)
        {
            try
            {
                string token = Request.Headers["Authorization"];
                token = token.Substring(7);

                var tokenClaims = Fluxion_Handler.GetJWTTokenClaims(token, _key._jwtKey, true);

                var parameters = new DynamicParameters();
                parameters.Add("@Flag", 113); 
                parameters.Add("@ClientID", tokenClaims.ClientId);
                parameters.Add("@PatientID", patientID);

                var _data = _dbcontext.Query("SP_Reports", parameters, commandType: CommandType.StoredProcedure);

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

        // DashBooard API's

        #region Transaction Summary 
        [HttpGet("getTransactionSummary")]
        public IActionResult getTransactionSummary()
        {
            try
            {
                string token = Request.Headers["Authorization"];
                token = token.Substring(7);
               
                var tokenClaims = Fluxion_Handler.GetJWTTokenClaims(token, _key._jwtKey, true);
                var parameters = new DynamicParameters();
               
                parameters.Add("@Flag", 114);
                parameters.Add("@ClientID", tokenClaims.ClientId);
                
                var data = _dbcontext.QueryMultiple("SP_Reports", parameters, commandType: CommandType.StoredProcedure);

                var _salesSummary = data.Read<dynamic>().ToList();
                var _purchaseSummary = data.Read<dynamic>().ToList();
                var _patientSummary = data.Read<dynamic>().ToList();
                var _todaySummary = data.Read<dynamic>().ToList();


                var Response = new
                {
                    isSucess = true,
                    message = "Success",
                    data = new
                    {
                        salesSummary = _salesSummary,
                        purchaseSummary = _purchaseSummary,
                        patientSummary = _patientSummary,
                        todaySummary = _todaySummary 
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

        #region Transaction Summary 
        [HttpGet("getTransactionSummarybyGraph")]
        public IActionResult getTransactionSummarybyGraph([FromHeader] string graphType)
        {
            try
            {
                string token = Request.Headers["Authorization"];
                token = token.Substring(7);

                var tokenClaims = Fluxion_Handler.GetJWTTokenClaims(token, _key._jwtKey, true);
                var parameters = new DynamicParameters();

                parameters.Add("@Flag", 115);
                parameters.Add("@ClientID", tokenClaims.ClientId);
                parameters.Add("@GraphType", graphType); 

                var data = _dbcontext.Query("SP_Reports", parameters, commandType: CommandType.StoredProcedure); 

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

        #region Receipt Details Summary
        [HttpGet("getReceiptDetailsSummary")]
        public IActionResult GetReceiptDetailsSummary([FromHeader] string? fromDate, [FromHeader] string? toDate)
        {
            try
            {
                string token = Request.Headers["Authorization"];
                token = token.Substring(7);

                var tokenClaims = Fluxion_Handler.GetJWTTokenClaims(token, _key._jwtKey, true);

                var parameters = new DynamicParameters();
                parameters.Add("@Flag", 116);
                parameters.Add("@ClientID", tokenClaims.ClientId);
                parameters.Add("@FromDate", fromDate);
                parameters.Add("@ToDate", toDate);

                var data = _dbcontext.Query("SP_Reports", parameters, commandType: CommandType.StoredProcedure);

                var response = new
                {
                    isSucess = true,
                    message = "Success",
                    data = data
                };
                return Ok(response);
            }
            catch (Exception ex)
            {
                _response.isSucess = false;
                _response.message = ex.Message;
                return StatusCode(500, _response);
            }
        }
        #endregion

        #region OP Report (Flag 117)
        [HttpGet("getOPReport")]
        public IActionResult GetOPReport([FromHeader] string? fromDate, [FromHeader] string? toDate)
        {
            try
            {
                string token = Request.Headers["Authorization"];
                token = token.Substring(7);
                var tokenClaims = Fluxion_Handler.GetJWTTokenClaims(token, _key._jwtKey, true);

                // Use the connection string from the current _dbcontext if possible
                var sqlConn = _dbcontext as SqlConnection;
                string connStr = sqlConn != null ? sqlConn.ConnectionString : null;
                if (string.IsNullOrEmpty(connStr))
                {
                    throw new Exception("Unable to resolve SQL connection string. Ensure the DB context is a SqlConnection or provide a valid connection string.");
                }

                DataTable dt = new DataTable();
                using (SqlConnection conn = new SqlConnection(connStr))
                {
                    using (SqlCommand cmd = new SqlCommand("SP_Reports", conn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@Flag", 117);
                        cmd.Parameters.AddWithValue("@ClientID", tokenClaims.ClientId);
                        cmd.Parameters.AddWithValue("@FromDate", (object?)fromDate ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@ToDate", (object?)toDate ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@SearchKey", -1);
                        cmd.Parameters.AddWithValue("@PatientID", DBNull.Value);
                        cmd.Parameters.AddWithValue("@GraphType", DBNull.Value);

                        using (SqlDataAdapter da = new SqlDataAdapter(cmd))
                        {
                            da.Fill(dt);
                        }
                    }
                }

                // Convert DataTable to List<Dictionary<string, object>> for serialization
                var rows = new List<Dictionary<string, object>>();
                foreach (DataRow row in dt.Rows)
                {
                    var dict = new Dictionary<string, object>();
                    foreach (DataColumn col in dt.Columns)
                    {
                        dict[col.ColumnName] = row[col];
                    }
                    rows.Add(dict);
                }

                var response = new
                {
                    isSucess = true,
                    message = "Success",
                    data = rows
                };
                return Ok(response);
            }
            catch (Exception ex)
            {
                var response = new
                {
                    isSucess = false,
                    message = ex.Message,
                    data = (object)null
                };
                return StatusCode(500, response);
            }
        }
        #endregion

        #region OutSource Summmry Report
        [HttpGet("getOutSourceTestsSummary")]
        public IActionResult GetOutSourceTestsSummary([FromHeader] int labId, [FromHeader] int testId, [FromHeader] string? fromDate, [FromHeader] string? toDate)
        {
            try
            {
                string token = Request.Headers["Authorization"];
                token = token.Substring(7);

                var tokenClaims = Fluxion_Handler.GetJWTTokenClaims(token, _key._jwtKey, true);

                // Get connection string from _dbcontext
                var sqlConn = _dbcontext as SqlConnection;
                string connStr = sqlConn != null ? sqlConn.ConnectionString : null;
                if (string.IsNullOrEmpty(connStr))
                {
                    throw new Exception("Unable to resolve SQL connection string. Ensure the DB context is a SqlConnection or provide a valid connection string.");
                }

                var resultList = new List<object>();
                using (SqlConnection conn = new SqlConnection(connStr))
                {
                    using (SqlCommand cmd = new SqlCommand("SP_Reports", conn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@Flag", 118);
                        cmd.Parameters.AddWithValue("@ClientID", tokenClaims.ClientId);
                        cmd.Parameters.AddWithValue("@LabID", labId);
                        cmd.Parameters.AddWithValue("@TestID", testId);
                        cmd.Parameters.AddWithValue("@FromDate", (object?)fromDate ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@ToDate", (object?)toDate ?? DBNull.Value);

                        conn.Open();
                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                resultList.Add(new {
                                    Sequence = reader["Sequence"] != DBNull.Value ? reader["Sequence"] : null,
                                    InvoiceNo = reader["InvoiceNo"] != DBNull.Value ? reader["InvoiceNo"] : null,
                                    EditNo = reader["EditNo"] != DBNull.Value ? reader["EditNo"] : null,
                                    ID = reader["ID"] != DBNull.Value ? reader["ID"] : null,
                                    Name = reader["Name"] != DBNull.Value ? reader["Name"] : null,
                                    Type = reader["Type"] != DBNull.Value ? reader["Type"] : null,
                                    LabID = reader["LabID"] != DBNull.Value ? reader["LabID"] : null,
                                    LabName = reader["LabName"] != DBNull.Value ? reader["LabName"] : null,
                                    Amount = reader["Amount"] != DBNull.Value ? reader["Amount"] : null ,
                                    CollectionDateTime = reader["CollectionDateTime"] != DBNull.Value ? reader["CollectionDateTime"] : null,
                                    ContactNo = reader["ContactNo"] != DBNull.Value ? reader["ContactNo"] : null,
                                    ContactPersonName = reader["ContactPersonName"] != DBNull.Value ? reader["ContactPersonName"] : null 
                                });
                            }
                        }
                    }
                }

                return Ok(new
                {
                    isSuccess = true,
                    message = "Success",
                    data = resultList
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    isSuccess = false,
                    message = ex.Message
                });
            }
        } 
        #endregion

        #region Stock Ageing Report
        [HttpGet("getStockAgingReport")]
        public IActionResult GetStockAgingReport([FromHeader] int itemNo, [FromHeader] string? fromDate, [FromHeader] string? toDate)
        {
            try
            {
                string token = Request.Headers["Authorization"];
                token = token.Substring(7);

                var tokenClaims = Fluxion_Handler.GetJWTTokenClaims(token, _key._jwtKey, true);

                var parameters = new DynamicParameters();
                parameters.Add("@Flag", 119);
                parameters.Add("@ClientID", tokenClaims.ClientId);
                parameters.Add("@ItemNo", itemNo);
                parameters.Add("@FromDate", fromDate);
                parameters.Add("@ToDate", toDate);

                var result = _dbcontext.QueryMultiple("SP_Reports", parameters, commandType: CommandType.StoredProcedure);

                // Fetch summary data
                var summaryData = result.Read<dynamic>().ToList();

                // Fetch ledger data
                var ledgerData = result.Read<dynamic>().ToList();

                var response = new
                {
                    isSucess = true,
                    message = "Success",
                    data = new
                    {
                        summary = summaryData,
                        ledger = ledgerData
                    }
                };
                return Ok(response);
            }
            catch (Exception ex)
            {
                _response.isSucess = false;
                _response.message = ex.Message;

                return StatusCode(500, _response);
            }
        }
        #endregion

        #region Reception Transaction Summary 
        [HttpGet("ReceptionSummarybyGraph")]
        public IActionResult ReceptionSummarybyGraph([FromHeader] string graphType)
        {
            try
            {
                string token = Request.Headers["Authorization"];
                token = token.Substring(7);

                var tokenClaims = Fluxion_Handler.GetJWTTokenClaims(token, _key._jwtKey, true);
                var parameters = new DynamicParameters();

                parameters.Add("@Flag", 120);
                parameters.Add("@ClientID", tokenClaims.ClientId);
                parameters.Add("@GraphType", graphType);

                var data = _dbcontext.Query("SP_Reports", parameters, commandType: CommandType.StoredProcedure);

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

    }
}
