using Dapper;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Data;
using System.Linq;
using Fluxion_Lab.Controllers.Transactions;
using static Fluxion_Lab.Models.Transactions.TestEntries.TestEntries;
using static Fluxion_Lab.Controllers.Transactions.TransactionsController;

namespace Fluxion_Lab.Services.Masters
{
    public class AnalyzerService
    {
        private readonly IDbConnection _dbcontext;

        public AnalyzerService(IDbConnection dbcontext)
        {
            _dbcontext = dbcontext;
        }

        public async Task<IActionResult> PostAnalyzerData(int ClientID, int sequence, long InvoiceNo, int editNo,string AnalayzerJsonData,bool? isfromBarcode )
        {
            try
            {
                List<HierarchyEntry> resultData = new List<HierarchyEntry>();
                List<ResultEntryLines> resultData1 = new List<ResultEntryLines>(); // Reset for each entry

                // Step 2: Fetch result entry data with hierarchy
                var result = await GetHirarachy(ClientID.ToString(), sequence, InvoiceNo, editNo);

                if (result == null)
                {
                    Console.WriteLine($"No result entries found for InvoiceNo {InvoiceNo}");
                    return new NotFoundObjectResult(new { isSuccess = false, message = "No result entries found." });
                }

                resultData.AddRange(result);

                foreach (var hierarchyEntry in result)
                {
                    ExtractHierarchyEntries(hierarchyEntry, resultData1);
                }

                // Step 4: Update UniqueID in the database
                var resultEntry = new ResultEntry { Results = resultData1 };
                
                postTestEntryResult1(ClientID.ToString(), InvoiceNo, sequence, editNo, resultEntry, AnalayzerJsonData, isfromBarcode);

                Console.WriteLine($"Unique IDs updated for InvoiceNo {InvoiceNo}");

                // Clear resultData1 to prevent accumulation across invoices
                resultData1.Clear();

                return new OkObjectResult(new { isSuccess = true, message = "Unique IDs updated successfully." });
            }
            catch (Exception ex)
            {
                return new ObjectResult(new { isSuccess = false, message = ex.Message }) { StatusCode = 500 };
            }
        }

