namespace Fluxion_Lab.Models.Transactions.Payments
{
    public class PaymentPOST
    {
        public class PaymentDetails
        {
            public long? SupplierID { get; set; }   
            public string SupplierName { get; set; }   
            public DateTime? PaymentDate { get; set; }   
            public decimal? CashAmount { get; set; }  
            public decimal? BankAmount { get; set; }   
            public string Remarks { get; set; }   
            public List<Invoice> InvoiceDetails { get; set; }   
        }

        public class Invoice
        {
            public int SINo { get; set; }   
            public int InvoiceSequence { get; set; }   
            public long InvoiceNo { get; set; }  
            public int InvoiceEditNo { get; set; }   
            public DateTime InvoiceDate { get; set; }   
            public decimal TotalAmount { get; set; }   
            public decimal PaidAmount { get; set; }   
        }
    }
}
