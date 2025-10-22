namespace Fluxion_Lab.Models.Reception
{
    public class Reception
    {
        public class AppointmentBookingDto
        { 
            public string Department { get; set; }
            public int DoctorID { get; set; }
            public string Doctor { get; set; }
            public string BookingDate { get; set; }
            public int Token { get; set; }
            public string BookingTime { get; set; }
            public long? PatientID { get; set; } 
            public string PatientName { get; set; }
            public string MobileNumber { get; set; }
            public string? AlternateMobileNumber { get; set; }
            public string Place { get; set; } 
        }
    }

    public class DoctorWithScheduleDto
    {
        public int DoctorID { get; set; }
        public string DoctorName { get; set; }
        public string Department { get; set; }
        public string Designation { get; set; }
        public int? DayOfWeek { get; set; }
        public string DayOfWeekNName { get; set; }
        public string StartTime { get; set; }
        public string EndTime { get; set; }
        public int? SlotDuration { get; set; } 
        public List<BookingDto> Bookings { get; set; } = new List<BookingDto>(); 
        public List<RescheduleHeaderDto> RescheduleHeaders { get; set; } = new List<RescheduleHeaderDto>();
    }

    public class BookingDto
    {
        public long BookingID { get; set; }
        public int DoctorID { get; set; }
        public string BookingDate { get; set; }
        public string BookingTime { get; set; }
        public string PatientName { get; set; }
        public string MobileNumber { get; set; }
        public string AlternateMobileNumber { get; set; }
        public string Place { get; set; }
        public string BookingStatus { get; set; }
        public int Token { get; set; }
    } 

    public class RescheduleBookingRequest
    {
        public int DoctorID { get; set; }
        public string Departmet { get; set; } // Note: matches input JSON key
        public string ResheduledFrom { get; set; }
        public string ResheduledTO { get; set; } 
        public string ResheduledFromTime { get; set; }
        public string ResheduleToTime { get; set; } 
        public List<RescheduleBookingDto> Bookings { get; set; }
    }

    public class RescheduleBookingDto
    {
        public long BookingID { get; set; }
        public string BookingDate { get; set; }
        public string BookingTime { get; set; } // Note: matches input JSON key
    }

    public class RescheduleHeaderDto
    {
        public int DoctorID { get; set; }
        public string ResheduledFrom { get; set; }
        public string ResheduledTo { get; set; }
        public string StartTime { get; set; } 
        public string EndTime { get; set; } 
    }

    public class GeneralBillEntryHdrDto
    {
        public string OpNumber { get; set; }
        public int PatientID { get; set; }
        public string PatientName { get; set; }
        public string MobileNo { get; set; }
        public int Age { get; set; }
        public string Gender { get; set; }
        public DateTime EntryDate { get; set; }
        public int Ref_DoctorID { get; set; }
        public string Ref_DoctorName { get; set; }
        public string PaymentMode { get; set; }
        public string PaymentStatus { get; set; }
        public decimal CashAmount { get; set; }
        public decimal BankAmount { get; set; }
        public decimal BalanceDue { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal GrandTotal { get; set; }
    }

    public class GeneralBillEntryLineDto
    {
        public int SI_No { get; set; }
        public int ID { get; set; }
        public string Name { get; set; }
        public decimal Amount { get; set; }
        public int Qty { get; set; }
        public decimal Rate { get; set; }
    }

    public class GeneralBillEntryRequest
    {
        public GeneralBillEntryHdrDto GeneralBillEtryHdr { get; set; }
        public List<GeneralBillEntryLineDto> GeneralBillEntryLines { get; set; }
    }

    public class OPBillingEntryRequest
    { 
        public long? PatientID { get; set; } 
        public long? OpNumber { get; set; }
        public string PatientName { get; set; }
        public string MobileNo { get; set; }
        public string Department { get; set; }
        public string BookingDate { get; set; }
        public int? Token { get; set; } 
        public int? Age { get; set; }
        public string? DOB { get; set; }
        public int? Month { get; set; }
        public int? Days { get; set; }  
        public string Gender { get; set; }
        public string? EntryDate { get; set; }
        public string? ValidUpto { get; set; }
        public int? Ref_DoctorID { get; set; }
        public string Ref_DoctorName { get; set; }
        public string PaymentMode { get; set; }
        public string PaymentStatus { get; set; }
        public decimal? CashAmount { get; set; }
        public decimal? BankAmount { get; set; }
        public decimal? BalanceDue { get; set; }
        public decimal? GrandTotal { get; set; }
        public bool? IsReview { get; set; }
        public bool? IsRenew { get; set; }
        public string? Place { get; set; }
        public string? OPType { get; set; } 
    }

    // New model classes for PendingBills with ReceiptHistory
    public class PendingBillDto
    {
        public long ClientID { get; set; }
        public int Sequence { get; set; }
        public long InvoiceNo { get; set; }
        public int EditNo { get; set; }
        public string TransType { get; set; }
        public string Departmet { get; set; } // Note: keeping original spelling from DB
        public long? PatientID { get; set; }
        public string PatientName { get; set; }
        public DateTime EntryDate { get; set; }
        public decimal? BalanceDue { get; set; }
        public decimal? TotalAmount { get; set; }
        public string DocStatus { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? LastUpdatedAt { get; set; }
        public long? OpNumber { get; set; }
        
        // Additional properties that might come from joins or other sources
        public string MobileNo { get; set; }
        public string Department { get; set; } // Corrected spelling variant
        public decimal PaidAmount { get; set; } // Calculated field
        
        // Receipt History from the stored procedure JSON
        public List<ReceiptHistoryDto> ReceiptHistory { get; set; } = new List<ReceiptHistoryDto>();
    }

    public class ReceiptHistoryDto
    {
        public DateTime ReceiptDate { get; set; }
        public string PaymentMode { get; set; }
        public decimal CashAmount { get; set; }
        public decimal BankAmount { get; set; }
        public int Sequence { get; set; }
        public int EditNo { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal PayingAmout { get; set; } // Note: keeping original spelling from SP
        public string TransType { get; set; }
    }

    public class PendingBillsGroupedDto
    {
        public string Department { get; set; }
        public int PendingBillCount { get; set; }
        public List<PendingBillDto> Bills { get; set; } = new List<PendingBillDto>();
    }
}