        private async Task<List<HierarchyEntry>> GetHirarachy(string clientID, int sequence, long invoiceNo, int editNo)
        {

            string baseQuery = @"
                SELECT 
                     ID,Name,Type
                    ,CASE WHEN A.Type = 'G' THEN 'Group' ELSE 'Test' END AS [LabelName]
                    ,CASE WHEN A.Type = 'G' THEN '' ELSE ISNULL(B.[Values],'') END AS [Value]
                    ,ISNULL(B.[Action],'') AS [Action]
                    ,CASE WHEN A.Type = 'G' THEN '' ELSE ISNULL(B.Comments,'') END AS [Comments] 
                    ,CASE WHEN A.Type = 'G' THEN ISNULL(G.Section,'') ELSE ISNULL(T.Section,'') END AS [TestSection]
                    ,CASE WHEN A.Type = 'G' THEN 0.00 ELSE ISNULL(T.HighValue,0.00) END AS HighValue 
                    ,CASE WHEN A.Type = 'G' THEN 0.00 ELSE ISNULL(T.LowValue,0.00) END AS LowValue 
                    ,CASE WHEN A.Type = 'G' THEN ISNULL(G.MachineName,'') ELSE ISNULL(T.MachineName,'') END AS MachineName 
                    ,CASE WHEN A.Type = 'G' THEN '' ELSE ISNULL(T.Methord,'') END AS Method 
                    ,CASE WHEN A.Type = 'G' THEN '' ELSE ISNULL(T.Unit,'') END AS Unit
                    ,CASE WHEN A.Type = 'G' THEN '' ELSE ISNULL(T.NormalRange,'') END AS NormalRange
                    ,CASE WHEN A.Type = 'G' THEN ISNULL(GroupCode,'') ELSE ISNULL(T.TestCode,'') END AS TestCode 
                    ,CASE WHEN A.Type = 'G' THEN '' ELSE ISNULL(T.Specimen,'') END AS Specimen  
                    ,ISNULL(A.[TransactionUniqueID],'') AS [UniqueID]
                    ,'H' as [TransType]
                FROM 
                    [dbo].[trntbl_TestEntriesLine] A
                    LEFT JOIN [dbo].[trntbl_TestEntriesResults] B ON A.ClientID = B.ClientID and A.Sequence = B.Sequence and A.InvoiceNo = B.InvoiceNo and A.EditNo = B.EditNo and A.ID = B.TestID and A.[TransactionUniqueID] = B.UniqueID
                    LEFT JOIN [dbo].[mtbl_TestMaster] T ON T.ClientID = A.ClientID and T.TestID = A.ID and A.Type = 'T'
                    LEFT JOIN [dbo].[mtbl_TestGroupMaster] G ON G.ClientID = A.ClientID and G.GroupID = A.ID and A.Type = 'G'                    
                WHERE
                    A.ClientID = @ClientID AND A.[Sequence] = @Sequence AND A.InvoiceNo = @InvoiceNo AND A.EditNo = @EditNo ORDER BY A.SI_No";

            var baseResults = _dbcontext.Query<BaseEntry>(baseQuery, new
            {
                ClientID = clientID,
                Sequence = sequence,
                InvoiceNo = invoiceNo,
                EditNo = editNo
            });

            // Process each entry and fetch hierarchy for groups
            var result = new List<HierarchyEntry>();

            foreach (var entry in baseResults)
            {
                var hierarchyEntry = new HierarchyEntry
                {
                    ID = entry.ID,
                    Name = entry.Name,
                    Type = entry.Type,
                    LabelType = entry.LabelName,
                    Value = entry.Value,
                    Action = entry.Action,
                    Comments = entry.Comments,
                    TestSection = entry.TestSection,
                    HighValue = entry.HighValue,
                    LowValue = entry.LowValue,
                    MachineName = entry.MachineName,
                    Method = entry.Method,
                    Unit = entry.Unit,
                    NormalRange = entry.NormalRange,
                    TestCode = entry.TestCode,
                    Specimen = entry.Specimen,
                    UniqueID = entry.UniqueID,
                    TransType = entry.TransType,
                    SubArray = entry.Type == "G" ? await GetGroupHierarchyUniqueID((int)entry.ID, clientID, sequence, invoiceNo, editNo) : new List<HierarchyEntry>()
                };

                result.Add(hierarchyEntry);
            }

            return result;
        }

        public async Task<List<HierarchyEntry>> GetGroupHierarchyUniqueID(int groupId, string clientId, int sequence, long invoiceNo, int editNo)
        {
            try
            {
                string hierarchyQuery = @"
                WITH CTE_Hierarchy AS (
                    SELECT 
                        A.ClientID, A.GroupID, A.ChildGroupID, A.TestID, A.Type, A.LastModified,
                        CAST(A.GroupID AS NVARCHAR(MAX)) AS Path,CAST(A.UniqueID AS NVARCHAR(100)) UniqueID,
                        1 AS Level,A.SortOrder
                    FROM mtbl_TestGroupMappings A
                    WHERE A.GroupID = @GroupID
                    UNION ALL
                    SELECT 
                        mt.ClientID, mt.GroupID, mt.ChildGroupID, mt.TestID, mt.Type, mt.LastModified,
                        CAST(ch.Path + '->' + CAST(mt.GroupID AS NVARCHAR(MAX)) AS NVARCHAR(MAX)) AS Path,CAST(mt.UniqueID AS NVARCHAR(100)),
                        ch.Level + 1 AS Level,mt.SortOrder
                    FROM mtbl_TestGroupMappings mt
                    INNER JOIN CTE_Hierarchy ch ON mt.GroupID = ch.ChildGroupID
                )
                SELECT 
                    GroupID AS ID,
                    '' AS Name, 
                    ChildGroupID AS ChildID,
                    A.TestID AS TestID,
                    Type,SortOrder,UniqueID
                FROM 
                    CTE_Hierarchy A
                    
                Order by
                    SortOrder";


                var flatHierarchy = await _dbcontext.QueryAsync<HierarchyEntry>(hierarchyQuery, new
                {
                    GroupID = groupId
                });

                var sortedHierarchy = flatHierarchy
                                    .OrderBy(x => x.SortOrder)
                                    .ToList();

                return BuildHierarchyUniqueID(sortedHierarchy, groupId, clientId, sequence, invoiceNo, editNo);
            }
            catch (Exception ex)
            {

                throw ex;
            }
        }

