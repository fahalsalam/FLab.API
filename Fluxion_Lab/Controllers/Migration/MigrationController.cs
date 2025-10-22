using Azure;
using Dapper;
using Fluxion_Lab.Models.General;
using Fluxion_Lab.Models.Masters.TestGroupMaster;
using Fluxion_Lab.Models.Masters.TestMaster;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MySqlConnector;
using Newtonsoft.Json;
using System.Data;
using System.Data.SqlClient; 
using static Fluxion_Lab.Controllers.Authentication.AuthenticationController;

namespace Fluxion_Lab.Controllers.Migration
{
    public class MigrationController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly IDbConnection _dbcontext;
        protected APIResponse _response;

        public MigrationController(IConfiguration configuration, IDbConnection dbcontext, APIResponse response)
        {
            _configuration = configuration;
            _dbcontext = dbcontext;
            _response = response;
        }

        private IDbConnection CreateConnection()
        {
            var connectionString = _configuration.GetConnectionString("MySqlConnection");
            return new MySqlConnection(connectionString);
        }

        #region Master Data Migration
        [AllowAnonymous]
        [HttpGet("postMasterData")]
        public async Task<IActionResult> PostMasterData()
        {
            const string TestMaster = @"
                            SELECT 
                                `TestID` AS `TestID`,
                                `Name` AS `TestName`,
                                `Section`,
                                `Rate`,
                                `TestCode`,
                                `Units` AS `Unit`,
                                `NormalRanges` AS `NormalRange`,
                                `Methord` AS `Method`,
                                CAST(`LowValue` AS DECIMAL(18,2)) AS `LowValue`,
                                CAST(`HighValue` AS DECIMAL(18,2)) AS `HighValue`,
                                `Specimen`,
                                CAST(`ReagentUnits` AS DECIMAL(18,2)) AS `MinReagentValue`,
                                '' AS `MinReagentUnit`,
                                `Order` AS `ItemNo`,
                                NULL AS `Discount`,
                                '' AS `MachineName`
                            FROM `family`.`testdetails`";

            const string TestGroup = @"SELECT * FROM family.testgroup";
            const string PatientMaster = @"SELECT * FROM family.customer";


            using (var connection = CreateConnection())
            {
                try
                {
                    var _testMaster = await connection.QueryAsync<TestMasterMigration>(TestMaster);

                    var _testGroup = await connection.QueryAsync(TestGroup);

                    var _patentMaster = await connection.QueryAsync(PatientMaster);

                    var testGroups = _testGroup.Select(item => new TestGroupMaster.TestGroupsMigration
                    {
                        GroupID = item.ID,
                        GroupName = item.GroupName,
                        Section = item.Section,
                        GroupCode = item.GroupCode,
                        Rate = item.Rate,
                        MachineName = item.MachineName,
                        ShowInReport = item.Show == 1, // Assuming 'Show' is an integer in DB (0 or 1)
                        TestIDs = ProcessTestIDs(item.ID, item.Tests) // Parse the Tests field
                    }).ToList();


                    var parameters = new DynamicParameters();
                    parameters.Add("@ClientID", 1001);
                    parameters.Add("@TestMasterJSONDATA",  null)/*JsonConvert.SerializeObject(_testMaster)*/;
                    parameters.Add("@TestGroupJSONDATA", null); /*JsonConvert.SerializeObject(testGroups)*/
                    parameters.Add("@PatientMasterJSONDATA", JsonConvert.SerializeObject(_patentMaster));

                    var data = _dbcontext.Query("SP_DataMigration", parameters, commandType: CommandType.StoredProcedure, commandTimeout: 600);

                    return Ok("Sucess");

                }
                catch (Exception ex)
                {
                    return StatusCode(500, $"Error: {ex.Message}");
                }

            }
        }
        #endregion

