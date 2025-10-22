using Dapper;
using Fluxion_Lab.Classes.DBOperations;
using Fluxion_Lab.Models.General;
using Fluxion_Lab.Models.Reception;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System.Data;
using System.Data.SqlClient;
using static Fluxion_Lab.Models.Reception.Reception;
using Fluxion_Lab.Services.Printing;
using Newtonsoft.Json;

namespace Fluxion_Lab.Controllers.Reception
{
    [Route("api/9989")] 
    public class ReceptionController : ControllerBase
    {
        private readonly JwtKey _key;
        private readonly IDbConnection _dbcontext;
        protected APIResponse _response;
        private readonly RawPrinterService _printerService;

        public ReceptionController(IOptions<JwtKey> options, IDbConnection dbcontext, APIResponse response, RawPrinterService printerService)
        {
            this._key = options.Value;
            _dbcontext = dbcontext;
            _response = response;
            _printerService = printerService;
        }

        #region POST Appointment Booking
        [HttpPost("postAppointmentBooking")]
        public IActionResult PostAppointmentBooking([FromHeader] long? bookingID, [FromBody] AppointmentBookingDto appointment)
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
                    using (SqlCommand cmd = new SqlCommand("SP_Appoinments", conn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@Flag", 100);
                        cmd.Parameters.AddWithValue("@ClientID", tokenClaims.ClientId);
                        cmd.Parameters.AddWithValue("@AppoimentID", (object?)bookingID ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@JsonData", Newtonsoft.Json.JsonConvert.SerializeObject(appointment));
                        cmd.Parameters.AddWithValue("@UserID", tokenClaims.UserId);

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

                _response.isSucess = true;
                _response.message = "Success";
                _response.data = rows;

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

        #region GET Appointment Bookings
        [HttpGet("getAppointmentBookings")]
        public IActionResult GetAppointmentBookings([FromHeader] string? Department, [FromHeader] int? DoctorID, [FromHeader] string? BookingDate)
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
                    using (SqlCommand cmd = new SqlCommand("SP_Appoinments", conn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@Flag", 101);
                        cmd.Parameters.AddWithValue("@ClientID", tokenClaims.ClientId);
                        cmd.Parameters.AddWithValue("@Department", (object?)Department ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@DoctorID", (object?)DoctorID ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@BookingDate", (object?)BookingDate ?? DBNull.Value);

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

                _response.isSucess = true;
                _response.message = "Success";
                _response.data = rows;

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

        #region GET Booking Details
        [HttpGet("getBookingDetails")]
        public IActionResult GetBookingDetails([FromHeader] string? Department, [FromHeader] string BookingDate)
        {
            try
            {
                string token = Request.Headers["Authorization"];
                token = token.Substring(7);

                var tokenClaims = Fluxion_Handler.GetJWTTokenClaims(token, _key._jwtKey, true);

                var parameters = new DynamicParameters();
                parameters.Add("@Flag", 102);
                parameters.Add("@ClientID", tokenClaims.ClientId);
                parameters.Add("@Department", Department);
                parameters.Add("@BookingDate", BookingDate);

                using (var multi = _dbcontext.QueryMultiple("SP_Appoinments", parameters, commandType: CommandType.StoredProcedure))
                {
                    var doctors = multi.Read<Fluxion_Lab.Models.Reception.DoctorWithScheduleDto>().ToList();
                    var bookings = multi.Read<Fluxion_Lab.Models.Reception.BookingDto>().ToList();
                    var rescheduleHeaders = multi.Read<Fluxion_Lab.Models.Reception.RescheduleHeaderDto>().ToList();

                    // Assign bookings and reschedule headers to each doctor by DoctorID
                    foreach (var doctor in doctors)
                    {
                        doctor.Bookings = bookings.Where(b => b.DoctorID == doctor.DoctorID).ToList();
                        doctor.RescheduleHeaders = rescheduleHeaders.Where(r => r.DoctorID == doctor.DoctorID).ToList();
                    }

                    var result = new {
                        Doctors = doctors
                    };

                    _response.isSucess = true;
                    _response.message = "Success";
                    _response.data = result;
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

        #region POST Reschedule Booking
        [HttpPost("rescheduleBooking")]
        public IActionResult RescheduleBooking([FromBody] RescheduleBookingRequest request)
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
                    using (SqlCommand cmd = new SqlCommand("SP_Appoinments", conn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@Flag", 103);
                        cmd.Parameters.AddWithValue("@ClientID", tokenClaims.ClientId);
                        cmd.Parameters.AddWithValue("@DoctorID", request.DoctorID);
                        cmd.Parameters.AddWithValue("@Department", request.Departmet);
                        cmd.Parameters.AddWithValue("@ResheduledFrom", request.ResheduledFrom);
                        cmd.Parameters.AddWithValue("@ResheduledTO", request.ResheduledTO);
                        cmd.Parameters.AddWithValue("@ResheduledFromTime", request.ResheduledFromTime);
                        cmd.Parameters.AddWithValue("@ResheduleToTime", request.ResheduleToTime);
                        cmd.Parameters.AddWithValue("@JsonData", Newtonsoft.Json.JsonConvert.SerializeObject(request.Bookings));
                        cmd.Parameters.AddWithValue("@UserID", tokenClaims.UserId);

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

                _response.isSucess = true;
                _response.message = "Reschedule Success";
                _response.data = rows;
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

        #region GET General Bill Form Data
        [HttpGet("getGeneralBillFormData")]
        public IActionResult GetGeneralBillFormData()
        {
            try
            {

                string token = Request.Headers["Authorization"];
                token = token.Substring(7);
                var tokenClaims = Fluxion_Handler.GetJWTTokenClaims(token, _key._jwtKey, true); 

                var parameters = new DynamicParameters();
                parameters.Add("@Flag", 104);
                parameters.Add("@ClientID", tokenClaims.ClientId);

                using (var multi = _dbcontext.QueryMultiple("SP_Appoinments", parameters, commandType: CommandType.StoredProcedure))
                {
                    var nextNum = multi.Read().FirstOrDefault();
                    var doctors = multi.Read().ToList();
                    var items = multi.Read().ToList();

                    var result = new {
                        NextNum = nextNum != null && nextNum.NextNum != null ? nextNum.NextNum : 0,
                        Doctors = doctors,
                        Items = items
                    };

                    _response.isSucess = true;
                    _response.message = "Success";
                    _response.data = result;
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

        #region POST General Bill Transaction
        [HttpPost("postGeneralBillTransaction")]
        public async Task<IActionResult> PostGeneralBillTransaction(
            [FromHeader] long InvoiceNo,
            [FromHeader] int Sequence,
            [FromHeader] int EditNo,
            [FromHeader] string DocStatus,
            [FromBody] GeneralBillEntryRequest request)
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
                parameters.Add("@JsonData", Newtonsoft.Json.JsonConvert.SerializeObject(request));
                parameters.Add("@UserID", tokenClaims.UserId);
                parameters.Add("@DocStatus", DocStatus);

                var data = await _dbcontext.QueryAsync("SP_GeneralBillingPOST", parameters, commandType: CommandType.StoredProcedure);

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

        #region GET General Bills
        [HttpGet("getGeneralBills")]
        public IActionResult GetGeneralBills(
            [FromHeader] int? sequence,
            [FromHeader] long? invoiceNo,
            [FromHeader] int? editNo,
            [FromHeader] string? groupBy,
            [FromHeader] string? searchKey, [FromHeader] string? fromDate, [FromHeader] string? toDate, [FromHeader] bool? isforward)
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
                    using (SqlCommand cmd = new SqlCommand("SP_Reception", conn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@Flag", 100);
                        cmd.Parameters.AddWithValue("@ClientID", tokenClaims.ClientId);
                        cmd.Parameters.AddWithValue("@Sequence", (object?)sequence ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@InvoiceNo", (object?)invoiceNo ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@EditNo", (object?)editNo ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@GroupBy", (object?)groupBy ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@SearchKey", (object?)searchKey ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@FromDate", (object?)fromDate ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@ToDate", (object?)toDate ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@IsForward", (object?)isforward ?? DBNull.Value);



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

                _response.isSucess = true;
                _response.message = "Success";
                _response.data = rows;
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

        #region GET Pending Bills
        [HttpGet("getPendingBills")]
        public IActionResult GetPendingBills([FromHeader] string? EntryDate, [FromHeader] string? opNumber)
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
                    using (SqlCommand cmd = new SqlCommand("SP_Reception", conn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@Flag", 101);
                        cmd.Parameters.AddWithValue("@ClientID", tokenClaims.ClientId);
                        cmd.Parameters.AddWithValue("@Date", (object?)EntryDate ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@OpNumber", (object?)opNumber ?? DBNull.Value);

                        using (SqlDataAdapter da = new SqlDataAdapter(cmd))
                        {
                            da.Fill(dt);
                        }
                    }
                }

                var pendingBills = new List<PendingBillDto>();
                foreach (DataRow row in dt.Rows)
                {
                    var bill = new PendingBillDto
                    {
                        // Core table columns from trntbl_BalanceDueBills
                        ClientID = Convert.ToInt64(row["ClientID"]),
                        Sequence = Convert.ToInt32(row["Sequence"]),
                        InvoiceNo = Convert.ToInt64(row["InvoiceNo"]),
                        EditNo = Convert.ToInt32(row["EditNo"]),
                        TransType = row["TransType"]?.ToString(),
                        Departmet = row["Departmet"]?.ToString(), // Original spelling from table
                        PatientID = row["PatientID"] == DBNull.Value ? null : Convert.ToInt64(row["PatientID"]),
                        PatientName = row["PatientName"]?.ToString(),
                        EntryDate = Convert.ToDateTime(row["EntryDate"]),
                        BalanceDue = row["BalanceDue"] == DBNull.Value ? null : Convert.ToDecimal(row["BalanceDue"]),
                        TotalAmount = row["TotalAmount"] == DBNull.Value ? null : Convert.ToDecimal(row["TotalAmount"]),
                        DocStatus = row["DocStatus"]?.ToString(),
                        OpNumber = row["OpNumber"] == DBNull.Value ? null : Convert.ToInt64(row["OpNumber"]),
                        
                        // Additional fields (if present in the SP result)
                        MobileNo = dt.Columns.Contains("MobileNo") ? row["MobileNo"]?.ToString() : null,
                        Department = dt.Columns.Contains("Department") ? row["Department"]?.ToString() : null,
                        PaidAmount = dt.Columns.Contains("PaidAmount") ? 
                            (row["PaidAmount"] == DBNull.Value ? 0 : Convert.ToDecimal(row["PaidAmount"])) : 0,
                        
                        // Handle CreatedAt and LastUpdatedAt if they exist in SP result
                        CreatedAt = dt.Columns.Contains("CreatedAt") ? 
                            Convert.ToDateTime(row["CreatedAt"]) : DateTime.MinValue,
                        LastUpdatedAt = dt.Columns.Contains("LastUpdatedAt") && row["LastUpdatedAt"] != DBNull.Value ? 
                            Convert.ToDateTime(row["LastUpdatedAt"]) : null,
                        
                        // Always set ReceiptHistory to an empty list
                        ReceiptHistory = new List<ReceiptHistoryDto>()
                    };

                    pendingBills.Add(bill);
                }

                var grouped = pendingBills
                    .GroupBy(x => x.Departmet ?? x.Department ?? "Unknown") // Handle both spelling variants and null values
                    .Select(g => new PendingBillsGroupedDto
                    {
                        Department = g.Key,
                        PendingBillCount = g.Count(),
                        Bills = g.ToList()
                    })
                    .ToList();

                _response.isSucess = true;
                _response.message = "Success";
                _response.data = grouped;
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

        #region POST Receipt Transaction
        [HttpPost("postReceiptTransaction")]
        public IActionResult PostReceiptTransaction([FromBody] Fluxion_Lab.Models.General.BodyParams.ReceiptEntryRequest request)
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
                    using (SqlCommand cmd = new SqlCommand("SP_Reception", conn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@Flag", 102);
                        cmd.Parameters.AddWithValue("@ClientID", tokenClaims.ClientId);
                        cmd.Parameters.AddWithValue("@UserID", tokenClaims.UserId);
                        cmd.Parameters.AddWithValue("@JsonData", Newtonsoft.Json.JsonConvert.SerializeObject(request));

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

                _response.isSucess = true;
                _response.message = "Receipt Transaction Success";
                _response.data = rows;
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

        #region POST Receipt Cancel
        [HttpPost("cancelReceiptTransaction")]
        public IActionResult CancelReceiptTransaction(
            [FromHeader] int Sequence,
            [FromHeader] long ReceiptNo, [FromHeader] string cancelReason)
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
                    using (SqlCommand cmd = new SqlCommand("SP_Reception", conn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@Flag", 103);
                        cmd.Parameters.AddWithValue("@ClientID", tokenClaims.ClientId);
                        cmd.Parameters.AddWithValue("@UserID", tokenClaims.UserId);
                        cmd.Parameters.AddWithValue("@Sequence", Sequence);
                        cmd.Parameters.AddWithValue("@InvoiceNo", ReceiptNo);
                        cmd.Parameters.AddWithValue("@CancelReason", cancelReason);

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

                _response.isSucess = true;
                _response.message = "Receipt Cancelled Successfully";
                _response.data = rows;
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

        #region POST Update Booking Status
        [HttpPost("updateBookingStatus")]
        public IActionResult UpdateBookingStatus([FromHeader] long BookingID, [FromHeader] string BookingStatus)
        {
            try
            {
                string token = Request.Headers["Authorization"];
                token = token.Substring(7);
                var tokenClaims = Fluxion_Handler.GetJWTTokenClaims(token, _key._jwtKey, true);
                int? flag = 105;

                // Use the connection string from the current _dbcontext if possible
                var sqlConn = _dbcontext as SqlConnection;
                string connStr = sqlConn != null ? sqlConn.ConnectionString : null;
                if (string.IsNullOrEmpty(connStr))
                {
                    throw new Exception("Unable to resolve SQL connection string. Ensure the DB context is a SqlConnection or provide a valid connection string.");
                }

                using (SqlConnection conn = new SqlConnection(connStr))
                {
                    using (SqlCommand cmd = new SqlCommand("SP_Appoinments", conn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@Flag", flag);
                        cmd.Parameters.AddWithValue("@ClientID", tokenClaims.ClientId);
                        cmd.Parameters.AddWithValue("@AppoimentID", BookingID);
                        cmd.Parameters.AddWithValue("@BookingStatus", BookingStatus);

                        conn.Open();
                        cmd.ExecuteNonQuery();
                        conn.Close();
                    }
                }

                _response.isSucess = true;
                _response.message = "Booking status updated successfully";
                _response.data = null;
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

        #region POST Cancel General Bill
        [HttpPost("cancelGeneralBill")]
        public IActionResult CancelGeneralBill(
            [FromHeader] int sequence,
            [FromHeader] long invoiceNo,
            [FromHeader] int editNo)
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
                    using (SqlCommand cmd = new SqlCommand("SP_Reception", conn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@Flag", 104);
                        cmd.Parameters.AddWithValue("@ClientID", tokenClaims.ClientId);
                        cmd.Parameters.AddWithValue("@Sequence", sequence);
                        cmd.Parameters.AddWithValue("@InvoiceNo", invoiceNo);
                        cmd.Parameters.AddWithValue("@EditNo", editNo);

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

                _response.isSucess = true;
                _response.message = "General bill cancelled successfully";
                _response.data = rows;
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

        #region GET Patient Info
        [HttpGet("getPatientInfo")]
        public IActionResult GetPatientInfo([FromHeader] long PatientID)
        {
            try
            {
                string token = Request.Headers["Authorization"];
                token = token.Substring(7);
                var tokenClaims = Fluxion_Handler.GetJWTTokenClaims(token, _key._jwtKey, true);

                var parameters = new DynamicParameters();
                parameters.Add("@Flag", 105);
                parameters.Add("@ClientID", tokenClaims.ClientId);
                parameters.Add("@PatientID", PatientID);

                using (var multi = _dbcontext.QueryMultiple("SP_Reception", parameters, commandType: CommandType.StoredProcedure))
                {
                    var visitSummary = multi.Read().ToList();
                    var recentAppointments = multi.Read().ToList();
                    var dueBillSummary = multi.Read().ToList();
                    var upcomingAppoimet = multi.Read().ToList(); 

                    var result = new {
                        VisitSummary = visitSummary,
                        RecentAppointments = recentAppointments,
                        DueBillSummary = dueBillSummary,
                        upAppoiment = upcomingAppoimet
                    };

                    _response.isSucess = true;
                    _response.message = "Success";
                    _response.data = result;
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

        #region GET Next OP Number
        [HttpGet("getNextOPNo")]
        public IActionResult GetNextONo()
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

                object nextNum = null;
                var doctors = new List<Dictionary<string, object>>();
                var places = new List<Dictionary<string, object>>();

                using (SqlConnection conn = new SqlConnection(connStr))
                {
                    using (SqlCommand cmd = new SqlCommand("SP_Reception", conn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@Flag", 106);
                        cmd.Parameters.AddWithValue("@ClientID", tokenClaims.ClientId);
                        conn.Open();
                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            // First result set: NextNum
                            if (reader.Read())
                            {
                                nextNum = reader["NextNum"];
                            }
                            // Move to second result set: Doctors
                            if (reader.NextResult())
                            {
                                while (reader.Read())
                                {
                                    var dict = new Dictionary<string, object>();
                                    for (int i = 0; i < reader.FieldCount; i++)
                                    {
                                        dict[reader.GetName(i)] = reader.GetValue(i);
                                    }
                                    doctors.Add(dict);
                                }
                            }
                            // Move to third result set: Places
                            if (reader.NextResult())
                            {
                                while (reader.Read())
                                {
                                    var dict = new Dictionary<string, object>();
                                    for (int i = 0; i < reader.FieldCount; i++)
                                    {
                                        dict[reader.GetName(i)] = reader.GetValue(i);
                                    }
                                    places.Add(dict);
                                }
                            }
                        }
                        conn.Close();
                    }
                }

                var result = new {
                    NextNum = nextNum ?? 0,
                    Doctors = doctors,
                    Places = places
                };

                _response.isSucess = true;
                _response.message = "Success";
                _response.data = result;
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

        #region GET Next Available Token
        [HttpGet("getNextAvailableToken")]
        public IActionResult GetNextAvailableToken(
            [FromHeader] long PatientID,
            [FromHeader] int DoctorID,
            [FromHeader] string BookingDate,[FromHeader] bool? isReview, [FromHeader] bool? isReNew)
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
                    using (SqlCommand cmd = new SqlCommand("SP_Appoinments", conn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@Flag", 106);
                        cmd.Parameters.AddWithValue("@ClientID", tokenClaims.ClientId);
                        cmd.Parameters.AddWithValue("@PatientID", PatientID);
                        cmd.Parameters.AddWithValue("@DoctorID", DoctorID);
                        cmd.Parameters.AddWithValue("@BookingDate", BookingDate);
                        cmd.Parameters.AddWithValue("@IsReview", isReview);
                        cmd.Parameters.AddWithValue("@IsReNew", isReNew);

                        using (SqlDataAdapter da = new SqlDataAdapter(cmd))
                        {
                            da.Fill(dt);
                        }
                    }
                }

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

                _response.isSucess = true;
                _response.message = "Success";
                _response.data = rows;
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

        #region POST OP Billing
        [HttpPost("postOPBilling")]
        public IActionResult PostOPBilling(
            [FromHeader] long? InvoiceNo,
            [FromHeader] int? Sequence,
            [FromHeader] int? EditNo,
            [FromHeader] string DocStatus,
            [FromBody] OPBillingEntryRequest request)
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
                parameters.Add("@JsonData", Newtonsoft.Json.JsonConvert.SerializeObject(request));
                parameters.Add("@UserID", tokenClaims.UserId);
                parameters.Add("@DocStatus", DocStatus);

                var data = _dbcontext.Query("SP_OPBillingPOST", parameters, commandType: CommandType.StoredProcedure);

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

        #region GET OP Billing Details
        [HttpGet("getOPBillingDetails")]
        public IActionResult GetOPBillingDetails(
            [FromHeader] int Sequence,
            [FromHeader] long InvoiceNo,
            [FromHeader] int EditNo)
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
                    using (SqlCommand cmd = new SqlCommand("SP_Reception", conn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@Flag", 107);
                        cmd.Parameters.AddWithValue("@ClientID", tokenClaims.ClientId);
                        cmd.Parameters.AddWithValue("@InvoiceNo", InvoiceNo);
 
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

                _response.isSucess = true;
                _response.message = "Success";
                _response.data = rows;
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

        #region GET Appointment Booking Details
        [HttpGet("getAppointmentBookingDetails")]
        public IActionResult GetAppointmentBookingDetails([FromHeader] long BookingID)
        {
            try
            {
                string token = Request.Headers["Authorization"];
                token = token.Substring(7);
                var tokenClaims = Fluxion_Handler.GetJWTTokenClaims(token, _key._jwtKey, true);

                var parameters = new DynamicParameters();
                parameters.Add("@Flag", 108);
                parameters.Add("@ClientID", tokenClaims.ClientId);
                parameters.Add("@BookingID", BookingID);

                var data = _dbcontext.Query("SP_Reception", parameters, commandType: CommandType.StoredProcedure).ToList();

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

        #region POST Cancel OP Billing
        [HttpPost("cancelOPBilling")]
        public IActionResult CancelOPBilling(
            [FromHeader] long InvoiceNo,
            [FromHeader] int Sequence,
            [FromHeader] int EditNo)
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
                    using (SqlCommand cmd = new SqlCommand("SP_OPBillingCancel", conn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@ClientID", tokenClaims.ClientId);
                        cmd.Parameters.AddWithValue("@InvoiceNo", InvoiceNo);
                        cmd.Parameters.AddWithValue("@Sequece", Sequence);
                        cmd.Parameters.AddWithValue("@EditNo", EditNo);
                        cmd.Parameters.AddWithValue("@UserID", tokenClaims.UserId);

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

                _response.isSucess = true;
                _response.message = "OP billing cancelled successfully";
                _response.data = rows;
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


        #region Dotmetrics PRINT
        [HttpGet("printDotMetrics")]
        public IActionResult PrintDotMetrics([FromHeader] int Sequence, [FromHeader] long InvoiceNo, [FromHeader] int EditNo, [FromHeader] string printerName)
        {
            try
            {
                string token = Request.Headers["Authorization"];
                token = token.Substring(7);
                var tokenClaims = Fluxion_Handler.GetJWTTokenClaims(token, _key._jwtKey, true);

                var parameters = new DynamicParameters();
                parameters.Add("@Flag", 109);
                parameters.Add("@ClientID", tokenClaims.ClientId);
                parameters.Add("@Sequence", Sequence);
                parameters.Add("@InvoiceNo", InvoiceNo);
                parameters.Add("@EditNo", EditNo);

                using (var multi = _dbcontext.QueryMultiple("SP_Reception", parameters, commandType: CommandType.StoredProcedure))
                {
                    var generalSettings = multi.Read().ToList();
                    var headerInfo = multi.Read().ToList();
                    var lineItems = multi.Read().ToList();

                    var result = new {
                        GeneralSettings = generalSettings,
                        HeaderInfo = headerInfo,
                        LineItems = lineItems
                    };

                    // Call printer service
                    _printerService.PrintDotMetricsReceipt(result, printerName);

                    _response.isSucess = true;
                    _response.message = "Success & Print job sent";
                    _response.data = result;
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

        #region GET Invoice Payment History
        [HttpGet("getInvoicePaymentHistory")]
        public IActionResult GetInvoicePaymentHistory( [FromHeader] long InvoiceNo, [FromHeader] int Sequence, [FromHeader] string TransType)
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
                    using (SqlCommand cmd = new SqlCommand("SP_Reception", conn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@Flag", 110);
                        cmd.Parameters.AddWithValue("@ClientID", tokenClaims.ClientId);
                        cmd.Parameters.AddWithValue("@InvoiceNo", InvoiceNo);
                        cmd.Parameters.AddWithValue("@Sequence", Sequence);
                        cmd.Parameters.AddWithValue("@TransType", TransType);

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

                _response.isSucess = true;
                _response.message = "Success";
                _response.data = rows;
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