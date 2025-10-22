namespace Fluxion_Lab.Models.DataSync
{
    public class DataSync
    {
        public class TestEntryHeader
        {
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

        public class TestEntryLine1
        {
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

        public class TestDataSyncRequest
        {
            public List<TestEntryHeader> Headers { get; set; }
            public List<TestEntryLine1> Lines { get; set; }
        }

        public class SyncResponse
        {
            public bool Success { get; set; }
            public string Message { get; set; }
        }
    }
}
