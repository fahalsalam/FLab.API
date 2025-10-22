namespace Fluxion_Lab.Models.Masters.TestGroupMaster
{
    public class TestGroupMaster
    {
        public class TestID
        {
            public long TestId { get; set; }
            public string Type { get; set; }
            public int SortOrder { get; set; }
        } 
        public class TestGroups
        {
            public string GroupName { get; set; }
            public string? Section { get; set; }
            public string GroupCode { get; set; }
            public decimal Rate { get; set; } 
            public string MachineName { get; set; }
            public bool? ShowInReport { get; set; } 
            public decimal? Discount { get; set; }
            public string? AltTestName { get; set; }
            public string? AvgDays { get; set; }
            public string? AvgHour { get; set; }
            public string? AvgMinute { get; set; } 
            public List<TestID> TestIDs { get; set; }
        }

        public class TestIDMigration
        {
            public int GroupID { get; set; } 
            public long TestId { get; set; }
            public string Type { get; set; }
        }
        public class TestGroupsMigration
        {
            public int GroupID { get; set; }
            public string GroupName { get; set; }
            public string? Section { get; set; }
            public string GroupCode { get; set; }
            public decimal Rate { get; set; }
            public string MachineName { get; set; }
            public bool? ShowInReport { get; set; }
            public List<TestIDMigration> TestIDs { get; set; }
        }

    }
}
