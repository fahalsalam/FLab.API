using Dapper;
using Fluxion_Lab.Classes.DBOperations;
using Fluxion_Lab.Models.General;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using System.Data;
using System.Data.SqlClient;
using static Fluxion_Lab.Models.Reports;

namespace Fluxion_Lab.Controllers.MobileApp
{
    [Route("api/7990")]
   
    public class MobileAppController : ControllerBase
    {
        private readonly JwtKey _key;
        private readonly IDbConnection _dbcontext;
        private readonly IDbConnection _dbcontext1;
        
        private readonly string offlineConnectionString = "Server=localhost;Database=db_Fluxion_Prambil;User Id=FS;Password=Fluxion@FS@987;Encrypt=True;TrustServerCertificate=True;";
        private readonly string onlineConnectionString = "Server=localhost;Database=db_Fluxion_Prod;User Id=FS;Password=Fluxion@FS@987;Encrypt=True;TrustServerCertificate=True;";


        protected APIResponse _response;

        public MobileAppController(IOptions<JwtKey> options, IDbConnection dbcontext, APIResponse response, IConfiguration configuration, 
            IDbConnection dbcontext1)
        {
            this._key = options.Value;
            var connectionString = "Server=localhost;Database=db_Fluxion_Prod;User Id=FS;Password=Fluxion@FS@987;Encrypt=True;TrustServerCertificate=True;";
            var connectionString1 = "Server=localhost;Database=db_Fluxion_MetaDB;User Id=FS;Password=Fluxion@FS@987;Encrypt=True;TrustServerCertificate=True;";
            _dbcontext = new SqlConnection(connectionString);
            _dbcontext1 = new SqlConnection(connectionString1);
            _response = response;
        }

        #region Test Data Sync
        /// <summary>
        /// Triggers the Test Data Summary sync from local → prod.
        /// </summary>
        [HttpPost("sync-test-summary")]
        public async Task<IActionResult> SyncTestDataSummary()
        {
            try
            {
                await SyncTestDataFromLocalMachine();
                return Ok(new { message = "Data sync completed successfully." });
            }
            catch (Exception ex)
            {
                 
                return StatusCode(500, new { error = ex.Message });
            }
        }


