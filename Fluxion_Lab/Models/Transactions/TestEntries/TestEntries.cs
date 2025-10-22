using static Fluxion_Lab.Models.Transactions.Purchase.PurchaseDto;

namespace Fluxion_Lab.Models.Transactions.TestEntries
{
    public class TestEntries
    {
        public class TestEntriesData
        {
            public TestEntryHdr testEtryHdr { get; set; }
            public List<TestEntryLine> testEntryLines { get; set; }
        }

        public class TestEntryHdr
        {
            public long PatientID { get; set; }
            public string PatientName { get; set; }
            public string? MobileNo { get; set; }
            public int? Age { get; set; }
            public int? Month { get; set; }
            public int? Days { get; set; }
            public string? Gender { get; set; }
            public string? DOB { get; set; }
            public string? Place { get; set; } 
            public string? EntryDate { get; set; }
            public string? Ref_Lab { get; set; }
            public int? Ref_DoctorID { get; set; }
            public string? Ref_DoctorName { get; set; }
            public string TestStatus { get; set; }
            public string? SiringeType { get; set; }
            public string? PaymentMode { get; set; }
            public string? PaymentStatus { get; set; }
            public decimal? CashAmount { get; set; }
            public decimal? BankAmount { get; set; }
            public decimal? AdvanceAmount { get; set; }
            public decimal? BalanceDue { get; set; }
            public decimal? TotalAmount { get; set; }
            public decimal? DiscAmount { get; set; }
            public decimal? GrandTotal { get; set; }
            public decimal? AdditionalAmount { get; set; }
            public string? Remarks { get; set; }

        }

        public class TestEntryLine
        {
            public int SI_No { get; set; }
            public long ID { get; set; }
            public string Name { get; set; }
            public char Type { get; set; }
            public decimal Amount { get; set; } = 0;
        }

        public class ResultEntry
        {
            public List<ResultEntryLines> Results { get; set; } 
        }

        public class ResultEntryLines
        {
            public long ID { get; set; }
            public string Name { get; set; }
            public string Values { get; set; }
            public string Action { get; set; }
            public string Comments { get; set; }
            public string UniqueID { get; set; }
            public string transType { get; set; }  
        }

        public class TestdIds
        {
            public List<Tests> TestIDs { get; set; }
        }

        public class Tests
        {
            public long TestID { get; set; } 
        }

        public class GroupChild
        {
            public int? SI_No { get; set; }
            public long? ID { get; set; }
            public string? Name { get; set; }
            public string? LableType { get; set; }
            public string? Value { get; set; }
            public string? Action { get; set; }
            public string? Comments { get; set; }
            public string? TestSection { get; set; }
            public decimal? HighValue { get; set; }
            public decimal? LowValue { get; set; }
            public string? MachineName { get; set; }
            public string? Methord { get; set; }
            public string? Unit { get; set; }
            public string? NormalRange { get; set; } 
            public string? TestCode { get; set; }
            public string? Specimen { get; set; }
            public List<TestHistory> TestHistory { get; set; }
        }

        public class TestHistory
        {
            public long? ID { get; set; }
            public string? TestName { get; set; }
            public string? Values { get; set; }
            public string? EntryDate { get; set; }
        }


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

        public class TestEntryDataLine
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
        public class SyncDataRequest
        {
            public List<TestEntryHeader> Headers { get; set; } = new();
            public List<TestEntryDataLine> LineItems { get; set; } = new();
            public long ClientID { get; set; }
            public string ClientName { get; set; } = string.Empty;
        } 
        public class SyncDataResponse
        {
            public bool Success { get; set; }
            public string Message { get; set; } = string.Empty;
            public List<dynamic>? SyncedData { get; set; }
            public int HeaderCount { get; set; }
            public int LineItemCount { get; set; }
        }

    }
}
