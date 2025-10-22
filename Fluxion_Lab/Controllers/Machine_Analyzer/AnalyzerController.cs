using Dapper;
using Fluxion_Lab.Classes.DBOperations;
using Fluxion_Lab.Models.General;
using Fluxion_Lab.Services.Masters;
using Microsoft.AspNetCore.DataProtection.KeyManagement;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Amqp.Framing;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using System.Data;
using static Fluxion_Lab.Models.Masters.Machine_Analyzer.MachineAnalyzer;
using static Org.BouncyCastle.Math.EC.ECCurve;

namespace Fluxion_Lab.Controllers.Machine_Analyzer
{
    [Route("api/0909")] 
    public class AnalyzerController : ControllerBase
    {
        private readonly JwtKey _key;
        private readonly IDbConnection _dbcontext;
        protected APIResponse _response;

        public AnalyzerController(IOptions<JwtKey> options, IDbConnection dbcontext, APIResponse response)
        {
            this._key = options.Value;
            _dbcontext = dbcontext;
            _response = response;
        }

        #region POST Analyzer Tests Mapping
        [HttpPost("postAnalyzerTestMapping")]
        public IActionResult postAnalyzerTestMapping([FromHeader] int? DeviceID,[FromBody] List<AnalyzerTestMappingDto> _test)
        {
            try
            {
                string token = Request.Headers["Authorization"];
                token = token.Substring(7);

                var tokenClaims = Fluxion_Handler.GetJWTTokenClaims(token, _key._jwtKey, true);

                var parameters = new DynamicParameters();
                parameters.Add("@Flag", 127);
                parameters.Add("@ClientID", tokenClaims.ClientId);
                parameters.Add("@JSONDATA", JsonConvert.SerializeObject(_test));
                parameters.Add("@DeviceID", DeviceID);

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

        #region GET Analyzer Test Mapping
        [HttpGet("getAnalyzerTestMapping")]
        public IActionResult getAnalyzerTestMapping([FromHeader] int? DeviceID)
        {
            try
            {
                string token = Request.Headers["Authorization"];
                token = token.Substring(7);

                var tokenClaims = Fluxion_Handler.GetJWTTokenClaims(token, _key._jwtKey, true);

                var parameters = new DynamicParameters();
                parameters.Add("@Flag", 128);
                parameters.Add("@ClientID", tokenClaims.ClientId);
                parameters.Add("@DeviceID", DeviceID);

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

        #region GET Analyzer Devices
        [HttpGet("getAnalyzerDevices")]
        public IActionResult getAnalyzerDevices()
        {
            try
            {
                string token = Request.Headers["Authorization"];
                token = token.Substring(7);

                var tokenClaims = Fluxion_Handler.GetJWTTokenClaims(token, _key._jwtKey, true);

                var parameters = new DynamicParameters();
                parameters.Add("@Flag", 129);
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

        #region GET Analyzer Devices Mappings
        [HttpGet("getAnalyzerDevicesMappings")]
        public IActionResult getAnalyzerDevicesMappings()
        {
            try
            {
                //string token = Request.Headers["Authorization"];
                //token = token.Substring(7);

                //var tokenClaims = Fluxion_Handler.GetJWTTokenClaims(token, _key._jwtKey, true);

                var parameters = new DynamicParameters();
                parameters.Add("@Flag", 130);
                //parameters.Add("@ClientID", tokenClaims.ClientId);

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

        #region POST Barcode Analyzer 
        [HttpPost("postBarcodeAnalyzer")]   
        public IActionResult postBarcodeAnalyzer([FromHeader] int InvoiceNo, [FromHeader] int Sequence, [FromHeader] int EditNo,
            [FromBody] List<BarcodeTest> _test)
        {
            try
            {
                string token = Request.Headers["Authorization"];
                token = token.Substring(7);
                
                var tokenClaims = Fluxion_Handler.GetJWTTokenClaims(token, _key._jwtKey, true);
                var parameters = new DynamicParameters(); 
                
                parameters.Add("@ClientID", tokenClaims.ClientId); 
                parameters.Add("@Flag", 100);
                parameters.Add("@Sequence", Sequence);
                parameters.Add("@InvoiceNo", InvoiceNo);
                parameters.Add("@EditNo", EditNo); 
                parameters.Add("@JsonData", JsonConvert.SerializeObject(_test));
                
                var data = _dbcontext.Query("SP_BarcodeAnalyzerTestDetails", parameters, commandType: CommandType.StoredProcedure);
               
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

        #region Send Data to Analyzer Machine
        [HttpGet("getAnalyzerBarcodeData")]
        public IActionResult getAnalyzerBarcodeData([FromQuery] string device_id = "")
        {
            try
            {
                // Get raw data from database
                var rawData = _dbcontext.Query<RawAnalyzerData>(@"
                SELECT 
                    A.Barcode,B.PatientID as patient_id, B.PatientName, '' as Gender,'' as SpecimenType, 
                    A.AnalyzerMachineName,A.TestName,1 as DeviceID
                FROM
                   [dbo].[trntbl_BarcodeMachineData] A
                   JOIN [dbo].[trntbl_TestEntriesHdr] B ON A.ClientID = B.ClientID  AND A.Sequence = B.Sequence  AND A.InvoiceNo = B.InvoiceNo AND A.EditNo = B.EditNo
                ORDER BY 
                    A.Barcode, A.AnalyzerMachineName");

                // Group and transform data using LINQ
                var groupedData = rawData
                    .GroupBy(x => new {
                        x.Barcode,
                        x.patient_id,
                        x.DeviceID
                    })
                    .Select(g => new AnalyzerDeviceResponse
                    {
                        BarcodeNo = g.Key.Barcode,
                        patient_id = g.Key.patient_id,
                        PatientName = g.First().PatientName,
                        Gender = g.First().Gender ?? string.Empty,
                        SpecimenType = g.First().SpecimenType ?? string.Empty,
                        DeviceId = g.Key.DeviceID,
                        Assays = g.Select(item => new AssayInfo
                        {
                            OnlineTestCode = item.AnalyzerMachineName,
                            OnlineTestName = item.TestName
                        }).ToList()
                    })
                    .ToList();

                // Update IsResultSent to true for all processed records
                //if (rawData.Any())
                //{
                //    var barcodes = rawData.Select(x => x.Barcode).Distinct().ToArray();

                //    // Create a temp table or use table-valued parameter
                //    var updateQuery = @"
                //    UPDATE [dbo].[trntbl_BarcodeMachineData] 
                //    SET IsResultSent = 1 
                //    WHERE Barcode IN @barcodes";

                //    _dbcontext.Execute(updateQuery, new { barcodes });
                //}

                _response.isSucess = true;
                _response.message = "Success";
                _response.data = groupedData;

                return Ok(groupedData);
            }
            catch (Exception ex)
            {
                _response.isSucess = false;
                _response.message = ex.Message;

                return StatusCode(500, _response);
            }
        }
        #endregion

        #region POST Analyzer Results Barcode Data
        [HttpPost("postAnalyzerResultsBarcodeData")]
        public async Task<IActionResult> postAnalyzerResultsBarcodeData([FromBody] List<DeviceResult> _test)
        {
            try
            {

                var analyzerService = new AnalyzerService(_dbcontext);

                var parameters = new DynamicParameters();
                
                parameters.Add("@Flag", 101);
                parameters.Add("@JsonData", JsonConvert.SerializeObject(_test));
                
                var data = _dbcontext.QueryMultiple("SP_BarcodeAnalyzerTestDetails", parameters, commandType: CommandType.StoredProcedure);

                var invDt = data.Read<InvoiceDetails>().ToList();
                var resultDetails = data.Read<ResultDetails>().ToList();  

                var firstInvoice = invDt.FirstOrDefault();
               
                if (firstInvoice != null)
                { 
                    // With this corrected block:
                    await analyzerService.PostAnalyzerData(
                        Convert.ToInt16(firstInvoice.ClientID),
                        Convert.ToInt16(firstInvoice.Sequence),
                        Convert.ToInt16(firstInvoice.InvoiceNo),
                        Convert.ToInt16(firstInvoice.EditNo),
                        JsonConvert.SerializeObject(resultDetails),true
                    );  
                }
                else
                {
                    _response.isSucess = false;
                    _response.message = "No invoice details found.";
                    return StatusCode(500, _response);
                }

                _response.isSucess = true;
                _response.message = "result value updated sucessfully";
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
    }
}
