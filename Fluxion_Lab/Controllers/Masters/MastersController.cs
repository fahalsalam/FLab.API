using Dapper;
using Fluxion_Lab.Classes.DBOperations;
using Fluxion_Lab.Models.General;
using Fluxion_Lab.Models.Masters.DoctorMaster;
using Fluxion_Lab.Models.Masters.GeneralSettings;
using Fluxion_Lab.Models.Masters.ItemMaster;
using Fluxion_Lab.Models.Masters.Manufacture;
using Fluxion_Lab.Models.Masters.SupplierMaster;
using Fluxion_Lab.Models.Masters.TestGroupMaster;
using Fluxion_Lab.Models.Masters.TestMaster;
using Fluxion_Lab.Models.Masters.UserMaster;
using Fluxion_Lab.Services.Masters;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using System.Data;
using System.Data.Common;
using System.Reflection;
using System.Text.Json.Serialization;
using static Fluxion_Lab.Controllers.Authentication.AuthenticationController;
using static Fluxion_Lab.Controllers.Transactions.TransactionsController;
using static Fluxion_Lab.Models.General.BodyParams;
using static Fluxion_Lab.Models.MachineConfig.MachineConfig;
using static Fluxion_Lab.Models.Masters.DoctorMaster.DoctorMasterWithSchedule;
using static Fluxion_Lab.Models.Masters.Machine_Analyzer.MachineAnalyzer;
using static Fluxion_Lab.Models.Masters.PatientMaster.Patients;
using static Fluxion_Lab.Models.Masters.PrivilegeCards.PrivilageCards;
using static Fluxion_Lab.Models.Masters.TestGroupMaster.TestGroupMaster;
using static Fluxion_Lab.Models.RBACL.RBACL;
using static Fluxion_Lab.Models.Transactions.TestEntries.TestEntries;

namespace Fluxion_Lab.Controllers.Masters
{
    [Route("api/0202")]
    public class MastersController : ControllerBase
    {
        private readonly JwtKey _key;
        private readonly IDbConnection _dbcontext;
        protected APIResponse _response;

        public MastersController(IOptions<JwtKey> options, IDbConnection dbcontext, APIResponse response)
        {
            this._key = options.Value;
            _dbcontext = dbcontext;
            _response = response;
        }

        /************* Section Master ***************/