        private async Task SyncTestDataFromLocalMachine()
        {
            long clientID = 0;
            string clientName = string.Empty;

            try
            {
                // Get client ID from local
                clientID = await GetClientIDFromLocal();

                using (var offlineConn = new SqlConnection(offlineConnectionString))
                {
                    await offlineConn.OpenAsync();

                    var parameters = new DynamicParameters();
                    parameters.Add("@InvoiceNo", 0L);
                    parameters.Add("@Sequeece", 0L);

                    var multi = await offlineConn.QueryMultipleAsync("SP_ClientTestDataSync", parameters, commandType: CommandType.StoredProcedure);

                    var header = multi.Read<TestEntryHeader>().ToList();
                    var clientInfo = multi.Read<dynamic>()
                                          .Select(x => new { ClientID = (long)x.ClientID, ClientName = (string)x.ClientName })
                                          .FirstOrDefault();
                    var lineItems = multi.Read<TestEntryLine>().ToList();

                    if (clientInfo != null)
                    {
                        clientID = clientInfo.ClientID;
                        clientName = clientInfo.ClientName;
                    }

                    // Push to online
                    using (var onlineConn = new SqlConnection(onlineConnectionString))
                    {
                        await onlineConn.OpenAsync();

                        var p1 = new DynamicParameters();
                        p1.Add("@JsonHeaderData", JsonConvert.SerializeObject(header));
                        p1.Add("@JsonDetailData", JsonConvert.SerializeObject(lineItems));

                        var returned = (await onlineConn.QueryAsync<dynamic>("SP_ClientTestDataSummary", p1, commandType: CommandType.StoredProcedure)).ToList();

                        if (returned.Any())
                        {
                            LogDataSync(onlineConn, clientID, clientName, "Data sync successful", false, true);

                            // Update local flags
                            string json = JsonConvert.SerializeObject(returned);
                            var updateSql = @"
                                DECLARE @json NVARCHAR(MAX) = @JsonData;
                                DECLARE @temp TABLE (Sequence BIGINT, InvoiceNo BIGINT, EditNo BIGINT);

                                INSERT INTO @temp (Sequence, InvoiceNo, EditNo)
                                SELECT Sequence, InvoiceNo, EditNo
                                FROM OPENJSON(@json)
                                WITH (Sequence BIGINT, InvoiceNo BIGINT, EditNo BIGINT);

                                UPDATE H
                                SET H.IsDataSynced = 1
                                FROM trntbl_TestEntriesHdr H
                                INNER JOIN @temp T ON H.Sequence = T.Sequence AND H.InvoiceNo = T.InvoiceNo AND H.EditNo = T.EditNo;

                                UPDATE L
                                SET L.IsDataSynced = 1
                                FROM trntbl_TestEntriesLine L
                                INNER JOIN @temp T ON L.Sequence = T.Sequence AND L.InvoiceNo = T.InvoiceNo AND L.EditNo = T.EditNo;";
                            using (var flagConn = new SqlConnection(offlineConnectionString))
                            {
                                await flagConn.OpenAsync();
                                await flagConn.ExecuteAsync(updateSql, new { JsonData = json }, commandType: CommandType.Text);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                try
                {
                    var decConn = Fluxion_Handler.DecryptString(onlineConnectionString, Fluxion_Handler.APIString);
                    using var conn = new SqlConnection(decConn);
                    await conn.OpenAsync();
                    LogDataSync(conn, clientID, clientName, ex.Message, true, false);
                }
                catch (Exception logEx)
                {
                }

                throw;
            }
        }

        private void LogDataSync(SqlConnection connection, long clientID, string clientName, string message, bool isError, bool isSuccess)
        {
            var p = new DynamicParameters();
            p.Add("@ClientID", clientID);
            p.Add("@ClientName", clientName);
            p.Add("@SyncMessage", message);
            p.Add("@ErrorTime", DateTime.Now);
            p.Add("@IsSyncWithError", isError);
            p.Add("@IsDataSyncSucess", isSuccess);

            var sql = @"
                INSERT INTO DataSyncLogs (ClientID, ClientName, SyncMessage, ErrorTime, IsSyncWithError, IsDataSyncSucess)
                VALUES (@ClientID, @ClientName, @SyncMessage, @ErrorTime, @IsSyncWithError, @IsDataSyncSucess);";

            connection.Execute(sql, p);
        }

        private async Task<long> GetClientIDFromLocal()
        {
            using (var conn = new SqlConnection(offlineConnectionString))
            {
                await conn.OpenAsync();
                var result = await conn.QueryFirstOrDefaultAsync<GetClientID>("SELECT ClientID FROM mtbl_ClientMaster");
                return result?.ClientID ?? 0;
            }
        }

        // DTOs – populate properties to match your SP output
        public class TestEntryHeader {

            public long? ClientID { get; set; }
            public string? ClientName { get; set; }
            public long? PatientID { get; set; }
            public string? PatientName { get; set; }
            public int? Age { get; set; }
            public string? MobileNo { get; set; }
            public string? DOB { get; set; }
            public string? EntryDate { get; set; }
            public long? InvoiceNo { get; set; }
            public int? Sequence { get; set; }
            public string? SequenceName { get; set; }
            public long? EditNo { get; set; }
            public decimal? GrandTotal { get; set; }
            public string? ResultStatus { get; set; }
            public string? PaymentStatus { get; set; }
            public string? LastModified { get; set; }
            public string? HeaderImageUrl { get; set; }
            public string? FooterImageUrl { get; set; }
            public string? LineJsonData { get; set; }
            public string? Gender { get; set; }
            public string? DrName { get; set; }
            public DateTime? CreatedDateTime { get; set; }
            public string? ResultApprovedBy { get; set; }
            public DateTime? ResultApprovedDateTime { get; set; }
            public string? ResultVerifiedBy { get; set; }
            public string? ResultApproveSign { get; set; }
            public string? LabName { get; set; }
            public decimal? BalanceDue { get; set; }
            public decimal? DiscAmount { get; set; }

        }
        public class TestEntryLine {

            public int? Sequence { get; set; }
            public long? InvoiceNo { get; set; }
            public long? EditNo { get; set; }
            public int? SI_No { get; set; }
            public int? ID { get; set; }
            public string? Name { get; set; }
            public string? Type { get; set; }
            public string? LineStatus { get; set; }
            public long? ClientID { get; set; }

        }

        public class GetClientID
        {
            public long? ClientID { get; set; }
        }


        #endregion

        #region Get Mobile No Verification
        [AllowAnonymous]
        [HttpGet("mobileNoVerification")]
        public IActionResult mobileNoVerification([FromHeader] string MobileNo)
        {
            try
            { 
                var parameters = new DynamicParameters();
                parameters.Add("@flag", 100);
                parameters.Add("@MobileNo", MobileNo);  

                var data = _dbcontext.Query("SP_MobileApp", parameters, commandType: CommandType.StoredProcedure); 

                _response.isSucess = true;
                _response.message = "Success";
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

        #region Patient Data By Mobile No
        [AllowAnonymous]
        [HttpGet("getPatientDataByMobileNo")]
        public IActionResult getPatientDataByMobileNo([FromHeader] string MobileNo)
        {
            try
            { 

                var parameters = new DynamicParameters();
                parameters.Add("@flag", 101);
                parameters.Add("@MobileNo", MobileNo);

                var data = _dbcontext.QueryMultiple("SP_MobileApp", parameters, commandType: CommandType.StoredProcedure);
               
                var _patients = data.Read<dynamic>().ToList();
                
                var _recentTransaction = data.Read<dynamic>().ToList();

                if (_patients.Count > 0)
                {

                    var Response = new
                    {
                        isSucess = true,
                        message = "Success",
                        data = new
                        {
                            patients = _patients,
                            transactions = _recentTransaction,
                        }
                    };
                    return Ok(Response);
                }
                else
                {
                    _response.isSucess = false;
                    _response.message = "No Data Found";
                    return NotFound(_response);
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

        #region Patient Data by PatientID
        [AllowAnonymous]
        [HttpGet("getPatientDataByPatientID")]
        public IActionResult getPatientDataByPatientID([FromHeader] string MobileNo, [FromHeader] long PatientID)
        {
            try
            {  
                var parameters = new DynamicParameters();
                parameters.Add("@flag", 102);
                parameters.Add("@MobileNo", MobileNo);
                parameters.Add("@PatientID", PatientID);


                var data = _dbcontext.Query("SP_MobileApp", parameters, commandType: CommandType.StoredProcedure);

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

        #region MobileApp Client Config
        [AllowAnonymous]
        [HttpPost("ManageClientConfig")]
        public async Task<IActionResult> ManageClientMobileAppConfig([FromBody] ClientMobileAppConfigRequest request)
        {
            try
            {
                var parameters = new DynamicParameters();
                parameters.Add("@Action", request.Action);
                parameters.Add("@ClientID", request.ClientID);
                parameters.Add("@HeaderImagerUrl", request.HeaderImagerUrl);
                parameters.Add("@FooterImagerUrl", request.FooterImagerUrl);

                if (request.Action == "GET")
                {
                    var result = await _dbcontext.QueryAsync<ClientMobileAppConfig>("SP_ClientMobileAppConfig_CRUD", parameters, commandType: CommandType.StoredProcedure);
                    return Ok(result);
                }
                else
                {
                    var result = await _dbcontext.ExecuteAsync("SP_ClientMobileAppConfig_CRUD", parameters, commandType: CommandType.StoredProcedure);
                    return Ok(new { Message = "Operation successful", AffectedRows = result });
                }
            }
            catch (Exception ex)
            { 
               return StatusCode(500, new { Message = ex.Message });
            } 
        }
        #endregion

        /**************** DashBoard App Controller ***********/

        #region DashBoard App Login
        [HttpPost("DashBoardAppLogin")]
        public IActionResult getDashBoardAppLogin([FromHeader] string MobileNo)
        {
            try
            {
                // First, check if this is a stakeholder login
                var stakeholder = _dbcontext1.QueryFirstOrDefault<dynamic>(
                    @"SELECT StakeholderID, FullName 
                      FROM mtbl_Stakeholder 
                      WHERE MobileNo = @MobileNo",
                    new { MobileNo });

                if (stakeholder != null)
                {
                    // This is a stakeholder login
                    var associatedTenants = _dbcontext1.Query<dynamic>(
                        @"SELECT 
                    t.ClientID, 
                    t.ClientName AS TenantName, 
                    t.IsDefaultOrg,
                    stm.Role
                  FROM 
                    mtbl_Stakeholder_Tenant_Mapping stm
                  INNER JOIN 
                    mtbl_Tenant_Master t ON stm.ClientID = t.ClientID
                  WHERE 
                    stm.StakeholderID = @StakeholderID
                    AND stm.IsActive = 1",
                        new { StakeholderID = stakeholder.StakeholderID });

                    // For each associated tenant, also get its child organizations
                    var allTenants = new List<dynamic>();
                    foreach (var tenant in associatedTenants)
                    {
                        // Add the parent tenant
                        allTenants.Add(tenant);

                        // Add child tenants with same role
                        var childTenants = _dbcontext1.Query<dynamic>(
                            @"SELECT 
                        t.ClientID, 
                        t.ClientName AS TenantName, 
                        t.IsDefaultOrg,
                        @Role as Role
                      FROM 
                        mtbl_Tenant_Master t
                      WHERE 
                        t.ParentID = @ParentID",
                            new { ParentID = tenant.ClientID, Role = tenant.Role });

                        allTenants.AddRange(childTenants);
                    }

                    _response.isSucess = true;
                    _response.message = "Success";
                    _response.data = new
                    {
                        stakeholderInfo = stakeholder,
                        tenants = allTenants
                    };
                    return Ok(_response);
                }
                else
                {
                    // If not a stakeholder, try the original tenant login approach
                    var rootClient = _dbcontext1.QueryFirstOrDefault<dynamic>(
                        @"SELECT TOP 1 Root.ClientID
                  FROM mtbl_Tenant_Master AS Child
                  INNER JOIN mtbl_Tenant_Master AS Root ON 
                      (Root.ClientID = Child.ClientID AND Root.ParentID IS NULL)
                      OR (Root.ClientID = Child.ParentID AND Root.ParentID IS NULL)
                  WHERE Child.MobileNo = @MobileNo",
                        new { MobileNo });

                    if (rootClient == null)
                    {
                        _response.isSucess = false;
                        _response.message = "Mobile number not registered.";
                        return Ok(_response);
                    }

                    long rootClientId = rootClient.ClientID;

                    // Get both parent (root) and its children
                    var tenants = _dbcontext1.Query<dynamic>(
                        @"SELECT 
                      ClientID, 
                      ClientName AS TenantName, 
                      IsDefaultOrg,
                      'Owner' as Role  -- Default role for tenant owner
                  FROM
                      mtbl_Tenant_Master 
                  WHERE 
                      ClientID = @RootClientID OR ParentID = @RootClientID",
                        new { RootClientID = rootClientId });

                    _response.isSucess = true;
                    _response.message = "Success";
                    _response.data = tenants;
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

        #region Transaction Summary 
        [HttpGet("getTransactionSummary")]
        public IActionResult getTransactionSummary([FromHeader] int? clientID)
        {
            try
            {
                 
                var parameters = new DynamicParameters();

                parameters.Add("@Flag", 104);
                parameters.Add("@ClientID", clientID);

                var data = _dbcontext.QueryMultiple("SP_MobileApp", parameters, commandType: CommandType.StoredProcedure);

                
                var _todaySummary = data.Read<dynamic>().ToList();
                var _salesSummary = data.Read<dynamic>().ToList(); 

                var Response = new
                {
                    isSucess = true,
                    message = "Success",
                    data = new
                    {
                        salesSummary = _salesSummary, 
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
        public IActionResult getTransactionSummarybyGraph([FromHeader] string graphType, [FromHeader] int? clientID)
        {
            try
            {
                 
                var parameters = new DynamicParameters();

                parameters.Add("@Flag", 105);
                parameters.Add("@ClientID", clientID);
                parameters.Add("@GraphType", graphType);

                var data = _dbcontext.Query("SP_MobileApp", parameters, commandType: CommandType.StoredProcedure);

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

        #region Sales Report Page Load
        [HttpGet("getMobileAppsalesReportDataSets")]
        public IActionResult getsalesReportDataSets([FromHeader] long? clientID)
        {
            try
            {
                 var parameters = new DynamicParameters();
                parameters.Add("@Flag", 106);
                parameters.Add("@ClientID", clientID);

                var data = _dbcontext.QueryMultiple("SP_MobileApp", parameters, commandType: CommandType.StoredProcedure);

                var _testMaster = data.Read<dynamic>().ToList();
                var _testGroup = data.Read<dynamic>().ToList();
                var _doctorMaster = data.Read<dynamic>().ToList();
                var _labMaster = data.Read<dynamic>().ToList();
               

                var Response = new
                {
                    isSucess = true,
                    message = "Success",
                    data = new
                    {
                        testMaster = _testMaster,
                        testGroupMaster = _testGroup,
                        doctorMaster = _doctorMaster,
                        labMaster = _labMaster 
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

        #region Sales Report
        [HttpPost("getMobileAppSalesReport")]
        public IActionResult getSalesReport([FromHeader] long? clientID, [FromBody] salesMobileReports _rp)
        {
            try
            {

                int? _flag = 100;
                if (_rp.groupby == "Bill Wise")
                {
                    _flag = 107;
                }
                else if (_rp.groupby == "Test Wise")
                {
                    _flag = 108;
                }
                else if (_rp.groupby == "Group Wise")
                {
                    _flag = 109;
                }
                else if (_rp.groupby == "Doctor Wise")
                {
                    _flag = 110;
                }
                else if (_rp.groupby == "Lab Wise")
                {
                    _flag = 111;
                } 


                var parameters = new DynamicParameters();
                parameters.Add("@Flag", _flag);
                parameters.Add("@ClientID", clientID);
                //parameters.Add("@PageNo", _rp.pageNo);
                //parameters.Add("@PageSize", _rp.pageSize);
                parameters.Add("@FromDate", _rp.fromDate);
                parameters.Add("@ToDate", _rp.toDate);
                parameters.Add("@SearchKey", _rp.searchKey);

                var data = _dbcontext.Query("SP_MobileApp", parameters, commandType: CommandType.StoredProcedure);

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





        public class ClientMobileAppConfigRequest
        {
            public string Action { get; set; }  // "GET", "POST", "PUT"
            public int? ClientID { get; set; }
            public string HeaderImagerUrl { get; set; }
            public string FooterImagerUrl { get; set; }
        } 
        public class ClientMobileAppConfig
        {
            public int ClientID { get; set; }
            public string HeaderImagerUrl { get; set; }
            public string FooterImagerUrl { get; set; }
        }
    }
}
