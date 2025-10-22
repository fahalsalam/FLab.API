namespace Fluxion_Lab.Models
{
    public class Reports
    {
        public class salesReports
        {
            public string? fromDate { get; set; }
            public string? toDate { get; set; }
            public int? pageNo { get; set; }
            public int? pageSize { get; set; }
            public string? groupby { get; set; }
            public long? searchKey { get; set; }
            public string? itemtype { get; set; } 
        }

        public class salesMobileReports
        {
            public string? fromDate { get; set; }
            public string? toDate { get; set; }
            public int? pageNo { get; set; }
            public int? pageSize { get; set; }
            public string? groupby { get; set; }
            public string? searchKey { get; set; }

        }

    }
}