        #region Section Master Post
        [HttpPost("postSectionMaster")]
        public IActionResult PostSectionMaster([FromHeader] string Name, [FromHeader] string? Description)
        {
            try
            {
                string token = Request.Headers["Authorization"];
                token = token.Substring(7);

                var tokenClaims = Fluxion_Handler.GetJWTTokenClaims(token, _key._jwtKey, true);

                var parameters = new DynamicParameters();
                parameters.Add("@Flag", 100);
                parameters.Add("@ClientID", tokenClaims.ClientId);
                parameters.Add("@Name", Name);
                parameters.Add("@Description", Description);
                parameters.Add("@UserID", tokenClaims.UserId);

                var data = _dbcontext.Query("SP_SectionMasters", parameters, commandType: CommandType.StoredProcedure);

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

        #region Section Master Update
        [HttpPost("putSectionMaster")]
        public IActionResult PUTSectionMaster([FromHeader] long ID, [FromHeader] string Name, [FromHeader] string Description)
        {
            try
            {
                string token = Request.Headers["Authorization"];
                token = token.Substring(7);

                var tokenClaims = Fluxion_Handler.GetJWTTokenClaims(token, _key._jwtKey, true);

                var parameters = new DynamicParameters();
                parameters.Add("@Flag", 101);
                parameters.Add("@ClientID", tokenClaims.ClientId);
                parameters.Add("@ID", ID);
                parameters.Add("@Name", Name);
                parameters.Add("@Description", Description);
                parameters.Add("@UserID", tokenClaims.UserId);

                var data = _dbcontext.Query("SP_SectionMasters", parameters, commandType: CommandType.StoredProcedure);

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

        #region Section Master Status Update
        [HttpPost("putSectionMasterStatus")]
        public IActionResult PUTSectionMasterStatusUpdate([FromHeader] long ID, [FromHeader] bool IsActive)
        {
            try
            {
                string token = Request.Headers["Authorization"];
                token = token.Substring(7);

                var tokenClaims = Fluxion_Handler.GetJWTTokenClaims(token, _key._jwtKey, true);

                var parameters = new DynamicParameters();
                parameters.Add("@Flag", 102);
                parameters.Add("@ClientID", tokenClaims.ClientId);
                parameters.Add("@ID", ID);
                parameters.Add("@IsActive", IsActive);
                parameters.Add("@UserID", tokenClaims.UserId);

                var data = _dbcontext.Query("SP_SectionMasters", parameters, commandType: CommandType.StoredProcedure);

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

        #region Get Section Master With Pagination
        [HttpGet("getSectionMaster")]
        public IActionResult GetSectionMaster([FromHeader] int PageNo, [FromHeader] int PageSize, [FromHeader] string? FilterText)
        {
            try
            {
                string token = Request.Headers["Authorization"];
                token = token.Substring(7);

                var tokenClaims = Fluxion_Handler.GetJWTTokenClaims(token, _key._jwtKey, true);

                var parameters = new DynamicParameters();
                parameters.Add("@Flag", 103);
                parameters.Add("@ClientID", tokenClaims.ClientId);
                parameters.Add("@PageNo", PageNo);
                parameters.Add("@PageSize", PageSize);
                parameters.Add("@FilterText", string.IsNullOrEmpty(FilterText) ? "" : FilterText);

                var data = _dbcontext.QueryMultiple("SP_SectionMasters", parameters, commandType: CommandType.StoredProcedure);
                var _sectionData = data.Read<dynamic>().ToList();
                var _pageContex = data.Read<dynamic>().ToList();

                if (_sectionData.Count > 0)
                {

                    var Response = new
                    {
                        isSucess = true,
                        message = "Success",
                        data = new
                        {
                            masterData = _sectionData,
                            pageContex = _pageContex,
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

        /************* Test Master ***************/

        #region Test Master Post
        [HttpPost("postTestMaster")]
        public IActionResult PostTestMaster([FromBody] TestMaster _test)
        {
            try
            {
                string token = Request.Headers["Authorization"];
                token = token.Substring(7);

                var tokenClaims = Fluxion_Handler.GetJWTTokenClaims(token, _key._jwtKey, true);

                var parameters = new DynamicParameters();
                parameters.Add("@Flag", 100);
                parameters.Add("@ClientID", tokenClaims.ClientId);
                parameters.Add("@JSONDATA", JsonConvert.SerializeObject(_test));
                parameters.Add("@UserID", tokenClaims.UserId);

                var data = _dbcontext.Query("SP_TestMaster", parameters, commandType: CommandType.StoredProcedure);

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

        #region Test Master Update
        [HttpPost("putTestMaster")]
        public IActionResult PuTTestMaster([FromHeader] long ID, [FromBody] TestMaster _test)
        {
            try
            {
                string token = Request.Headers["Authorization"];
                token = token.Substring(7);

                var tokenClaims = Fluxion_Handler.GetJWTTokenClaims(token, _key._jwtKey, true);

                var parameters = new DynamicParameters();
                parameters.Add("@Flag", 101);
                parameters.Add("@ClientID", tokenClaims.ClientId);
                parameters.Add("@ID", ID);
                parameters.Add("@JSONDATA", JsonConvert.SerializeObject(_test));
                parameters.Add("@UserID", tokenClaims.UserId);

                var data = _dbcontext.Query("SP_TestMaster", parameters, commandType: CommandType.StoredProcedure);

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

        #region Test Master Status Update
        [HttpPost("putTestMasterStatus")]
        public IActionResult PutTestMasterStatusUpdate([FromHeader] long ID, [FromHeader] bool IsActive)
        {
            try
            {
                string token = Request.Headers["Authorization"];
                token = token.Substring(7);

                var tokenClaims = Fluxion_Handler.GetJWTTokenClaims(token, _key._jwtKey, true);

                var parameters = new DynamicParameters();
                parameters.Add("@Flag", 102);
                parameters.Add("@ClientID", tokenClaims.ClientId);
                parameters.Add("@ID", ID);
                parameters.Add("@IsActive", IsActive);
                parameters.Add("@UserID", tokenClaims.UserId);

                var data = _dbcontext.Query("SP_TestMaster", parameters, commandType: CommandType.StoredProcedure);

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

        #region Get Section Master With Pagination
        [HttpGet("getTestMaster")]
        public IActionResult GetTestMaster([FromHeader] int PageNo, [FromHeader] int PageSize, [FromHeader] string? FilterText)
        {
            try
            {
                string token = Request.Headers["Authorization"];
                token = token.Substring(7);

                var tokenClaims = Fluxion_Handler.GetJWTTokenClaims(token, _key._jwtKey, true);

                var parameters = new DynamicParameters();
                parameters.Add("@Flag", 103);
                parameters.Add("@ClientID", tokenClaims.ClientId);
                parameters.Add("@PageNo", PageNo);
                parameters.Add("@PageSize", PageSize);
                parameters.Add("@FilterText", string.IsNullOrEmpty(FilterText) ? "" : FilterText);

                var data = _dbcontext.QueryMultiple("SP_TestMaster", parameters, commandType: CommandType.StoredProcedure);
                var _sectionData = data.Read<dynamic>().ToList();
                var _pageContex = data.Read<dynamic>().ToList();

                var Response = new
                {
                    isSucess = true,
                    message = "Success",
                    data = new
                    {
                        masterData = _sectionData,
                        pageContex = _pageContex,
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

        #region  Test Master Delete
        [HttpPost("deleteTestMaster")]
        public IActionResult DeleteTestMaster([FromHeader] long ID)
        {
            try
            {
                string token = Request.Headers["Authorization"];
                token = token.Substring(7);

                var tokenClaims = Fluxion_Handler.GetJWTTokenClaims(token, _key._jwtKey, true);

                var parameters = new DynamicParameters();
                parameters.Add("@Flag", 104);
                parameters.Add("@ClientID", tokenClaims.ClientId);
                parameters.Add("@ID", ID);
                parameters.Add("@UserID", tokenClaims.UserId);

                bool result = _dbcontext.ExecuteScalar<bool>("SP_TestMaster", parameters, commandType: CommandType.StoredProcedure);

                if (result)
                {
                    var parameters1 = new DynamicParameters();
                    parameters1.Add("@Flag", 105);
                    parameters1.Add("@ClientID", tokenClaims.ClientId);
                    parameters1.Add("@ID", ID);
                    parameters1.Add("@UserID", tokenClaims.UserId);

                    var _data = _dbcontext.ExecuteScalar<bool>("SP_TestMaster", parameters1, commandType: CommandType.StoredProcedure);

                    _response.isSucess = true;
                    _response.message = "Success";
                    _response.data = _data;
                }
                else
                {
                    _response.isSucess = false;
                    _response.message = "Someting Went Wrong ..";
                    _response.data = null;
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

        /******* Test Group Master *****************/

        #region Test Group Master Post
        [HttpPost("postTestGroupMaster")]
        public IActionResult PostTestGroupMaster([FromBody] TestGroups _test)
        {
            try
            {
                string token = Request.Headers["Authorization"];
                token = token.Substring(7);

                var tokenClaims = Fluxion_Handler.GetJWTTokenClaims(token, _key._jwtKey, true);

                var parameters = new DynamicParameters();
                parameters.Add("@Flag", 100);
                parameters.Add("@ClientID", tokenClaims.ClientId);
                parameters.Add("@JSONDATA", JsonConvert.SerializeObject(_test));
                parameters.Add("@UserID", tokenClaims.UserId);

                var data = _dbcontext.Query("SP_TestGroupMaster", parameters, commandType: CommandType.StoredProcedure);

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

        #region Test Group Master Update
        [HttpPost("putTestGroupMaster")]
        public IActionResult PutTestGroupMaster([FromHeader] long ID, [FromBody] TestGroups _test)
        {
            try
            {
                string token = Request.Headers["Authorization"];
                token = token.Substring(7);

                var tokenClaims = Fluxion_Handler.GetJWTTokenClaims(token, _key._jwtKey, true);

                var parameters = new DynamicParameters();
                parameters.Add("@Flag", 101);
                parameters.Add("@ClientID", tokenClaims.ClientId);
                parameters.Add("@ID", ID);
                parameters.Add("@JSONDATA", JsonConvert.SerializeObject(_test));
                parameters.Add("@UserID", tokenClaims.UserId);

                var data = _dbcontext.Query("SP_TestGroupMaster", parameters, commandType: CommandType.StoredProcedure);

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

        #region Test Group Master Status Update
        [HttpPost("putTestGroupMasterStatus")]
        public IActionResult PutTestGroupMasterStatus([FromHeader] long ID, [FromHeader] bool IsActive)
        {
            try
            {
                string token = Request.Headers["Authorization"];
                token = token.Substring(7);

                var tokenClaims = Fluxion_Handler.GetJWTTokenClaims(token, _key._jwtKey, true);

                var parameters = new DynamicParameters();
                parameters.Add("@Flag", 102);
                parameters.Add("@ClientID", tokenClaims.ClientId);
                parameters.Add("@ID", ID);
                parameters.Add("@IsActive", IsActive);
                parameters.Add("@UserID", tokenClaims.UserId);

                var data = _dbcontext.Query("SP_TestGroupMaster", parameters, commandType: CommandType.StoredProcedure);

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

        #region Get Test Group Master With Pagination
        [HttpGet("getTestGroupMaster")]
        public IActionResult GetTestGroupMaster([FromHeader] int PageNo, [FromHeader] int PageSize, [FromHeader] string? FilterText)
        {
            try
            {
                string token = Request.Headers["Authorization"];
                token = token.Substring(7);

                var tokenClaims = Fluxion_Handler.GetJWTTokenClaims(token, _key._jwtKey, true);

                var parameters = new DynamicParameters();
                parameters.Add("@Flag", 103);
                parameters.Add("@ClientID", tokenClaims.ClientId);
                parameters.Add("@PageNo", PageNo);
                parameters.Add("@PageSize", PageSize);
                parameters.Add("@FilterText", string.IsNullOrEmpty(FilterText) ? "" : FilterText);

                var data = _dbcontext.QueryMultiple("SP_TestGroupMaster", parameters, commandType: CommandType.StoredProcedure);
                var _header = data.Read<dynamic>().ToList();
                var _child = data.Read<dynamic>().ToList();
                var _pageContex = data.Read<dynamic>().ToList();


                var groupChilds = _header.Select(hd => new
                {
                    GroupID = hd.GroupID,
                    GroupName = hd.GroupName,
                    Section = hd.Section,
                    GroupCode = hd.GroupCode,
                    Rate = hd.Rate,
                    MachineName = hd.MachineName,
                    Show_In_Report = hd.Show_In_Report,
                    Discount = hd.Discount,
                    AltTestName = hd.AltTestName,
                    AvgDays = hd.AvgDays,
                    AvgHour = hd.AvgHour,
                    AvgMinute = hd.AvgMinute,
                    Tests = _child
                                .Where(childs => childs.GroupID == hd.GroupID)
                                .Select(childs => new
                                {
                                    GroupID = childs.GroupID,
                                    TestID = childs.TestID,
                                    TestName = childs.TestName,
                                    Rate = childs.Rate,
                                    Section = childs.Section,
                                    Unit = childs.Unit,
                                    NormalRange = childs.NormalRange,
                                    type = childs.Type,
                                    SortOrder = childs.SortOrder,
                                }).OrderBy(child => child.SortOrder).ToList()
                }).ToList();

                var Response = new
                {
                    isSucess = true,
                    message = "Success",
                    data = new
                    {
                        groups = groupChilds,
                        pageContex = _pageContex,
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

        #region  Test Group Master Delete
        [HttpPost("deleteTestGroupMaster")]
        public IActionResult DeleteTestGroupMaster([FromHeader] long ID)
        {
            try
            {
                string token = Request.Headers["Authorization"];
                token = token.Substring(7);

                var tokenClaims = Fluxion_Handler.GetJWTTokenClaims(token, _key._jwtKey, true);

                var parameters = new DynamicParameters();
                parameters.Add("@Flag", 104);
                parameters.Add("@ClientID", tokenClaims.ClientId);
                parameters.Add("@ID", ID);
                parameters.Add("@UserID", tokenClaims.UserId);

                bool result = _dbcontext.ExecuteScalar<bool>("SP_TestGroupMaster", parameters, commandType: CommandType.StoredProcedure);

                if (result)
                {
                    var parameters1 = new DynamicParameters();
                    parameters1.Add("@Flag", 105);
                    parameters1.Add("@ClientID", tokenClaims.ClientId);
                    parameters1.Add("@ID", ID);
                    parameters1.Add("@UserID", tokenClaims.UserId);

                    var _data = _dbcontext.ExecuteScalar<bool>("SP_TestGroupMaster", parameters1, commandType: CommandType.StoredProcedure);

                    _response.isSucess = true;
                    _response.message = "Success";
                    _response.data = _data;
                }
                else
                {
                    _response.isSucess = false;
                    _response.message = "Someting Went Wrong ..";
                    _response.data = null;
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


        /********* Item Master ************/

        #region Item Master POST
        [HttpPost("postItemMaster")]
        public IActionResult PostItemMaster([FromBody] ItemMaster _item)
        {
            try
            {
                string token = Request.Headers["Authorization"];
                token = token.Substring(7);

                var tokenClaims = Fluxion_Handler.GetJWTTokenClaims(token, _key._jwtKey, true);

                var parameters = new DynamicParameters();
                parameters.Add("@Flag", 100);
                parameters.Add("@ClientID", tokenClaims.ClientId);
                parameters.Add("@JsonData", JsonConvert.SerializeObject(_item));
                parameters.Add("@UserID", tokenClaims.UserId);

                var data = _dbcontext.Query("SP_ItemMaster", parameters, commandType: CommandType.StoredProcedure);

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

        #region Item Master Update
        [HttpPost("putItemMaster")]
        public IActionResult PutItemMaster([FromHeader] long ItemNo, [FromBody] ItemMaster _item)
        {
            try
            {
                string token = Request.Headers["Authorization"];
                token = token.Substring(7);

                var tokenClaims = Fluxion_Handler.GetJWTTokenClaims(token, _key._jwtKey, true);

                var parameters = new DynamicParameters();
                parameters.Add("@Flag", 101);
                parameters.Add("@ClientID", tokenClaims.ClientId);
                parameters.Add("@ItemNo", ItemNo);
                parameters.Add("@JsonData", JsonConvert.SerializeObject(_item));
                parameters.Add("@UserID", tokenClaims.UserId);

                var data = _dbcontext.Query("SP_ItemMaster", parameters, commandType: CommandType.StoredProcedure);

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

        #region Get Item Master 
        [HttpGet("getItemMaster")]
        public IActionResult GetItemMaster([FromHeader] int PageNo, [FromHeader] int PageSize
            ,[FromHeader] string? FilterText, [FromHeader] string? FilterColumn, [FromHeader] string? FilterValue)
        {
            try
            {
                string token = Request.Headers["Authorization"];
                token = token.Substring(7);

                var tokenClaims = Fluxion_Handler.GetJWTTokenClaims(token, _key._jwtKey, true);

                var parameters = new DynamicParameters();
                parameters.Add("@Flag", 102);
                parameters.Add("@ClientID", tokenClaims.ClientId);
                parameters.Add("@PageNo", PageNo);
                parameters.Add("@PageSize", PageSize);
                parameters.Add("@FilterColumn", FilterColumn);
                parameters.Add("@FilterValue", FilterValue);

                parameters.Add("@FilterText", string.IsNullOrEmpty(FilterText) ? "" : FilterText);

                var data = _dbcontext.QueryMultiple("SP_ItemMaster", parameters, commandType: CommandType.StoredProcedure);
                var _sectionData = data.Read<dynamic>().ToList();
                var _pageContex = data.Read<dynamic>().ToList();

                var Response = new
                {
                    isSucess = true,
                    message = "Success",
                    data = new
                    {
                        masterData = _sectionData,
                        pageContex = _pageContex,
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

        #region  Test Master Delete
        [HttpPost("deleteItemMaster")]
        public IActionResult DeleteItemMaster([FromHeader] long ItemNo, [FromHeader] bool IsActive)
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
                parameters.Add("@IsActive", ItemNo);

                parameters.Add("@UserID", tokenClaims.UserId);

                bool result = _dbcontext.ExecuteScalar<bool>("SP_ItemMaster", parameters, commandType: CommandType.StoredProcedure);

                if (result)
                {
                    var parameters1 = new DynamicParameters();
                    parameters1.Add("@Flag", 104);
                    parameters1.Add("@ClientID", tokenClaims.ClientId);
                    parameters1.Add("@ItemNo", ItemNo);
                    parameters.Add("@IsActive", IsActive);
                    parameters1.Add("@UserID", tokenClaims.UserId);

                    var _data = _dbcontext.ExecuteScalar<bool>("SP_ItemMaster", parameters1, commandType: CommandType.StoredProcedure);

                    _response.isSucess = true;
                    _response.message = "Success";
                    _response.data = _data;
                }
                else
                {
                    _response.isSucess = false;
                    _response.message = "Someting Went Wrong ..";
                    _response.data = null;
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

        #region  Test Master Delete
        [HttpPost("InactiveItemMaster")]
        public IActionResult InactiveItemMaster([FromHeader] long ItemNo, [FromHeader] bool IsActive)
        {
            try
            {
                string token = Request.Headers["Authorization"];
                token = token.Substring(7);

                var tokenClaims = Fluxion_Handler.GetJWTTokenClaims(token, _key._jwtKey, true); 
               
                var parameters1 = new DynamicParameters();
                parameters1.Add("@Flag", 105);
                parameters1.Add("@ClientID", tokenClaims.ClientId);
                parameters1.Add("@ItemNo", ItemNo);
                parameters1.Add("@IsActive", IsActive);
                parameters1.Add("@UserID", tokenClaims.UserId);

                var _data = _dbcontext.ExecuteScalar<bool>("SP_ItemMaster", parameters1, commandType: CommandType.StoredProcedure);

                _response.isSucess = true;
                _response.message = "Success";
                _response.data = _data; 

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

        /*********** Supplier Master *********/

        #region Supplier Master POST
        [HttpPost("postSupplierMaster")]
        public IActionResult PostSupplierMaster([FromBody] SupplierMaster _supplier)
        {
            try
            {
                // Get Authorization header safely
                if (!Request.Headers.ContainsKey("Authorization"))
                    return Unauthorized(new { isSucess = false, message = "Authorization header missing" });

                string token = Request.Headers["Authorization"].ToString();
                if (string.IsNullOrWhiteSpace(token) || token.Length <= 7)
                    return Unauthorized(new { isSucess = false, message = "Invalid authorization token" });

                token = token.Substring(7);

                var tokenClaims = Fluxion_Handler.GetJWTTokenClaims(token, _key._jwtKey, true);

                var parameters = new DynamicParameters();
                parameters.Add("@Flag", 100);
                parameters.Add("@ClientID", tokenClaims.ClientId);
                // core fields
                parameters.Add("@SupplierCode", _supplier.SupplierCode);
                parameters.Add("@SupplierName", _supplier.SupplierName);
                parameters.Add("@Address", _supplier.Address);
                parameters.Add("@MobileNo", _supplier.MobileNo);
                parameters.Add("@GstNo", _supplier.GstNo);
                parameters.Add("@Place", _supplier.Place);
                parameters.Add("@CreditDays", _supplier.CreditDays);

                // NEW fields from the UI / updated SP
                parameters.Add("@City", _supplier.City);
                parameters.Add("@State", _supplier.State);
                parameters.Add("@Pincode", _supplier.Pincode);
                parameters.Add("@Phone1", _supplier.Phone1);
                parameters.Add("@Phone2", _supplier.Phone2);
                parameters.Add("@Email", _supplier.Email);
                parameters.Add("@ContactPersonName", _supplier.ContactPersonName);
                parameters.Add("@ContactPersonNumber", _supplier.ContactPersonNumber);

                parameters.Add("@UserID", tokenClaims.UserId);

                // Execute stored proc. Using Query to keep same pattern; first result contains SupplierID returned by SP.
                var data = _dbcontext.Query("SP_SupplierMaster", parameters, commandType: CommandType.StoredProcedure);

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


        #region Supplier Master PUT
        [HttpPost("putSupplierMaster")]
        public IActionResult PutSupplierMaster([FromHeader] long supplierID, [FromBody] SupplierMaster _supplier)
        {
            try
            {
                // Validate Authorization header
                if (!Request.Headers.ContainsKey("Authorization"))
                    return Unauthorized(new { isSucess = false, message = "Authorization header missing" });

                string token = Request.Headers["Authorization"].ToString();
                if (string.IsNullOrWhiteSpace(token) || token.Length <= 7)
                    return Unauthorized(new { isSucess = false, message = "Invalid authorization token" });

                token = token.Substring(7);

                var tokenClaims = Fluxion_Handler.GetJWTTokenClaims(token, _key._jwtKey, true);

                var parameters = new DynamicParameters();
                parameters.Add("@Flag", 101);
                parameters.Add("@ClientID", tokenClaims.ClientId);
                parameters.Add("@SuplierID", supplierID);

                // core fields
                parameters.Add("@SupplierCode", _supplier.SupplierCode);
                parameters.Add("@SupplierName", _supplier.SupplierName);
                parameters.Add("@Address", _supplier.Address);
                parameters.Add("@MobileNo", _supplier.MobileNo);
                parameters.Add("@GstNo", _supplier.GstNo);
                parameters.Add("@Place", _supplier.Place);
                parameters.Add("@CreditDays", _supplier.CreditDays);

                // NEW fields added in SP
                parameters.Add("@City", _supplier.City);
                parameters.Add("@State", _supplier.State);
                parameters.Add("@Pincode", _supplier.Pincode);
                parameters.Add("@Phone1", _supplier.Phone1);
                parameters.Add("@Phone2", _supplier.Phone2);
                parameters.Add("@Email", _supplier.Email);
                parameters.Add("@ContactPersonName", _supplier.ContactPersonName);
                parameters.Add("@ContactPersonNumber", _supplier.ContactPersonNumber);

                parameters.Add("@UserID", tokenClaims.UserId);

                var data = _dbcontext.Query("SP_SupplierMaster", parameters, commandType: CommandType.StoredProcedure);

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


        #region Supplier Master GET
        [HttpGet("getSupplierMaster")]
        public IActionResult getSupplierMaster([FromHeader] string? FilterText)
        {
            try
            {
                string token = Request.Headers["Authorization"];
                token = token.Substring(7);

                var tokenClaims = Fluxion_Handler.GetJWTTokenClaims(token, _key._jwtKey, true);

                var parameters = new DynamicParameters();
                parameters.Add("@Flag", 102);
                parameters.Add("@ClientID", tokenClaims.ClientId);
                parameters.Add("@FilterText", string.IsNullOrEmpty(FilterText) ? null : FilterText);

                var data = _dbcontext.Query("SP_SupplierMaster", parameters, commandType: CommandType.StoredProcedure);

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

        #region  Delete Supplier Master
        [HttpPost("deleteSupplierMaster")]
        public IActionResult DeleteSupplierMaster([FromHeader] long supplierID)
        {
            try
            {
                string token = Request.Headers["Authorization"];
                token = token.Substring(7);

                var tokenClaims = Fluxion_Handler.GetJWTTokenClaims(token, _key._jwtKey, true);

                var parameters = new DynamicParameters();
                parameters.Add("@Flag", 103);
                parameters.Add("@ClientID", tokenClaims.ClientId);
                parameters.Add("@SuplierID", supplierID);
                parameters.Add("@UserID", tokenClaims.UserId);

                bool result = _dbcontext.ExecuteScalar<bool>("SP_SupplierMaster", parameters, commandType: CommandType.StoredProcedure);

                if (result)
                {
                    var parameters1 = new DynamicParameters();
                    parameters1.Add("@Flag", 104);
                    parameters1.Add("@ClientID", tokenClaims.ClientId);
                    parameters1.Add("@SuplierID", supplierID);
                    parameters1.Add("@UserID", tokenClaims.UserId);

                    var _data = _dbcontext.ExecuteScalar<bool>("SP_SupplierMaster", parameters1, commandType: CommandType.StoredProcedure);

                    _response.isSucess = true;
                    _response.message = "Success";
                    _response.data = _data;
                }
                else
                {
                    _response.isSucess = false;
                    _response.message = "Someting Went Wrong ..";
                    _response.data = null;
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

        #region Supplier Master Activate/Deactivate
        [HttpPost("toggleSupplierStatus")]
        public IActionResult ToggleSupplierStatus([FromHeader] long supplierID, [FromHeader] bool isActive)
        {
            try
            {
                // Validate Authorization header
                if (!Request.Headers.ContainsKey("Authorization"))
                    return Unauthorized(new { isSucess = false, message = "Authorization header missing" });

                string token = Request.Headers["Authorization"].ToString();
                if (string.IsNullOrWhiteSpace(token) || token.Length <= 7)
                    return Unauthorized(new { isSucess = false, message = "Invalid authorization token" });

                token = token.Substring(7);
                var tokenClaims = Fluxion_Handler.GetJWTTokenClaims(token, _key._jwtKey, true);

                var parameters = new DynamicParameters();
                parameters.Add("@Flag", 106);
                parameters.Add("@ClientID", tokenClaims.ClientId);
                parameters.Add("@SuplierID", supplierID);
                parameters.Add("@IsActive", isActive);
                parameters.Add("@UserID", tokenClaims.UserId);

                var data = _dbcontext.Query("SP_SupplierMaster", parameters, commandType: CommandType.StoredProcedure);

                _response.isSucess = true;
                _response.message = "Supplier status updated successfully.";
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


        /******** Patient Master Controller **********/

        #region POST Patient Master
        [HttpPost("postPatientMaster")]
        public IActionResult PostPatientMaster([FromBody] PatientMaster _pat)
        {
            try
            {
                string token = Request.Headers["Authorization"];
                token = token.Substring(7);

                var tokenClaims = Fluxion_Handler.GetJWTTokenClaims(token, _key._jwtKey, true);

                var parameters = new DynamicParameters();
                parameters.Add("@Flag", 100);
                parameters.Add("@ClientID", tokenClaims.ClientId);
                parameters.Add("@JsonData", JsonConvert.SerializeObject(_pat));
                parameters.Add("@UserID", tokenClaims.UserId);

                var data = _dbcontext.Query("SP_PatientMaster", parameters, commandType: CommandType.StoredProcedure);

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

        #region Patient Master Update
        [HttpPost("putPatientMaster")]
        public IActionResult PutPatientMaster([FromHeader] long PatientID, [FromBody] PatientMaster _pat)
        {
            try
            {
                string token = Request.Headers["Authorization"];
                token = token.Substring(7);

                var tokenClaims = Fluxion_Handler.GetJWTTokenClaims(token, _key._jwtKey, true);

                var parameters = new DynamicParameters();
                parameters.Add("@Flag", 101);
                parameters.Add("@ClientID", tokenClaims.ClientId);
                parameters.Add("@PatientID", PatientID);
                parameters.Add("@JsonData", JsonConvert.SerializeObject(_pat));
                parameters.Add("@UserID", tokenClaims.UserId);

                var data = _dbcontext.Query("SP_PatientMaster", parameters, commandType: CommandType.StoredProcedure);

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

        #region Get Patient Master 
        [HttpGet("getPatientMaster")]
        public IActionResult GetPatientMaster([FromHeader] int PageNo, [FromHeader] int PageSize, [FromHeader] string? FilterText)
        {
            try
            {
                string token = Request.Headers["Authorization"];
                token = token.Substring(7);

                var tokenClaims = Fluxion_Handler.GetJWTTokenClaims(token, _key._jwtKey, true);

                var parameters = new DynamicParameters();
                parameters.Add("@Flag", 102);
                parameters.Add("@ClientID", tokenClaims.ClientId);
                parameters.Add("@PageNo", PageNo);
                parameters.Add("@PageSize", PageSize);
                parameters.Add("@FilterText", string.IsNullOrEmpty(FilterText) ? "" : FilterText);

                var data = _dbcontext.QueryMultiple("SP_PatientMaster", parameters, commandType: CommandType.StoredProcedure);
                var _sectionData = data.Read<dynamic>().ToList();
                var _pageContex = data.Read<dynamic>().ToList();

                var Response = new
                {
                    isSucess = true,
                    message = "Success",
                    data = new
                    {
                        masterData = _sectionData,
                        pageContex = _pageContex,
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

        /****** Doctor Master *******/

        #region Doctor Master POST (JSON)
        [HttpPost("postDoctorMaster")]
        public IActionResult PostDoctorMaster([FromBody] DoctorMasterWithSchedule doctor)
        {
            try
            {
                string token = Request.Headers["Authorization"];
                token = token.Substring(7);

                var tokenClaims = Fluxion_Handler.GetJWTTokenClaims(token, _key._jwtKey, true);

                var parameters = new DynamicParameters();
                parameters.Add("@Flag", 100);
                parameters.Add("@ClientID", tokenClaims.ClientId);
                parameters.Add("@JsonData", Newtonsoft.Json.JsonConvert.SerializeObject(doctor));
                parameters.Add("@UserID", tokenClaims.UserId);

                var data = _dbcontext.Query("SP_Masters", parameters, commandType: CommandType.StoredProcedure);

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

        #region Doctor Master GET
        [HttpGet("getDoctorMaster")]
        public IActionResult GetDoctorMaster([FromHeader] string? dptName)
        {
            try
            {
                string token = Request.Headers["Authorization"];
                token = token.Substring(7);

                var tokenClaims = Fluxion_Handler.GetJWTTokenClaims(token, _key._jwtKey, true);

                var parameters = new DynamicParameters();
                parameters.Add("@Flag", 101);
                parameters.Add("@ClientID", tokenClaims.ClientId);
                parameters.Add("DepartmentName", string.IsNullOrEmpty(dptName) ? null : dptName);

                // Use QueryMultiple to get doctors, schedules, and reschedule history
                var data = _dbcontext.QueryMultiple("SP_Masters", parameters, commandType: CommandType.StoredProcedure);

                var doctorListRaw = data.Read<dynamic>().ToList();
                var scheduleListRaw = data.Read<dynamic>().ToList();
                var rescheduleHistory = data.Read<AppointmentRescheduleHistoryDto>().ToList();

                // Fully materialize doctor and schedule lists to avoid disposed reader issues
                var doctorList = doctorListRaw.Select(doc => new
                {
                    DoctorID = doc.DoctorID != null ? (int)doc.DoctorID : 0,
                    DoctorName = doc.DoctorName != null ? (string)doc.DoctorName : string.Empty,
                    Department = doc.Department != null ? (string)doc.Department : string.Empty,
                    Designation = doc.Designation != null ? (string)doc.Designation : string.Empty,
                    MobileNo = doc.MobileNo != null ? (string)doc.MobileNo : string.Empty,
                    Address = doc.Address != null ? (string)doc.Address : string.Empty,
                    PersonalDetails = doc.PersonalDetails != null ? (string)doc.PersonalDetails : string.Empty,
                    ImageUrl = doc.ImageUrl != null ? (string)doc.ImageUrl : string.Empty,
                    SlotDuration = doc.SlotDuration != null ? (int?)doc.SlotDuration : null,
                    NewRegistrationFee = doc.new_registration_fee != null ? (decimal?)doc.new_registration_fee : null,
                    RenewRegistrationFee = doc.renew_registration_fee != null ? (decimal?)doc.renew_registration_fee : null,
                    NewConsultFee = doc.new_consult_fee != null ? (decimal?)doc.new_consult_fee : null,
                    RenewConsultFee = doc.renew_consult_fee != null ? (decimal?)doc.renew_consult_fee : null,
                    ValidityDays = doc.validity_days != null ? (int?)doc.validity_days : null,
                    CutsAmount = doc.cuts_amount != null ? (decimal?)doc.cuts_amount : null,
                    Commission = doc.commission != null ? (decimal?)doc.commission : null,
                    Discount = doc.Discount != null ? (decimal?)doc.Discount : null 
                }).ToList();

                var scheduleList = scheduleListRaw.Select(s => new
                {
                    DoctorID = s.DoctorID != null ? (int)s.DoctorID : 0,
                    DayOfWeek = s.DayOfWeek != null ? (byte)s.DayOfWeek : (byte)0,
                    DayOfWeekNName = s.DayOfWeekNName != null ? (string)s.DayOfWeekNName : string.Empty,
                    IsAvailable = s.IsAvailable != null ? (bool)s.IsAvailable : false,
                    StartTime = s.StartTime != null ? (string)s.StartTime : string.Empty,
                    EndTime = s.EndTime != null ? (string)s.EndTime : string.Empty
                }).ToList();

                // Map schedules and reschedule history to doctors using DoctorMasterGetDto
                var doctorWithSchedules = doctorList.Select(doc =>
                {
                    var docId = doc.DoctorID;
                    var schedules = scheduleList
                        .Where(s => s.DoctorID == docId)
                        .Select(s => new DoctorWeeklySchedule
                        {
                            DayOfWeek = s.DayOfWeek,
                            DayOfWeekNName = s.DayOfWeekNName,
                            IsAvailable = s.IsAvailable,
                            StartTime = s.StartTime,
                            EndTime = s.EndTime
                        }).ToList();

                    var doctorRescheduleHistory = rescheduleHistory
                        .Where(r => r.DoctorID == docId)
                        .ToList();

                    return new DoctorMasterGetDto
                    {
                        DoctorID = doc.DoctorID,
                        DoctorName = doc.DoctorName,
                        Designation = doc.Designation,
                        Department = doc.Department,
                        MobileNo = doc.MobileNo,
                        Address = doc.Address,
                        PersonalDetails = doc.PersonalDetails,
                        ImageUrl = doc.ImageUrl,
                        SlotDuration = doc.SlotDuration,
                        NewRegistrationFee = doc.NewRegistrationFee,
                        RenewRegistrationFee = doc.RenewRegistrationFee,
                        NewConsultFee = doc.NewConsultFee,
                        RenewConsultFee = doc.RenewConsultFee,
                        ValidityDays = doc.ValidityDays,
                        CutsAmount = doc.CutsAmount,
                        Commission = doc.Commission,
                        Discount = doc.Discount,
                        WeeklySchedule = schedules,
                        RescheduleHistory = doctorRescheduleHistory
                    };
                }).ToList();

                _response.isSucess = true;
                _response.message = "Success";
                _response.data = doctorWithSchedules;
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

        /****** Lab Master *******/

        #region Lab Master POST
        [HttpPost("postLabMaster")]
        public IActionResult PostLabMaster([FromHeader] int? ID, [FromHeader] string? Name, [FromHeader] string? Mobile, [FromHeader] string? Place, [FromHeader] bool? isOutSource)
        {
            try
            {
                string token = Request.Headers["Authorization"];
                token = token.Substring(7);

                var tokenClaims = Fluxion_Handler.GetJWTTokenClaims(token, _key._jwtKey, true);

                var parameters = new DynamicParameters();
                parameters.Add("@Flag", 102);
                parameters.Add("@ClientID", tokenClaims.ClientId);
                parameters.Add("@ID", ID);
                parameters.Add("@Name", Name);
                parameters.Add("@Place", Place);
                parameters.Add("@MobileNo", Mobile);
                parameters.Add("@IsOutSource", isOutSource);

                var data = _dbcontext.Query("SP_Masters", parameters, commandType: CommandType.StoredProcedure);

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

        #region Lab Master GET
        [HttpGet("getLabMaster")]
        public IActionResult getLabMaster()
        {
            try
            {
                string token = Request.Headers["Authorization"];
                token = token.Substring(7);

                var tokenClaims = Fluxion_Handler.GetJWTTokenClaims(token, _key._jwtKey, true);

                var parameters = new DynamicParameters();
                parameters.Add("@Flag", 103);
                parameters.Add("@ClientID", tokenClaims.ClientId);

                var data = _dbcontext.Query("SP_Masters", parameters, commandType: CommandType.StoredProcedure);


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

        /****** DELETE DATA *****/

        #region Trucate Tables
        [AllowAnonymous]
        [HttpPost("deletedata")]
        public IActionResult deleteTablesData()
        {
            try
            {
                var data = _dbcontext.Query("SP_ClearData", commandType: CommandType.StoredProcedure);

                return Ok("Success");

            }
            catch (Exception ex)
            {
                _response.isSucess = false;
                _response.message = ex.Message;

                return StatusCode(500, _response);
            }
        }
        #endregion

        /*********** User Permissions ***************/

        public class Permission
        {
            public string HeaderName { get; set; }
            public string ModuleName { get; set; }
            public string PermissionName { get; set; }
            public bool IsGranted { get; set; }
        }

        public class ModulePermissions
        {
            public int ModuleID { get; set; }
            public string ModuleName { get; set; }
            public Dictionary<string, bool> Permissions { get; set; }
        }

        public class HeaderPermissions
        {
            public string HeaderName { get; set; }
            public List<ModulePermissions> Modules { get; set; }
        }

        /**************** Place Master ******************/

        #region Post Place Master
        [HttpPost("postPlaceMaster")]
        public IActionResult PostPlaceMaster([FromHeader] int? ID, [FromHeader] string? Name)
        {
            try
            {
                string token = Request.Headers["Authorization"];
                token = token.Substring(7);

                var tokenClaims = Fluxion_Handler.GetJWTTokenClaims(token, _key._jwtKey, true);

                var parameters = new DynamicParameters();
                parameters.Add("@Flag", 104);
                parameters.Add("@ClientID", tokenClaims.ClientId);
                parameters.Add("@ID", ID);
                parameters.Add("@Name", Name);
                parameters.Add("@UserID", tokenClaims.UserId);

                var data = _dbcontext.Query("SP_Masters", parameters, commandType: CommandType.StoredProcedure);

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

        #region Place Master Get
        [HttpGet("getPlaceMaster")]
        public IActionResult getPlaceMaster()
        {
            try
            {
                string token = Request.Headers["Authorization"];
                token = token.Substring(7);

                var tokenClaims = Fluxion_Handler.GetJWTTokenClaims(token, _key._jwtKey, true);

                var parameters = new DynamicParameters();
                parameters.Add("@Flag", 105);
                parameters.Add("@ClientID", tokenClaims.ClientId);

                var data = _dbcontext.Query("SP_Masters", parameters, commandType: CommandType.StoredProcedure);


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


        /**************** Privilage Card ***********************/

        #region Privilege Card POST
        [HttpPost("postPrivilegeCard")]
        public IActionResult postPrivilegeCard([FromHeader] string cardName, [FromHeader] string description, [FromHeader] decimal amount,
            [FromHeader] decimal? discPerc)
        {
            try
            {
                string token = Request.Headers["Authorization"];
                token = token.Substring(7);

                var tokenClaims = Fluxion_Handler.GetJWTTokenClaims(token, _key._jwtKey, true);

                var parameters = new DynamicParameters();
                parameters.Add("@Flag", 100);
                parameters.Add("@ClientID", tokenClaims.ClientId);
                parameters.Add("@CardName", cardName);
                parameters.Add("@Description", description);
                parameters.Add("@CardAmount", amount);
                parameters.Add("@DiscPerc", discPerc);

                parameters.Add("@UserID", tokenClaims.UserId);

                var data = _dbcontext.Query("SP_PrivilegeCards", parameters, commandType: CommandType.StoredProcedure);

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

        #region Privilege Card PUT
        [HttpPost("putPrivilegeCard")]
        public IActionResult putPrivilegeCard([FromHeader] long cardID, [FromHeader] string cardName, [FromHeader] string description,
            [FromHeader] decimal amount, [FromHeader] decimal? discPerc)
        {
            try
            {
                string token = Request.Headers["Authorization"];
                token = token.Substring(7);

                var tokenClaims = Fluxion_Handler.GetJWTTokenClaims(token, _key._jwtKey, true);

                var parameters = new DynamicParameters();
                parameters.Add("@Flag", 101);
                parameters.Add("@ClientID", tokenClaims.ClientId);
                parameters.Add("@CardID", cardID);
                parameters.Add("@CardName", cardName);
                parameters.Add("@Description", description);
                parameters.Add("@CardAmount", amount);
                parameters.Add("@DiscPerc", discPerc);
                parameters.Add("@UserID", tokenClaims.UserId);

                var data = _dbcontext.Query("SP_PrivilegeCards", parameters, commandType: CommandType.StoredProcedure);

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

        #region Privilage Card GET
        [HttpGet("getPrivilegeCard")]
        public IActionResult GetPrivilegeCard()
        {
            try
            {
                string token = Request.Headers["Authorization"];
                token = token.Substring(7);

                var tokenClaims = Fluxion_Handler.GetJWTTokenClaims(token, _key._jwtKey, true);

                var parameters = new DynamicParameters();
                parameters.Add("@Flag", 102);
                parameters.Add("@ClientID", tokenClaims.ClientId);

                var data = _dbcontext.Query("SP_PrivilegeCards", parameters, commandType: CommandType.StoredProcedure);

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

        #region Privilage Card Pricing
        [HttpGet("getPrivilegeCardPricing")]
        public IActionResult getPrivilegeCardPricing()
        {
            try
            {
                string token = Request.Headers["Authorization"];
                token = token.Substring(7);

                var tokenClaims = Fluxion_Handler.GetJWTTokenClaims(token, _key._jwtKey, true);

                var parameters = new DynamicParameters();
                parameters.Add("@Flag", 103);
                parameters.Add("@ClientID", tokenClaims.ClientId);

                var data = _dbcontext.Query("SP_PrivilegeCards", parameters, commandType: CommandType.StoredProcedure);

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

        #region Privilege Card Pricing POST
        [HttpPost("postPrivilegeCardPricing")]
        public IActionResult postPrivilegeCard([FromBody] PrivilageCardsHd _prv)
        {
            try
            {
                string token = Request.Headers["Authorization"];
                token = token.Substring(7);

                var tokenClaims = Fluxion_Handler.GetJWTTokenClaims(token, _key._jwtKey, true);

                var parameters = new DynamicParameters();
                parameters.Add("@Flag", 104);
                parameters.Add("@ClientID", tokenClaims.ClientId);
                parameters.Add("@JsonData", JsonConvert.SerializeObject(_prv));
                parameters.Add("@UserID", tokenClaims.UserId);


                var data = _dbcontext.Query("SP_PrivilegeCards", parameters, commandType: CommandType.StoredProcedure);

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

        #region Privilage Card Patient Master Mapping
        [HttpPost("postPrivilegeCardpatietMapping")]
        public IActionResult postPrivilegeCardpatietMapping([FromBody] PrivilageCardsMapping _crd)
        {
            try
            {
                string token = Request.Headers["Authorization"];
                token = token.Substring(7);

                var tokenClaims = Fluxion_Handler.GetJWTTokenClaims(token, _key._jwtKey, true);

                var parameters = new DynamicParameters();
                parameters.Add("@Flag", 106);
                parameters.Add("@ClientID", tokenClaims.ClientId);
                parameters.Add("@PatientID", _crd.patientID);
                parameters.Add("@CardID", _crd.cardID);
                parameters.Add("@CardNumber", _crd.cardNumber);
                parameters.Add("@UserID", tokenClaims.UserId);

                var data = _dbcontext.Query("SP_PrivilegeCards", parameters, commandType: CommandType.StoredProcedure);

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


        /**************** Client Master ****************/

        #region GET Client Master
        [HttpGet("getClientMaster")]
        public IActionResult getClientMaster()
        {
            try
            {
                string token = Request.Headers["Authorization"];
                token = token.Substring(7);

                var tokenClaims = Fluxion_Handler.GetJWTTokenClaims(token, _key._jwtKey, true);

                var parameters = new DynamicParameters();
                parameters.Add("@Flag", 107);
                parameters.Add("@ClientID", tokenClaims.ClientId);

                var data = _dbcontext.Query("SP_Masters", parameters, commandType: CommandType.StoredProcedure);

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

        #region Client Master Update
        [HttpPost("putClientMaster")]
        public IActionResult putClientMaster([FromBody] ClientMasterPUT _img)
        {
            try
            {
                string token = Request.Headers["Authorization"];
                token = token.Substring(7);

                var tokenClaims = Fluxion_Handler.GetJWTTokenClaims(token, _key._jwtKey, true);

                var parameters = new DynamicParameters();
                parameters.Add("@Flag", 106);
                parameters.Add("@ClientID", tokenClaims.ClientId);
                parameters.Add("@HeaderImageUrl", _img.headerImage);
                parameters.Add("@HeaderCloudImageUrl", _img.headerICloudmage);
                parameters.Add("@FooterImageUrl", _img.footerImage);
                parameters.Add("@FooterCloudImageUrl", _img.footerCloudImage);
                parameters.Add("@SignatureImageUrl", _img.signature);
                parameters.Add("@letterheading_url", _img.letterheading_url);
                parameters.Add("@BackupPath", _img.backupPath);

                var data = _dbcontext.Query("SP_Masters", parameters, commandType: CommandType.StoredProcedure);

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

        /***************** Device Connfig & Printers **********************/

        #region Post Device info
        [HttpPost("postDeviceConfig")]
        public IActionResult postDeviceConfig([FromBody] DeviceConfig _dv)
        {
            try
            {
                string token = Request.Headers["Authorization"];
                token = token.Substring(7);

                var tokenClaims = Fluxion_Handler.GetJWTTokenClaims(token, _key._jwtKey, true);

                var parameters = new DynamicParameters();
                parameters.Add("@Flag", 108);
                parameters.Add("@ClientID", tokenClaims.ClientId);
                parameters.Add("@sysName", _dv.sysName);
                parameters.Add("@SysID", _dv.sysID);
                parameters.Add("@resultPrinter", _dv.resultPrinter);
                parameters.Add("@billPrinter", _dv.billPrinter);
                parameters.Add("@barcodePrinter", _dv.barcodePrinter);
                parameters.Add("@ZoomFactor", _dv.zoomFactor);
                parameters.Add("@OpBillPrinter", _dv.opBillPrinter);
                parameters.Add("@OpBillCardPrinter", _dv.opBillCardPrinter);
                parameters.Add("@OpRecieptPrinter", _dv.opRecieptPrinter);
                parameters.Add("@outsourceRecieptPrinter", _dv.outsourceRecieptPrinter);
                parameters.Add("@pharmacyBillPriinter", _dv.pharmacyBillPriinter); 

                var data = _dbcontext.Query("SP_Masters", parameters, commandType: CommandType.StoredProcedure);

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

        #region Get Device Info
        [HttpGet("getDeviceConfig")]
        public IActionResult getDeviceConfig()
        {
            try
            {
                string token = Request.Headers["Authorization"];
                token = token.Substring(7);

                var tokenClaims = Fluxion_Handler.GetJWTTokenClaims(token, _key._jwtKey, true);

                var parameters = new DynamicParameters();
                parameters.Add("@Flag", 109);
                parameters.Add("@ClientID", tokenClaims.ClientId);

                var data = _dbcontext.Query("SP_Masters", parameters, commandType: CommandType.StoredProcedure);

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

        /******************* User Master **********************/

        #region POST User Master
        [HttpPost("postUserMaster")]
        public IActionResult postUserMaster([FromBody] UserMaster _us)
        {
            try
            {
                string token = Request.Headers["Authorization"];
                token = token.Substring(7);

                var tokenClaims = Fluxion_Handler.GetJWTTokenClaims(token, _key._jwtKey, true);

                var parameters = new DynamicParameters();
                parameters.Add("@Flag", 110);
                parameters.Add("@ClientID", tokenClaims.ClientId);
                parameters.Add("@JsonData", JsonConvert.SerializeObject(_us));

                var data = _dbcontext.Query("SP_Masters", parameters, commandType: CommandType.StoredProcedure);

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

        #region PUT User Master
        [HttpPost("putUserMaster")]
        public IActionResult putUserMaster([FromHeader] int UserID, [FromBody] UserMaster _us)
        {
            try
            {
                string token = Request.Headers["Authorization"];
                token = token.Substring(7);

                var tokenClaims = Fluxion_Handler.GetJWTTokenClaims(token, _key._jwtKey, true);

                var parameters = new DynamicParameters();
                parameters.Add("@Flag", 111);
                parameters.Add("@ClientID", tokenClaims.ClientId);
                parameters.Add("@UserID", UserID);
                parameters.Add("@JsonData", JsonConvert.SerializeObject(_us));

                var data = _dbcontext.Query("SP_Masters", parameters, commandType: CommandType.StoredProcedure);

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

        #region GET User Master
        [HttpGet("getUserMaster")]
        public IActionResult getUserMaster()
        {
            try
            {
                string token = Request.Headers["Authorization"];
                token = token.Substring(7);

                var tokenClaims = Fluxion_Handler.GetJWTTokenClaims(token, _key._jwtKey, true);

                var parameters = new DynamicParameters();
                parameters.Add("@Flag", 112);
                parameters.Add("@ClientID", tokenClaims.ClientId);
                var data = _dbcontext.Query("SP_Masters", parameters, commandType: CommandType.StoredProcedure);

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

        #region DELETE User 
        [HttpPost("deleteUserMaster")]
        public IActionResult deleteUserMaster([FromHeader] int UserID)
        {
            try
            {
                string token = Request.Headers["Authorization"];
                token = token.Substring(7);

                var tokenClaims = Fluxion_Handler.GetJWTTokenClaims(token, _key._jwtKey, true);

                var parameters = new DynamicParameters();
                parameters.Add("@Flag", 118);
                parameters.Add("@ClientID", tokenClaims.ClientId);
                parameters.Add("@UserID", UserID);
                var data = _dbcontext.Query("SP_Masters", parameters, commandType: CommandType.StoredProcedure);

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

        /********** General Settings **************/

        #region POST General Settings
        [HttpPost("postGeneralSettings")]
        public IActionResult postGeneralSettings([FromBody] GeneralSettings _us)
        {
            try
            {
                string token = Request.Headers["Authorization"];
                token = token.Substring(7);

                var tokenClaims = Fluxion_Handler.GetJWTTokenClaims(token, _key._jwtKey, true);

                var jsonData = System.Text.Json.JsonSerializer.Serialize(_us);

                var parameters = new DynamicParameters();
                parameters.Add("@Flag", 113);
                parameters.Add("@ClientID", tokenClaims.ClientId);
                parameters.Add("@JsonData", jsonData);

                var data = _dbcontext.Query("SP_Masters", parameters, commandType: CommandType.StoredProcedure);

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

        #region Get General Settings
        [HttpGet("getGeneralSettings")]
        public IActionResult getGeneralSettings()
        {
            try
            {
                string token = Request.Headers["Authorization"];
                token = token.Substring(7);

                var tokenClaims = Fluxion_Handler.GetJWTTokenClaims(token, _key._jwtKey, true);

                var parameters = new DynamicParameters();
                parameters.Add("@Flag", 114);
                parameters.Add("@ClientID", tokenClaims.ClientId);
                var data = _dbcontext.Query("SP_Masters", parameters, commandType: CommandType.StoredProcedure);

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


        /******* Department Master *********/

        #region POST Department Master  
        [HttpPost("postDepartmentMaster")]
        public IActionResult postDepartmentMaster([FromHeader] string departmentName, [FromHeader] int? departmentID)
        {
            try
            {
                string token = Request.Headers["Authorization"];
                token = token.Substring(7);

                var tokenClaims = Fluxion_Handler.GetJWTTokenClaims(token, _key._jwtKey, true);

                int? _flag = 101;

                if (departmentID == -1)
                {
                    _flag = 115;
                }
                else
                {
                    _flag = 116;
                }

                var parameters = new DynamicParameters();
                parameters.Add("@Flag", _flag);
                parameters.Add("@ClientID", tokenClaims.ClientId);
                parameters.Add("@DepartmentID", departmentID);
                parameters.Add("@DepartmentName", departmentName);
                var data = _dbcontext.Query("SP_Masters", parameters, commandType: CommandType.StoredProcedure);

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

        #region GET Depatment Master
        [HttpGet("getDepartmentMaster")]
        public IActionResult getDepartmentMaster()
        {
            try
            {
                string token = Request.Headers["Authorization"];
                token = token.Substring(7);

                var tokenClaims = Fluxion_Handler.GetJWTTokenClaims(token, _key._jwtKey, true);

                var parameters = new DynamicParameters();
                parameters.Add("@Flag", 117);
                parameters.Add("@ClientID", tokenClaims.ClientId);
                var data = _dbcontext.Query("SP_Masters", parameters, commandType: CommandType.StoredProcedure);

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


        /*********** User Auth *************/

        #region POST User Auth Permission
        [HttpPost("postUserPermissions")]
        public IActionResult postUserPermissions([FromBody] UserPermissionsInputModel _pat)
        {
            try
            {
                string token = Request.Headers["Authorization"];
                token = token.Substring(7);

                var tokenClaims = Fluxion_Handler.GetJWTTokenClaims(token, _key._jwtKey, true);

                var parameters = new DynamicParameters();
                parameters.Add("@Flag", 100);
                parameters.Add("@ClientID", tokenClaims.ClientId);
                parameters.Add("@JsonData", JsonConvert.SerializeObject(_pat));
                parameters.Add("@UserID", tokenClaims.UserId);

                var data = _dbcontext.Query("SP_UserAuthPermissions", parameters, commandType: CommandType.StoredProcedure);

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

        #region GET User Auth Peermission
        [HttpGet("getUserAuthData")]
        public IActionResult getUserAuthData([FromHeader] int? userID)
        {
            try
            {
                string token = Request.Headers["Authorization"];
                token = token.Substring(7);

                var tokenClaims = Fluxion_Handler.GetJWTTokenClaims(token, _key._jwtKey, true);

                var parameters = new DynamicParameters();
                parameters.Add("@Flag", 101);
                parameters.Add("@ClientID", tokenClaims.ClientId);
                parameters.Add("@UserID", userID);
                var data = _dbcontext.Query("SP_UserAuthPermissions", parameters, commandType: CommandType.StoredProcedure);

                var groupedPermissions = data
                 .GroupBy(p => p.HeaderName)
                 .Select(headerGroup => new HeaderPermissions
                 {
                     HeaderName = headerGroup.Key,
                     Modules = headerGroup
                         .GroupBy(p => p.ModuleName)
                         .Select(moduleGroup => new ModulePermissions
                         {
                             ModuleID = moduleGroup.Select(p => (int)p.ModuleID).FirstOrDefault(),
                             ModuleName = moduleGroup.Key,
                             Permissions = moduleGroup
                                .GroupBy(p => (string)p.PermissionName) // Cast key to string
                                .ToDictionary(
                                    g => g.Key,
                                    g => g.Any(p => (bool)p.IsGranted) // Cast value to bool
                                )
                         }).ToList()
                 }).ToList();

                _response.isSucess = true;
                _response.message = "Success";
                _response.data = groupedPermissions;

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

        #region GET User Auth Initial Page Load Data
        [HttpGet("getUserAuthIntialLoad")]
        public IActionResult getUserAuthIntialLoad()
        {
            try
            {
                string token = Request.Headers["Authorization"];
                token = token.Substring(7);

                var tokenClaims = Fluxion_Handler.GetJWTTokenClaims(token, _key._jwtKey, true);

                var parameters = new DynamicParameters();
                parameters.Add("@Flag", 102);
                var data = _dbcontext.Query("SP_UserAuthPermissions", parameters, commandType: CommandType.StoredProcedure);

                var transformedData = data.GroupBy(d => d.HeaderName)
                .Select(g => new
                {
                    headerName = g.Key,
                    modules = g.Select(m => new
                    {
                        moduleID = m.ModuleID,
                        moduleName = m.ModuleName,
                        appName = m.AppName,
                    }).ToList()
                }).ToList();


                _response.isSucess = true;
                _response.message = "Success";
                _response.data = transformedData;

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

        #region GET User Auth Peermission by Single User
        [HttpGet("getUserAuthDatabyID")]
        public IActionResult getUserAuthDataByUserID([FromHeader] int? userID)
        {
            try
            {
                string token = Request.Headers["Authorization"];
                token = token.Substring(7);

                var tokenClaims = Fluxion_Handler.GetJWTTokenClaims(token, _key._jwtKey, true);

                var parameters = new DynamicParameters();
                parameters.Add("@Flag", 103);
                parameters.Add("@ClientID", tokenClaims.ClientId);
                parameters.Add("@UserID", userID);
                var data = _dbcontext.Query("SP_UserAuthPermissions", parameters, commandType: CommandType.StoredProcedure);

                var groupedPermissions = data
                 .GroupBy(p => (string)p.HeaderName)
                 .Select(headerGroup => new HeaderPermissions
                 {
                     HeaderName = headerGroup.Key,
                     Modules = headerGroup
                         .GroupBy(p => (string)p.ModuleName)
                         .Select(moduleGroup => new ModulePermissions
                         {
                             ModuleID = moduleGroup.Select(p => (int)p.ModuleID).FirstOrDefault(),
                             ModuleName = moduleGroup.Key,
                             Permissions = moduleGroup
                                 .GroupBy(p => (string)p.PermissionName)
                                 .ToDictionary(
                                     g => g.Key,
                                     g => g.Any(p => (bool)p.IsGranted)
                                 )
                         })
                         .ToList()
                 }).ToList();


                _response.isSucess = true;
                _response.message = "Success";
                _response.data = groupedPermissions;

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

        #region POST Copy User Roles
        public class CopyRolesRequest
        {
            public int SourceUserID { get; set; }
            public int TargetUserID { get; set; }
        }

        [HttpPost("copyUserRoles")]
        public IActionResult CopyUserRoles([FromBody] CopyRolesRequest request)
        {
            try
            {
                string token = Request.Headers["Authorization"];
                token = token.Substring(7);

                var tokenClaims = Fluxion_Handler.GetJWTTokenClaims(token, _key._jwtKey, true);

                var payload = new { SourceUserID = request.SourceUserID, TargetUserID = request.TargetUserID };
                var parameters = new DynamicParameters();
                parameters.Add("@Flag", 104);
                parameters.Add("@ClientID", tokenClaims.ClientId);
                parameters.Add("@JsonData", JsonConvert.SerializeObject(payload));

                var data = _dbcontext.Query("SP_UserAuthPermissions", parameters, commandType: CommandType.StoredProcedure);

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

        /**************** Test Default Values ****************/

        #region POST Default Value
        [HttpPost("postTestDefaultValue")]
        public IActionResult postTestDefaultValue([FromHeader] int TestID, [FromHeader] string TestName, [FromHeader] string Values, [FromHeader] string Section, [FromHeader] bool IsDefault)
        {
            try
            {
                string token = Request.Headers["Authorization"];
                token = token.Substring(7);

                var tokenClaims = Fluxion_Handler.GetJWTTokenClaims(token, _key._jwtKey, true);

                var parameters = new DynamicParameters();
                parameters.Add("@Flag", 120);
                parameters.Add("@ClientID", tokenClaims.ClientId);
                parameters.Add("@TestID", TestID);
                parameters.Add("@TestName", TestName);
                parameters.Add("@Value", Values);
                parameters.Add("@Section", Section);
                parameters.Add("@IsDefault", IsDefault);

                var data = _dbcontext.Query("SP_Masters", parameters, commandType: CommandType.StoredProcedure);

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

        #region PUT Default Value
        [HttpPost("putTestDefaultValue")]
        public IActionResult putTestDefaultValue([FromHeader] int TestID, [FromHeader] int ID)
        {
            try
            {
                string token = Request.Headers["Authorization"];
                token = token.Substring(7);

                var tokenClaims = Fluxion_Handler.GetJWTTokenClaims(token, _key._jwtKey, true);

                var parameters = new DynamicParameters();
                parameters.Add("@Flag", 121);
                parameters.Add("@ClientID", tokenClaims.ClientId);
                parameters.Add("@TestID", TestID);
                parameters.Add("@ID", ID);

                var data = _dbcontext.Query("SP_Masters", parameters, commandType: CommandType.StoredProcedure);

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

        #region GET Default Values
        [HttpGet("getTestDefaultValue")]
        public IActionResult getTestDefaultValue()
        {
            try
            {
                string token = Request.Headers["Authorization"];
                token = token.Substring(7);

                var tokenClaims = Fluxion_Handler.GetJWTTokenClaims(token, _key._jwtKey, true);

                var parameters = new DynamicParameters();
                parameters.Add("@Flag", 122);
                parameters.Add("@ClientID", tokenClaims.ClientId);
                var data = _dbcontext.Query("SP_Masters", parameters, commandType: CommandType.StoredProcedure);

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

        /****************** Machine Config ********************/

        #region postMachineConfig
        [HttpPost("postMachineConfig")]
        public IActionResult postMachineConfig([FromHeader] string machineName, [FromHeader] string hostIP, [FromHeader] string hostPort)
        {
            try
            {
                string token = Request.Headers["Authorization"];
                token = token.Substring(7);

                var tokenClaims = Fluxion_Handler.GetJWTTokenClaims(token, _key._jwtKey, true);

                var parameters = new DynamicParameters();
                parameters.Add("@Flag", 124);
                parameters.Add("@ClientID", tokenClaims.ClientId);
                parameters.Add("@MachineName", machineName);
                parameters.Add("@HostIP", hostIP);
                parameters.Add("@HostPort", hostPort);

                var data = _dbcontext.Query("SP_Masters", parameters, commandType: CommandType.StoredProcedure);

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

        #region putMachineConfig
        [HttpPost("putMachineConfig")]
        public IActionResult putMachineConfig([FromHeader] string machineName, [FromHeader] string hostIP)
        {
            try
            {
                string token = Request.Headers["Authorization"];
                token = token.Substring(7);

                var tokenClaims = Fluxion_Handler.GetJWTTokenClaims(token, _key._jwtKey, true);

                var parameters = new DynamicParameters();
                parameters.Add("@Flag", 125);
                parameters.Add("@MachineName", machineName);
                parameters.Add("@HostIP", hostIP);

                var data = _dbcontext.Query("SP_Masters", parameters, commandType: CommandType.StoredProcedure);

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

        #region getMachineConfig
        [AllowAnonymous]
        [HttpGet("getMachineConfig")]
        public IActionResult getMachineConfig([FromHeader] string machineName)
        {
            try
            {
                //string token = Request.Headers["Authorization"];
                //token = token.Substring(7);

                //var tokenClaims = Fluxion_Handler.GetJWTTokenClaims(token, _key._jwtKey, true);

                var parameters = new DynamicParameters();
                parameters.Add("@Flag", 126);
                parameters.Add("@MachineName", machineName);
                var data = _dbcontext.Query("SP_Masters", parameters, commandType: CommandType.StoredProcedure);

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

        #region postAnalyzerData
        [AllowAnonymous]
        [HttpPost("postAnalyzerData")]
        public async Task<IActionResult> postMachineConfig([FromBody] PatientAnalyzerResult _dt)
        {
            try
            {

                var analyzerService = new AnalyzerService(_dbcontext);


                var parameters = new DynamicParameters();
                parameters.Add("@JsonData", JsonConvert.SerializeObject(_dt));

                var data = _dbcontext.Query<MachineConfigDTO>("SP_MachineConfig", parameters, commandType: CommandType.StoredProcedure);
                var config = data.FirstOrDefault();

                var result = await analyzerService.PostAnalyzerData(config.ClientID, config.Sequence,
                    config.InvoiceNo, config.EditNo, JsonConvert.SerializeObject(_dt), false);

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

        /*********** Ficial Year *************/

        #region POST Ficial Year
        [HttpPost("postficialYear")]
        public async Task<IActionResult> postficialYear()
        {
            try
            {
                var data = _dbcontext.Query("SP_FiscalYears", commandType: CommandType.StoredProcedure);

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


        public class MachineConfigDTO
        {
            public int ClientID { get; set; }
            public int Sequence { get; set; }
            public long InvoiceNo { get; set; }
            public int EditNo { get; set; }
        }

        #region OutSource Test Mapping Bulk Insert
        [HttpPost("postOutSourceTestMapping")]
        public IActionResult PostOutSourceTestMapping([FromHeader] long LabID, [FromBody] List<OutSourceTestMappingDto> mappings)
        {
            try
            {
                string token = Request.Headers["Authorization"];
                token = token.Substring(7);

                var tokenClaims = Fluxion_Handler.GetJWTTokenClaims(token, _key._jwtKey, true);

                var parameters = new DynamicParameters();
                parameters.Add("@Flag", 131);
                parameters.Add("@ClientID", tokenClaims.ClientId);
                parameters.Add("@ID", LabID);
                parameters.Add("@JsonData", JsonConvert.SerializeObject(mappings));

                var data = _dbcontext.Query("SP_Masters", parameters, commandType: CommandType.StoredProcedure);

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

        #region OutSource Test Mapping - Save (Alias endpoint)
        [HttpPost("saveOutSourceTests")]
        public IActionResult SaveOutSourceTests([FromHeader] long LabID, [FromHeader] int Sequence, [FromHeader] long InvoiceNo, [FromHeader] int EditNo, [FromBody] List<OutSourceTestDto> mappings)
        {
            try
            {
                string token = Request.Headers["Authorization"];
                token = token.Substring(7);

                var tokenClaims = Fluxion_Handler.GetJWTTokenClaims(token, _key._jwtKey, true);

                var parameters = new DynamicParameters();
                parameters.Add("@Flag", 115);
                parameters.Add("@ClientID", tokenClaims.ClientId);
                parameters.Add("@LabID", LabID);
                parameters.Add("@Sequence", Sequence);
                parameters.Add("@InvoiceNo", InvoiceNo);
                parameters.Add("@EditNo", EditNo);
                parameters.Add("@JsonData", JsonConvert.SerializeObject(mappings));
                parameters.Add("@UserID", tokenClaims.UserId);

                _dbcontext.Execute("SP_TestEntry", parameters, commandType: CommandType.StoredProcedure);

                _response.isSucess = true;
                _response.message = "Success";
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

        #region OutSource Test Summary by Invoice GET
        [HttpGet("getOutSourceTestsByInvoice")]
        public IActionResult GetOutSourceTestsByInvoice([FromHeader] int Sequence, [FromHeader] long InvoiceNo, [FromHeader] int EditNo)
        {
            try
            {
                string token = Request.Headers["Authorization"];
                token = token.Substring(7);

                var tokenClaims = Fluxion_Handler.GetJWTTokenClaims(token, _key._jwtKey, true);

                var parameters = new DynamicParameters();
                parameters.Add("@Flag", 116);
                parameters.Add("@ClientID", tokenClaims.ClientId);
                parameters.Add("@Sequence", Sequence);
                parameters.Add("@InvoiceNo", InvoiceNo);
                parameters.Add("@EditNo", EditNo);

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

        #region OutSource Test Mapping GET
        [HttpGet("getOutSourceTestMapping")]
        public IActionResult GetOutSourceTestMapping([FromHeader] long LabID)
        {
            try
            {
                string token = Request.Headers["Authorization"];
                token = token.Substring(7);

                var tokenClaims = Fluxion_Handler.GetJWTTokenClaims(token, _key._jwtKey, true);

                var parameters = new DynamicParameters();
                parameters.Add("@Flag", 132);
                parameters.Add("@ClientID", tokenClaims.ClientId);
                parameters.Add("@ID", LabID);

                var data = _dbcontext.Query("SP_Masters", parameters, commandType: CommandType.StoredProcedure);

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

        /**************** Expense Category Master ******************/

        #region POST Expense Category
        [HttpPost("postExpenseCategory")]
        public IActionResult PostExpenseCategory([FromHeader] int? CategoryId, [FromHeader] string CategoryName, [FromHeader] bool? IsActive)
        {
            try
            {
                string token = Request.Headers["Authorization"];
                token = token.Substring(7);

                var tokenClaims = Fluxion_Handler.GetJWTTokenClaims(token, _key._jwtKey, true);

                var parameters = new DynamicParameters();
                parameters.Add("@Flag", 133);
                parameters.Add("@ClientID", tokenClaims.ClientId);
                parameters.Add("@ID", CategoryId);
                parameters.Add("@Name", CategoryName);
                parameters.Add("@IsDefault", IsActive);

                var data = _dbcontext.Query("SP_Masters", parameters, commandType: CommandType.StoredProcedure);

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

        #region GET Expense Categories
        [HttpGet("getExpenseCategories")]
        public IActionResult GetExpenseCategories()
        {
            try
            {
                string token = Request.Headers["Authorization"];
                token = token.Substring(7);

                var tokenClaims = Fluxion_Handler.GetJWTTokenClaims(token, _key._jwtKey, true);

                var parameters = new DynamicParameters();
                parameters.Add("@Flag", 134);
                parameters.Add("@ClientID", tokenClaims.ClientId);

                var data = _dbcontext.Query("SP_Masters", parameters, commandType: CommandType.StoredProcedure);

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

        /**************** Pharmacy Master ******************/

        #region POST Pharmacy Data
        [HttpPost("postMasterData")]
        public IActionResult postMasterData([FromHeader] string? Name, [FromHeader] string? Type)
        {
            try
            {
                string token = Request.Headers["Authorization"];
                token = token.Substring(7);

                var tokenClaims = Fluxion_Handler.GetJWTTokenClaims(token, _key._jwtKey, true);

                var parameters = new DynamicParameters();
                parameters.Add("@Flag", 135);
                parameters.Add("@ClientID", tokenClaims.ClientId); 
                parameters.Add("@Type", Type);
                parameters.Add("@Name", Name); 

                var data = _dbcontext.Query("SP_Masters", parameters, commandType: CommandType.StoredProcedure);

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

        #region PUT Pharmacy Data
        [HttpPost("putMasterData")]
        public IActionResult putMasterData([FromHeader] int? ID,[FromHeader] string? Name, [FromHeader] bool? isActive)
        {
            try
            {
                string token = Request.Headers["Authorization"];
                token = token.Substring(7);

                var tokenClaims = Fluxion_Handler.GetJWTTokenClaims(token, _key._jwtKey, true);

                var parameters = new DynamicParameters();
                parameters.Add("@Flag", 136);
                parameters.Add("@ClientID", tokenClaims.ClientId);
                parameters.Add("@ID", ID);
                parameters.Add("@Name", Name);
                parameters.Add("@IsDefault", isActive); 

                var data = _dbcontext.Query("SP_Masters", parameters, commandType: CommandType.StoredProcedure);

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

        #region GET Pharmacy Data
        [HttpGet("getMasterData")]
        public IActionResult getMasterData()
        {
            try
            {
                string token = Request.Headers["Authorization"];
                token = token.Substring(7);

                var tokenClaims = Fluxion_Handler.GetJWTTokenClaims(token, _key._jwtKey, true);

                var parameters = new DynamicParameters();
                parameters.Add("@Flag", 137);
                parameters.Add("@ClientID", tokenClaims.ClientId);
 
                var data = _dbcontext.Query("SP_Masters", parameters, commandType: CommandType.StoredProcedure);

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

        /**************** Manufacture Master ******************/

        #region POST Manufacture Data
        [HttpPost("postManufactureData")]
        public IActionResult postManufactureData([FromBody] ManufactureMaster model)
        {
            try
            {
                string token = Request.Headers["Authorization"];
                token = token.Substring(7);

                var tokenClaims = Fluxion_Handler.GetJWTTokenClaims(token, _key._jwtKey, true);

                var parameters = new DynamicParameters();
                parameters.Add("@Flag", 138);  
                parameters.Add("@ClientID", tokenClaims.ClientId);
                parameters.Add("@UserID", tokenClaims.UserId);

                parameters.Add("@JsonData", JsonConvert.SerializeObject(new
                {
                    model.Code,
                    model.Name,
                    model.ShortName,
                    model.Address,
                    model.Place,
                    model.ContactPerson,
                    model.Number,
                    model.IsActive
                }));

                var data = _dbcontext.Query("SP_Masters", parameters, commandType: CommandType.StoredProcedure);

                _response.isSucess = true;
                _response.message = "Manufacture saved successfully";
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


        #region PUT Manufacture Data
        [HttpPost("putManufactureData")]
        public IActionResult putManufactureData([FromBody] ManufactureMaster model)
        {
            try
            {
                string token = Request.Headers["Authorization"];
                token = token.Substring(7);

                var tokenClaims = Fluxion_Handler.GetJWTTokenClaims(token, _key._jwtKey, true);

                if (model.ID == null)
                {
                    _response.isSucess = false;
                    _response.message = "Manufacture ID is required for update";
                    return BadRequest(_response);
                }

                var parameters = new DynamicParameters();
                parameters.Add("@Flag", 139); // Manufacture PUT
                parameters.Add("@ClientID", tokenClaims.ClientId);
                parameters.Add("@UserID", tokenClaims.UserId);
                parameters.Add("@ID", model.ID);

                parameters.Add("@JsonData", JsonConvert.SerializeObject(new
                {
                    model.Code,
                    model.Name,
                    model.ShortName,
                    model.Address,
                    model.Place,
                    model.ContactPerson,
                    model.Number,
                    model.IsActive
                }));

                var data = _dbcontext.Query("SP_Masters", parameters, commandType: CommandType.StoredProcedure);

                _response.isSucess = true;
                _response.message = "Manufacture updated successfully";
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


        #region GET Manufacture Data
        [HttpGet("getManufactureData")]
        public IActionResult getManufactureData()
        {
            try
            {
                string token = Request.Headers["Authorization"];
                token = token.Substring(7);

                var tokenClaims = Fluxion_Handler.GetJWTTokenClaims(token, _key._jwtKey, true);

                var parameters = new DynamicParameters();
                parameters.Add("@Flag", 140); // Manufacture GET
                parameters.Add("@ClientID", tokenClaims.ClientId); 

                var data = _dbcontext.Query("SP_Masters", parameters, commandType: CommandType.StoredProcedure);

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
