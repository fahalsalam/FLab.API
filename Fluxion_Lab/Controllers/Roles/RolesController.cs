using System.Data;
using Dapper;
using Fluxion_Lab.Classes.DBOperations;
using Fluxion_Lab.Models.General;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace Fluxion_Lab.Controllers.Roles
{
    [Route("api/roles")]
    [Authorize]
    public class RolesController : ControllerBase
    {
        private readonly JwtKey _key;
        private readonly IDbConnection _dbcontext;
        protected APIResponse _response;

        public RolesController(IOptions<JwtKey> options, IDbConnection dbcontext, APIResponse response)
        {
            _key = options.Value;
            _dbcontext = dbcontext;
            _response = response;
        }

        // DTO for posting multiple modules under a single header
        public class ModulesRequest
        {
            public int? HeaderID { get; set; }
            public string? HeaderName { get; set; }
            public List<string> ModuleNames { get; set; } = new();
        }

        // POST: api/roles/postHeader
        [AllowAnonymous]
        [HttpPost("postHeader")]
        public IActionResult PostHeader([FromHeader] string headerName)
        {
            if (string.IsNullOrWhiteSpace(headerName))
            {
                _response.isSucess = false;
                _response.message = "Header name is required.";
                return BadRequest(_response);
            }

            try
            {
                if (_dbcontext.State != ConnectionState.Open)
                    _dbcontext.Open();

                using var transaction = _dbcontext.BeginTransaction();

                // Check for duplicate header name
                const string checkSql = "SELECT COUNT(1) FROM [dbo].[sstbl_AuthHeaders] WHERE [HeaderName] = @HeaderName";
                var exists = _dbcontext.ExecuteScalar<int>(checkSql, new { HeaderName = headerName }, transaction);
                if (exists > 0)
                {
                    transaction.Rollback();
                    _response.isSucess = false;
                    _response.message = "Header already exists.";
                    return Conflict(_response);
                }

                // Calculate next HeaderID as MAX + 1 within the same transaction to avoid races
                const string idSql = "SELECT ISNULL(MAX([HeaderID]), 0) + 1 FROM [dbo].[sstbl_AuthHeaders] WITH (HOLDLOCK, UPDLOCK)";
                int nextId = _dbcontext.ExecuteScalar<int>(idSql, transaction: transaction);

                const string insertHeaderSql = @"INSERT INTO [dbo].[sstbl_AuthHeaders] ([HeaderID], [HeaderName])
                                           VALUES (@HeaderID, @HeaderName);";
                _dbcontext.Execute(insertHeaderSql, new { HeaderID = nextId, HeaderName = headerName }, transaction);

                transaction.Commit();

                _response.isSucess = true;
                _response.message = "Success";
                _response.data = new { HeaderID = nextId, HeaderName = headerName };
                return Ok(_response);
            }
            catch (Exception ex)
            {
                _response.isSucess = false;
                _response.message = ex.Message;
                return StatusCode(500, _response);
            }
        }

        // POST: api/roles/postModules
        // Accepts a list of module names for a single header (by HeaderID or HeaderName)
        [AllowAnonymous]
        [HttpPost("postModules")]
        public IActionResult PostModules([FromBody] ModulesRequest request)
        {
            try
            {
                // Validate input
                if ((request.HeaderID == null || request.HeaderID <= 0) && string.IsNullOrWhiteSpace(request.HeaderName))
                {
                    _response.isSucess = false;
                    _response.message = "Either HeaderID or HeaderName is required.";
                    return BadRequest(_response);
                }

                var cleanedNames = request.ModuleNames
                    .Where(n => !string.IsNullOrWhiteSpace(n))
                    .Select(n => n.Trim())
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();

                if (cleanedNames.Count == 0)
                {
                    _response.isSucess = false;
                    _response.message = "ModuleNames cannot be empty.";
                    return BadRequest(_response);
                }

                if (_dbcontext.State != ConnectionState.Open)
                    _dbcontext.Open();

                using var transaction = _dbcontext.BeginTransaction();

                // Resolve HeaderID if only HeaderName provided
                int headerId;
                if (request.HeaderID.HasValue && request.HeaderID.Value > 0)
                {
                    headerId = request.HeaderID.Value;
                    const string chkHeader = "SELECT COUNT(1) FROM [dbo].[sstbl_AuthHeaders] WHERE [HeaderID] = @HeaderID";
                    var hdrExists = _dbcontext.ExecuteScalar<int>(chkHeader, new { HeaderID = headerId }, transaction);
                    if (hdrExists == 0)
                    {
                        transaction.Rollback();
                        _response.isSucess = false;
                        _response.message = "Header not found.";
                        return NotFound(_response);
                    }
                }
                else
                {
                    const string getHeader = "SELECT TOP 1 [HeaderID] FROM [dbo].[sstbl_AuthHeaders] WHERE [HeaderName] = @HeaderName";
                    var got = _dbcontext.ExecuteScalar<int?>(getHeader, new { HeaderName = request.HeaderName }, transaction);
                    if (got == null)
                    {
                        transaction.Rollback();
                        _response.isSucess = false;
                        _response.message = "Header not found.";
                        return NotFound(_response);
                    }
                    headerId = got.Value;
                }

                // Check for duplicates against existing rows
                const string dupSql = "SELECT [ModuleName] FROM [dbo].[sstbl_AuthModules] WHERE [HeaderID] = @HeaderID AND [ModuleName] IN @ModuleNames";
                var dupes = _dbcontext.Query<string>(dupSql, new { HeaderID = headerId, ModuleNames = cleanedNames }, transaction).ToList();
                if (dupes.Any())
                {
                    transaction.Rollback();
                    _response.isSucess = false;
                    _response.message = "One or more modules already exist for the header.";
                    _response.data = new { duplicates = dupes };
                    return Conflict(_response);
                }

                // Insert without explicit ModuleID (identity handled by DB)
                const string insertModuleSql = @"INSERT INTO [dbo].[sstbl_AuthModules] ([HeaderID], [ModuleName])
                                                OUTPUT INSERTED.[ModuleID]
                                                VALUES (@HeaderID, @ModuleName);";

                var inserted = new List<object>();
                foreach (var name in cleanedNames)
                {
                    int moduleId = _dbcontext.ExecuteScalar<int>(insertModuleSql, new { HeaderID = headerId, ModuleName = name }, transaction);
                    inserted.Add(new { ModuleID = moduleId, HeaderID = headerId, ModuleName = name });
                }

                transaction.Commit();

                _response.isSucess = true;
                _response.message = "Success";
                _response.data = inserted;
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
