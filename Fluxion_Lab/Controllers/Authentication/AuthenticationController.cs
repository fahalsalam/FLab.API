using Azure;
using Dapper; 
using Fluxion_Lab.Models.General; 
using Fluxion_Lab.Services.OAuth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.Data;
using System.Data.SqlClient;
using System.IdentityModel.Tokens.Jwt;
using System.Text;
using System.Security.Claims;
using Fluxion_Lab.Classes.DBOperations;
using static Fluxion_Lab.Models.RBACL.RBACL;
using Newtonsoft.Json;
using System.Net.Sockets;
using System.Linq;

namespace Fluxion_Lab.Controllers.Authentication
{
    [Route("api/0102")]
    public class AuthenticationController : ControllerBase
    {
        protected APIResponse _response;
        private readonly JwtKey _key;
        private readonly OauthServices _Oauth;
        private readonly IConfiguration _configuration;
        private readonly IDbConnection _dbcontext;
        public AuthenticationController(IOptions<JwtKey> options, IDbConnection dbcontext, IConfiguration configuration)
        {
            this._response = new();
            this._key = options.Value;
            _dbcontext = dbcontext;
            _configuration = configuration;
        }

        // Helper to get correct server string
        private string GetServerString()
        {
            var mode = _configuration["SaaSOptions:Mode"];
            return string.Equals(mode, "OnPrem", StringComparison.OrdinalIgnoreCase)
                ? "103.177.182.183,5734"
                : "localhost";
        }

