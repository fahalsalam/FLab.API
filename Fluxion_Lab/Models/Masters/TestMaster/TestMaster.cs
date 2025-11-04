using System.Collections.Generic;

namespace Fluxion_Lab.Models.Masters.TestMaster
{
    public class TestItem
    {
        public long ItemNo { get; set; }
        public decimal? MinReagentValue { get; set; }
        public string? MinReagentUnit { get; set; }
    }

    public class TestMaster
    {
        public string TestName { get; set; }
        public string? Section { get; set; }
        public decimal? Rate { get; set; }
        public string? TestCode { get; set; }
        public string? Unit { get; set; }
        public string? NormalRange { get; set; }
        public string? Method { get; set; }
        public decimal? LowValue { get; set; }
        public decimal? HighValue { get; set; } 
        public string? Specimen { get; set; }
        public List<TestItem> Items { get; set; }
        public decimal? Discount { get; set; }
        public string? MachineName { get; set; }
        public string? AltTestName { get; set; }
        public string? AvgDays { get; set; }
        public string? AvgHour { get; set; }
        public string? AvgMinute { get; set; }
        public string? ItemType { get; set; } 
    }

    public class TestMasterMigration
    {
        public int TestID { get; set; }
        public string TestName { get; set; }
        public string? Section { get; set; }
        public decimal? Rate { get; set; }
        public string? TestCode { get; set; }
        public string? Unit { get; set; }
        public string? NormalRange { get; set; }
        public string? Method { get; set; }
        public decimal? LowValue { get; set; }
        public decimal? HighValue { get; set; }
        public string? Specimen { get; set; }
        public decimal? MinReagentValue { get; set; }
        public string? MinReagentUnit { get; set; }
        public long? ItemNo { get; set; }
        public decimal? Discount { get; set; }
        public string? MachineName { get; set; }
    }
}