        private List<HierarchyEntry> BuildHierarchyUniqueID(IEnumerable<HierarchyEntry> flatData, int groupId, string clientId, int sequence, long invoiceNo, int editNo)
        {
            try
            {
                // Group the data by parent-child relationships
                var groupDictionary = flatData.ToLookup(x => x.ID);

                // Recursive function to build the hierarchy
                List<HierarchyEntry> BuildHierarchyRecursiveUniqueID(int groupId)
                {
                    var children = new List<HierarchyEntry>();

                    foreach (var x in groupDictionary[groupId])
                    {
                        HierarchyEntry entry;

                        if (x.Type == "G")
                        {
                            string uniqueID = x.UniqueID;

                            // Check if the UniqueID exists in the database
                            string existingUniqueID = _dbcontext.QueryFirstOrDefault<string>(
                                @"SELECT [UniqueID] 
                              FROM [dbo].[trntbl_TestEntriesResults] B 
                              WHERE 
                                  B.ClientID = @ClientID 
                                  AND B.Sequence = @Sequence 
                                  AND B.InvoiceNo = @InvoiceNo 
                                  AND B.EditNo = @EditNo AND B.TestID = @ID
                                  AND B.[UniqueID] = @UniqueID",
                                                    new
                                                    {
                                                        ClientID = clientId,
                                                        Sequence = sequence,
                                                        InvoiceNo = invoiceNo,
                                                        EditNo = editNo,
                                                        UniqueID = uniqueID,
                                                        ID = x.ChildID ?? x.ID
                                                    });

                            // Query based on whether UniqueID exists
                            string groupQuery;

                            if (existingUniqueID != null)
                            {
                                // If the UniqueID exists, use it directly
                                groupQuery = @"
                            SELECT 
                                G.GroupID AS ID,
                                CASE WHEN B.[Values] IS NULL THEN G.GroupName ELSE B.TestName END AS Name,
                                'Group' AS LabelType,
                                ISNULL(B.[Values], '') AS [Value],
                                ISNULL(B.[Action], '') AS [Action],
                                ISNULL(B.Comments, '') AS Comments,
                                ISNULL(G.Section, '') AS TestSection,
                                0.00 AS HighValue,
                                0.00 AS LowValue,
                                ISNULL(G.MachineName, '') AS MachineName,
                                '' AS Methord,
                                '' AS Unit,
                                '' AS NormalRange,
                                ISNULL(GroupCode, '') AS TestCode,
                                '' AS Specimen,
                                GM.SortOrder AS SortOrder,
                                @uniqueID AS [UniqueID],
                                'L' AS [TransType]
                            FROM 
                                [dbo].[mtbl_TestGroupMaster] G
                            LEFT JOIN 
                                [dbo].[trntbl_TestEntriesResults] B  ON B.ClientID = @ClientID  AND B.Sequence = @Sequence AND B.InvoiceNo = @InvoiceNo AND B.EditNo = @EditNo AND G.GroupID = B.TestID
                                AND B.TransType = 'L'
                            LEFT JOIN
                                mtbl_TestGroupMappings GM   ON GM.ClientID = G.ClientID AND G.GroupID = GM.GroupID  AND GM.Type = 'G'
                            WHERE 
                                G.GroupID = @ID";
                            }
                            else
                            {
                                // If the UniqueID does not exist, pass the ID directly
                                groupQuery = @"
                                SELECT 
                                    G.GroupID AS ID,
                                    CASE WHEN B.[Values] IS NULL THEN G.GroupName ELSE B.TestName END AS Name,
                                    'Group' AS LabelType,
                                    ISNULL(B.[Values], '') AS [Value],
                                    ISNULL(B.[Action], '') AS [Action],
                                    ISNULL(B.Comments, '') AS Comments,
                                    ISNULL(G.Section, '') AS TestSection,
                                    0.00 AS HighValue,
                                    0.00 AS LowValue,
                                    ISNULL(G.MachineName, '') AS MachineName,
                                    '' AS Methord,
                                    '' AS Unit,
                                    '' AS NormalRange,
                                    ISNULL(GroupCode, '') AS TestCode,
                                    '' AS Specimen,
                                    GM.SortOrder AS SortOrder,
                                    @uniqueID AS [UniqueID], 
                                    'L' AS [TransType]
                                FROM 
                                    [dbo].[mtbl_TestGroupMaster] G
                                LEFT JOIN 
                                    [dbo].[trntbl_TestEntriesResults] B 
                                    ON B.ClientID = @ClientID 
                                    AND B.Sequence = @Sequence
                                    AND B.InvoiceNo = @InvoiceNo
                                    AND B.EditNo = @EditNo
                                    AND G.GroupID = B.TestID
                                    AND B.TransType = 'L'
                                LEFT JOIN
                                    mtbl_TestGroupMappings GM 
                                    ON GM.ClientID = G.ClientID 
                                    AND G.GroupID = GM.GroupID 
                                    AND GM.Type = 'G'
                                WHERE 
                                    G.GroupID = @ID";
                            }

                            // Execute the query
                            entry = _dbcontext.QueryFirstOrDefault<HierarchyEntry>(groupQuery, new
                            {
                                ClientID = clientId,
                                Sequence = sequence,
                                InvoiceNo = invoiceNo,
                                EditNo = editNo,
                                uniqueID = uniqueID,
                                ID = x.ChildID ?? x.ID
                            });

                            foreach (var sub in entry.SubArray)
                            {
                                sub.SortOrder = x.SortOrder; // Assuming SubArray elements have SortOrder property
                            }

                            // Recursively build the SubArray
                            entry.SubArray = BuildHierarchyRecursiveUniqueID(x.ChildID ?? 0);
                        }
                        else if (x.Type == "T")
                        {
                            string uniqueID = x.UniqueID;
                            string groupQuery;

                            // Check if the UniqueID exists in the database
                            string existingUniqueID = _dbcontext.QueryFirstOrDefault<string>(
                                @"SELECT [UniqueID] 
                              FROM [dbo].[trntbl_TestEntriesResults] B 
                              WHERE 
                                  B.ClientID = @ClientID 
                                  AND B.Sequence = @Sequence 
                                  AND B.InvoiceNo = @InvoiceNo 
                                  AND B.EditNo = @EditNo AND B.TestID = @ID 
                                  AND B.[UniqueID] = @UniqueID",
                                                    new
                                                    {
                                                        ClientID = clientId,
                                                        Sequence = sequence,
                                                        InvoiceNo = invoiceNo,
                                                        EditNo = editNo,
                                                        UniqueID = uniqueID,
                                                        ID = x.ChildID ?? x.TestID
                                                    });

                            if (existingUniqueID != null)
                            {
                                // Query to fetch the test details
                                groupQuery = @"
                                SELECT 
                                    T.TestID AS ID, 
                                    CASE WHEN B.[Values] IS NULL THEN T.TestName ELSE B.TestName END AS Name,
                                    'Item' AS LabelType, 
                                    ISNULL(B.[Values], '') AS [Value], 
                                    ISNULL(B.[Action], '') AS [Action], 
                                    ISNULL(B.Comments, '') AS Comments,
                                    ISNULL(T.Section, '') AS TestSection, 
                                    T.HighValue AS HighValue, 
                                    T.LowValue AS LowValue, 
                                    ISNULL(T.MachineName, '') AS MachineName,
                                    T.Methord AS Methord, 
                                    T.Unit AS Unit, 
                                    T.NormalRange AS NormalRange, 
                                    ISNULL(T.TestCode, '') AS TestCode, 
                                    T.Specimen AS Specimen,
                                    GM.SortOrder AS SortOrder,
                                    @uniqueID AS [UniqueID],
                                    'L' AS [TransType]
                                FROM 
                                    [dbo].[mtbl_TestMaster] T  
                                LEFT JOIN [dbo].[trntbl_TestEntriesResults] B ON B.ClientID = @ClientID AND B.Sequence = @Sequence AND B.InvoiceNo = @InvoiceNo AND B.EditNo = @EditNo AND T.TestID = B.TestID 
                                    AND B.UniqueID = @uniqueID
                                LEFT JOIN 
                                    mtbl_TestGroupMappings GM 
                                    ON GM.ClientID = T.ClientID 
                                    AND GM.TestID = T.TestID 
                                    AND GM.Type = 'T'
                                WHERE 
                                    T.TestID = @ID";
                            }
                            else
                            {
                                groupQuery = @"
                                SELECT 
                                    T.TestID AS ID, 
                                    CASE WHEN B.[Values] IS NULL THEN T.TestName ELSE B.TestName END AS Name,
                                    'Item' AS LabelType, 
                                    ISNULL(B.[Values], '') AS [Value], 
                                    ISNULL(B.[Action], '') AS [Action], 
                                    ISNULL(B.Comments, '') AS Comments,
                                    ISNULL(T.Section, '') AS TestSection, 
                                    T.HighValue AS HighValue, 
                                    T.LowValue AS LowValue, 
                                    ISNULL(T.MachineName, '') AS MachineName,
                                    T.Methord AS Methord, 
                                    T.Unit AS Unit, 
                                    T.NormalRange AS NormalRange, 
                                    ISNULL(T.TestCode, '') AS TestCode, 
                                    T.Specimen AS Specimen,
                                    GM.SortOrder AS SortOrder,
                                    @uniqueID AS [UniqueID],  
                                    'L' AS [TransType]
                                FROM 
                                    [dbo].[mtbl_TestMaster] T  
                                LEFT JOIN [dbo].[trntbl_TestEntriesResults] B ON B.ClientID = @ClientID AND B.Sequence = @Sequence AND B.InvoiceNo = @InvoiceNo AND B.EditNo = @EditNo AND T.TestID = B.TestID 
                                    AND B.UniqueID = @uniqueID
                                LEFT JOIN 
                                    mtbl_TestGroupMappings GM 
                                    ON GM.ClientID = T.ClientID 
                                    AND GM.TestID = T.TestID 
                                    AND GM.Type = 'T'
                                WHERE 
                                    T.TestID = @ID";
                            }


                            // Execute the query
                            entry = _dbcontext.QueryFirstOrDefault<HierarchyEntry>(groupQuery, new
                            {
                                ClientID = clientId,
                                Sequence = sequence,
                                InvoiceNo = invoiceNo,
                                EditNo = editNo,
                                uniqueID = uniqueID, // Pass the final UniqueID
                                ID = x.ChildID ?? x.TestID      // Pass the TestID
                            });

                            // Update SortOrder for SubArray if applicable
                            if (entry != null && entry.SubArray != null)
                            {
                                foreach (var sub in entry.SubArray)
                                {
                                    sub.SortOrder = x.SortOrder; // Assuming SubArray elements have SortOrder property
                                }
                            }

                            // Ensure SubArray is initialized as an empty list if null
                            entry.SubArray ??= new List<HierarchyEntry>();
                        }

                        else
                        {
                            // Fallback entry
                            entry = new HierarchyEntry
                            {
                                ID = x.ID,
                                Type = x.Type,
                                SortOrder = x.SortOrder,
                                SubArray = new List<HierarchyEntry>()
                            };
                        }

                        children.Add(entry);
                    }

                    return children;

                }

                // Start building from the specified group ID
                return BuildHierarchyRecursiveUniqueID(groupId);
            }
            catch (Exception ex)
            {

                throw ex;
            }
        }