        #region Login
        [HttpGet("getAuthenticated")]
        public IActionResult Login([FromHeader] string TenantName, [FromHeader] string UserCode, [FromHeader] string Password, [FromHeader] string? sysID)
        {
            try
            {
                var mode = _configuration["SaaSOptions:Mode"];
                if (string.Equals(mode, "SaaS", StringComparison.OrdinalIgnoreCase))
                {
                    // SaaS mode: dynamic tenant connection
                    var metaDbConnStr = $"Server={GetServerString()};Database=db_Fluxion_MetaDB;User Id=FS;Password=Fluxion@FS@987;Encrypt=True;TrustServerCertificate=True;";
                    using var metaConn = new SqlConnection(metaDbConnStr);
                   
                    var tenant = metaConn.QueryFirstOrDefault<dynamic>(
                        "SELECT * FROM [dbo].[mtbl_TenantMasterConnectionConfig] WHERE ClientName = @TenantName",
                        new { TenantName });

                    if (tenant == null)
                    {
                        _response.isSucess = false;
                        _response.message = $"Tenant '{TenantName}' not found.";
                        return NotFound(_response);
                    }
                    if (tenant.IsActive != true && tenant.IsActive != 1)
                    {
                        _response.isSucess = false;
                        _response.message = $"Tenant '{TenantName}' is inactive.";
                        return BadRequest(_response);
                    }

                    string tenantConnStr = $"Server={tenant.Server};Database={tenant.DatabaseName};User Id={tenant.UserId};Password={tenant.Password};Encrypt=True;TrustServerCertificate=True;";
                    
                    using var tenantConn = new SqlConnection(tenantConnStr);

                    var parameters = new DynamicParameters();
                    parameters.Add("@UserCode", UserCode);
                    parameters.Add("@Password", Password);
                    parameters.Add("@SysID", sysID);

                    var data = tenantConn.QueryMultiple("SP_Authetication", parameters, commandType: CommandType.StoredProcedure);

                    if (data is not null)
                    {
                        var _sysInfo = data.Read<dynamic>().ToList();
                        var _clientInfo = data.Read<dynamic>().ToList();
                        var _userInfo = data.Read<dynamic>().ToList();
                        var _generalInfo = data.Read<dynamic>().ToList();
                        var _userAuthInfo = data.Read<dynamic>().ToList();

                        var groupedPermissions = _userAuthInfo
                        .GroupBy(p => (string)p.HeaderName)
                        .Select(headerGroup => new HeaderPermissions
                        {
                            HeaderName = headerGroup.Key,
                            Modules = headerGroup
                                .GroupBy(p => (string)p.ModuleName)
                                .Select(moduleGroup => new ModulePermissions
                                {
                                    ModuleName = moduleGroup.Key,
                                    Permissions = moduleGroup.ToDictionary(
                                        p => (string)p.PermissionName,  
                                        p => (bool)p.IsGranted
                                    )
                                })
                                .ToList()
                        }).ToList();

                        var ClientID = _clientInfo[0].ClientID;
                        var UserID = _userInfo[0].UserID;

                        var refreshToken = Fluxion_Handler.RefreshToken();

                        /** Jwt key Decryption **/
                        var jwtKeyDecrypted = Fluxion_Handler.DecryptString(_key._jwtKey, Fluxion_Handler.APIString);

                        // Format and encrypt connection info
                        string connectionInfo = $"{tenant.Server}|{tenant.DatabaseName}|{tenant.UserId}|{tenant.Password}";
                        string encryptedConnInfo = Fluxion_Handler.EncryptString(connectionInfo, Fluxion_Handler.APIString);

                        /** JWT Token Creation Begin.. **/
                        var tokenHandler = new JwtSecurityTokenHandler();
                        var tokenKey = Encoding.UTF8.GetBytes(jwtKeyDecrypted);
                        var tokenDescriptor = new SecurityTokenDescriptor
                        {
                            Subject = new ClaimsIdentity(new Claim[]
                            {
                                new Claim("xC", ClientID.ToString()),
                                new Claim("xU", UserID.ToString()), 
                                new Claim("xF", encryptedConnInfo)  // Add encrypted connection info
                            }),
                            Expires = DateTime.UtcNow.AddYears(5),
                            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(tokenKey), SecurityAlgorithms.HmacSha256)
                        };

                        var token = tokenHandler.CreateToken(tokenDescriptor);
                        var finalToken = tokenHandler.WriteToken(token);

                        var _data = new
                        {
                            sysInfo = _sysInfo,
                            clientInfo = _clientInfo,
                            userInfo = _userInfo,
                            generalSettings = _generalInfo,
                            _userRoles = groupedPermissions,
                            _accessToken = finalToken,
                            _refreshToken = refreshToken
                        };

                        _response.isSucess = true;
                        _response.message = "Success";
                        _response.data = _data;

                        return Ok(_response);
                    }
                    else
                    {
                        _response.message = "Authentication failed.";
                        _response.isSucess = false;
                        return BadRequest(_response);
                    }
                }
                else // OnPrem mode: use injected dbcontext
                {
                    var parameters = new DynamicParameters();
                    parameters.Add("@UserCode", UserCode);
                    parameters.Add("@Password", Password);
                    parameters.Add("@SysID", sysID);

                    var data = _dbcontext.QueryMultiple("SP_Authetication", parameters, commandType: CommandType.StoredProcedure);

                    if (data is not null)
                    {
                        var _sysInfo = data.Read<dynamic>().ToList();
                        var _clientInfo = data.Read<dynamic>().ToList();
                        var _userInfo = data.Read<dynamic>().ToList();
                        var _generalInfo = data.Read<dynamic>().ToList();
                        var _userAuthInfo = data.Read<dynamic>().ToList();

                        var groupedPermissions = _userAuthInfo
                        .GroupBy(p => (string)p.HeaderName)
                        .Select(headerGroup => new HeaderPermissions
                        {
                            HeaderName = headerGroup.Key,
                            Modules = headerGroup
                                .GroupBy(p => (string)p.ModuleName)
                                .Select(moduleGroup => new ModulePermissions
                                {
                                    ModuleName = moduleGroup.Key,
                                    Permissions = moduleGroup.ToDictionary(
                                        p => (string)p.PermissionName,  
                                        p => (bool)p.IsGranted
                                    )
                                })
                                .ToList()
                        }).ToList();

                        var ClientID = _clientInfo[0].ClientID;
                        var UserID = _userInfo[0].UserID;

                        var refreshToken = Fluxion_Handler.RefreshToken();

                        /** Jwt key Decryption **/
                        var jwtKeyDecrypted = Fluxion_Handler.DecryptString(_key._jwtKey, Fluxion_Handler.APIString);

                        /** JWT Token Creation Begin.. **/
                        var tokenHandler = new JwtSecurityTokenHandler();
                        var tokenKey = Encoding.UTF8.GetBytes(jwtKeyDecrypted);
                        var tokenDescriptor = new SecurityTokenDescriptor
                        {
                            Subject = new ClaimsIdentity(new Claim[]
                            {
                                new Claim("xC", ClientID.ToString()),
                                new Claim("xU", UserID.ToString())
                            }),
                            Expires = DateTime.UtcNow.AddYears(5),
                            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(tokenKey), SecurityAlgorithms.HmacSha256)
                        };

                        var token = tokenHandler.CreateToken(tokenDescriptor);
                        var finalToken = tokenHandler.WriteToken(token);

                        var _data = new
                        {
                            sysInfo = _sysInfo,
                            clientInfo = _clientInfo,
                            userInfo = _userInfo,
                            generalSettings = _generalInfo,
                            _userRoles = groupedPermissions,
                            _accessToken = finalToken,
                            _refreshToken = refreshToken
                        };

                        _response.isSucess = true;
                        _response.message = "Success";
                        _response.data = _data;

                        return Ok(_response);
                    }
                    else
                    {
                        _response.message = "Authentication failed.";
                        _response.isSucess = false;
                        return BadRequest(_response);
                    }
                }
            }
            catch (Exception ex)
            {
                _response.message = ex.Message;
                _response.isSucess = false;
                return BadRequest(_response);
            }
        }
        #endregion

        #region Get App Version
        [AllowAnonymous]
        [HttpGet("getAppVersion")]
        public async Task<IActionResult> GetAppVersion([FromHeader] string deviceID)
        {
            try
            {
                var mode = _configuration["SaaSOptions:Mode"];
                if (string.Equals(mode, "SaaS", StringComparison.OrdinalIgnoreCase))
                {
                    // SaaS mode: join sstbl_DeviceConfig and mtbl_Tenant_Master on ClientID, filter by SystemID
                    var metaDbConnStr = $"Server={GetServerString()};Database=db_Fluxion_MetaDB;User Id=FS;Password=Fluxion@FS@987;Encrypt=True;TrustServerCertificate=True;";
                    using var metaConn = new SqlConnection(metaDbConnStr);

                    string query = @"
                        SELECT TOP 1 
                            '1.0.0' as AppVersion, 
                            tm.ClientName, 
                            dc.ZoomFactor
                        FROM sstbl_DeviceConfig dc
                        INNER JOIN dbo.mtbl_Tenant_Master tm ON dc.ClientID = tm.ClientID
                        CROSS JOIN sstbl_DeviceConfig ac
                        WHERE dc.SystemID = @DeviceID
                    ";

                    var result = await metaConn.QueryFirstOrDefaultAsync<dynamic>(query, new { DeviceID = deviceID });

                    if (result != null)
                    {
                        var Response = new
                        {
                            isSucess = true,
                            message = "Success",
                            data = new
                            {
                                AppVersion = result.AppVersion,
                                ClientName = result.ClientName,
                                ZoomFactor = result.ZoomFactor
                            }
                        };
                        return Ok(Response);
                    }
                    else
                    {
                        var Response = new
                        {
                            isSucess = false,
                            message = "App version not found",
                            data = ""
                        };
                        return Ok(Response);
                    }
                }
                else
                {
                    // OnPrem mode: use injected dbcontext
                    string query = "SELECT AppVersion FROM dbo.sstbl_AppConfig";
                    string ClientName = "SELECT ClientName FROM dbo.mtbl_ClientMaster";
                    string zoomFactor = "SELECT ZoomFactor FROM sstbl_DeviceConfig Where SystemID = @DeviceID";

                    var appConfig = await _dbcontext.QueryFirstOrDefaultAsync<AppConfig>(query);
                    var _clientName = await _dbcontext.QueryFirstOrDefaultAsync<ClientDt>(ClientName);
                    var _zoomFactor = await _dbcontext.QueryFirstOrDefaultAsync<decimal?>(zoomFactor, new { DeviceID = deviceID });

                    if (appConfig != null)
                    {
                        var Response = new
                        {
                            isSucess = true,
                            message = "Success",
                            data = new
                            {
                                AppVersion = appConfig.AppVersion,
                                ClientName = _clientName?.ClientName,
                                ZoomFactor = _zoomFactor
                            }
                        };
                        return Ok(Response);
                    }
                    else
                    {
                        var Response = new
                        {
                            isSucess = false,
                            message = "App version not found",
                            data = ""
                        };
                        return Ok(Response);
                    }
                }
            }
            catch (Exception ex)
            {
                _response.message = ex.Message;
                _response.isSucess = false;
                return BadRequest(_response);
            }
        }
        #endregion

        #region  Get Client Device Details From Server
        [AllowAnonymous]
        [HttpGet("getClientUserDeviceConfig")]
        public IActionResult getClientUserDeviceConfig([FromHeader] string? sysname)
        {
            try
            {
                var metaDB = $"Server={GetServerString()};Database=db_Fluxion_MetaDB;User Id=FS;Password=Fluxion@FS@987;Encrypt=True;TrustServerCertificate=True;";
                using (var connection = new SqlConnection(metaDB))
                {
                    var parameters = new DynamicParameters();
                    parameters.Add("@SysID", sysname);
                    var data = connection.Query("SP_GetClientConfigDetails", parameters, commandType: CommandType.StoredProcedure);

                    if (data is not null)
                    {
                        string sample = JsonConvert.SerializeObject(data);
                        var parameters1 = new DynamicParameters();
                        parameters1.Add("@Flag", 119);
                        parameters1.Add("@JsonData", sample);
                        var data1 = _dbcontext.Query("SP_Masters", parameters1, commandType: CommandType.StoredProcedure);

                        _response.isSucess = true;
                        _response.message = "Success";
                        _response.data = data1;
                        return Ok(_response);
                    }
                    else
                    {
                        _response.message = "something went wrong";
                        _response.isSucess = false;
                        return BadRequest(_response);
                    }
                }

            }
            catch (Exception ex)
            {
                _response.message = ex.Message;
                _response.isSucess = false;
                return BadRequest(_response);
            }
        }
        #endregion

        #region Test Server
        [AllowAnonymous]
        [HttpGet("testApiServer")]
        public async Task<IActionResult> TestAPIServer()
        {
            return Ok("Running Sucess");

        }
        #endregion

        public class AppConfig
        {
            public string AppVersion { get; set; }
        }
        public class ClientDt
        {
            public string ClientName { get; set; }
        }

    }
}