        #region Transaction Data Migration
        [HttpGet("postTransactionData")]
        public async Task<IActionResult> PostTransactionData()
        {
            const string Bills = @"SELECT * FROM sinu.bill where Date >= '2022-01-01';";
            const string BillReceipt = @"SELECT * FROM sinu.billreceipt where  Date >= '2022-01-01' ORDER BY BillNo ASC";
            const string Tests = @"
                        SELECT 
                            *,
                            CAST(
                                CASE 
                                    WHEN LEFT(TestID, 1) = 'G' THEN SUBSTRING(TestID, 2) -- Remove the 'G' and keep the rest
                                    ELSE TestID -- For numeric TestID, keep it as is
                                END 
                            AS UNSIGNED) AS `FluTestID`,  
                            CASE 
                                WHEN LEFT(TestID, 1) = 'G' THEN 'G' -- If starts with 'G', Type = 'G'
                                ELSE 'T' -- Otherwise, Type = 'T'
                            END AS `Type`
                        FROM 
                            sinu.tests  A
                            INNER JOIN sinu.bill B ON A.BillNo = B.BillNo
                        where 
	                        B.Date >= '2022-01-01';";

            using (var connection = CreateConnection())
            {
                try
                {
                    // Fetch data
                    var _bills = await connection.QueryAsync(Bills);
                    var _billReceipt = await connection.QueryAsync(BillReceipt);
                    var _tests = await connection.QueryAsync(Tests);

                    // Batch size for each group
                    const int batchSize = 5000;

                    // Process bills first
                    var billBatches = _bills
                        .Select((item, index) => new { item, index })
                        .GroupBy(x => x.index / batchSize)
                        .Select(g => g.Select(x => x.item));

                    foreach (var billBatch in billBatches)
                    {
                        var parameters = new DynamicParameters();
                        parameters.Add("@ClientID", 1001);
                        parameters.Add("@Bills", JsonConvert.SerializeObject(billBatch));
                        parameters.Add("@Receipts", null); // No receipts for now
                        parameters.Add("@Tests", null);    // No tests for now

                        _dbcontext.Execute(
                           "SP_DataMigration",
                           parameters,
                           commandType: CommandType.StoredProcedure,
                           commandTimeout: 1800);
                    }

                    // Process receipts next
                    var receiptBatches = _billReceipt
                        .Select((item, index) => new { item, index })
                        .GroupBy(x => x.index / batchSize)
                        .Select(g => g.Select(x => x.item));

                    foreach (var receiptBatch in receiptBatches)
                    {
                        var parameters = new DynamicParameters();
                        parameters.Add("@ClientID", 1001);
                        parameters.Add("@Bills", null);    // Bills already processed
                        parameters.Add("@Receipts", JsonConvert.SerializeObject(receiptBatch));
                        parameters.Add("@Tests", null);    // No tests for now

                        await _dbcontext.ExecuteAsync(
                            "SP_DataMigration",
                            parameters,
                            commandType: CommandType.StoredProcedure,
                            commandTimeout: 600);
                    }

                    // Process tests last
                    var testBatches = _tests
                        .Select((item, index) => new { item, index })
                        .GroupBy(x => x.index / batchSize)
                        .Select(g => g.Select(x => x.item));

                    foreach (var testBatch in testBatches)
                    {
                        var parameters = new DynamicParameters();
                        parameters.Add("@ClientID", 1001);
                        parameters.Add("@Bills", null);    // Bills already processed
                        parameters.Add("@Receipts", null); // Receipts already processed
                        parameters.Add("@Tests", JsonConvert.SerializeObject(testBatch));

                        _dbcontext.Execute(
                           "SP_DataMigration",
                           parameters,
                           commandType: CommandType.StoredProcedure,
                           commandTimeout: 1800);
                    }

                    return Ok("Data migration completed successfully.");
                }
                catch (Exception ex)
                {
                    return StatusCode(500, $"Error: {ex.Message}");
                }
            }
        }
        #endregion

        private List<TestGroupMaster.TestIDMigration> ProcessTestIDs(int groupID, string tests)
        {
            if (string.IsNullOrEmpty(tests))
                return new List<TestGroupMaster.TestIDMigration>();

            return tests.Split(',', StringSplitOptions.RemoveEmptyEntries)
                        .Select(test =>
                        {
                            var type = test.StartsWith("G", StringComparison.OrdinalIgnoreCase) ? "G" : "T";
                            return new TestGroupMaster.TestIDMigration
                            {
                                GroupID = groupID,
                                TestId = long.Parse(test.Substring(1)), // Remove the prefix 'T' or 'G' and parse the ID
                                Type = type
                            };
                        }).ToList();
        }
    }
}