        private void ExtractHierarchyEntries(HierarchyEntry entry, List<ResultEntryLines> resultList)
        {
            var item = new ResultEntryLines
            {
                ID = entry.ID,
                Name = entry.Name,
                Values = entry.Value,
                Action = entry.Action,
                Comments = entry.Comments,
                UniqueID = entry.UniqueID,
                transType = entry.TransType
            };

            resultList.Add(item);

            // If the object has a subArray, process it recursively
            if (entry.SubArray != null)
            {
                foreach (var subItem in entry.SubArray)
                {
                    ExtractHierarchyEntries(subItem, resultList);
                }
            }
        }

        private void postTestEntryResult1(string ClientID, long InvoiceNo, long Sequence, int EditNo, ResultEntry _trn, string AnalayzerJsonData, bool? isfromBarcode)
        {
            try
            {

                var parameters = new DynamicParameters();
                parameters.Add("@Flag", isfromBarcode == true ? 102 : 101);
                parameters.Add("@ClientID", ClientID);
                parameters.Add("@InvoiceNo", InvoiceNo);
                parameters.Add("@Sequence", Sequence);
                parameters.Add("@EditNo", EditNo);
                parameters.Add("@JsonData", JsonConvert.SerializeObject(_trn));
                parameters.Add("@AnalyzerJsonData", AnalayzerJsonData); 

                var data = _dbcontext.Query("SP_TestResultEntryCopy", parameters, commandType: CommandType.StoredProcedure);
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
    }
}
