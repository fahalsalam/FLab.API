namespace Fluxion_Lab.Models.Transactions.Purchase
{
    public class PurchaseDto
    { 
        public class PurchaseHeader
        { 
            public long SupplierID { get; set; }
            public string? SupplierName { get; set; }
            public string? InvoiceDate { get; set; }
            public string? PurchaseRefNo { get; set; }
            public string? PurchaseDate { get; set; }
            public string? DueDate { get; set; }
            public decimal? Total { get; set; }
            public decimal? TaxAmount { get; set; }
            public decimal? DiscountAmount { get; set; }
            public decimal? NetAmount { get; set; }
            public decimal? RoudingAmount { get; set; } 
            public decimal? CashAmount { get; set; }
            public string? PaymentMode { get; set; }
            public string? Remarks { get; set; } 
           
        }

        public class PurchaseDetails
        { 
            public int SINo { get; set; }
            public long ItemNo { get; set; }
            public string ItemName { get; set; }
            public string BatchCode { get; set; }
            public decimal Quantity { get; set; }
            public decimal FOC { get; set; }
            public string Unit { get; set; }
            public decimal PackageSize { get; set; }
            public decimal Packing { get; set; } 
            public decimal Price { get; set; }
            public decimal MRP { get; set; }
            public decimal Total { get; set; }
            public decimal DiscountAmount { get; set; }
            public decimal DiscountPercentage { get; set; }
            public decimal RoundingAmount { get; set; } 
            public string TaxCode { get; set; }
            public decimal TaxAmount { get; set; }
            public decimal NetTotal { get; set; }
            public string? HscCode { get; set; } 

        }

        public class PurchaseBatch
        {
            public long ItemNo { get; set; }
            public string BatchCode { get; set; }
            public string PurchaseDate { get; set; }
            public DateTime ExpiryDate { get; set; }
            public decimal Quantity { get; set; } 
            public decimal? SalesPrice { get; set; }
            public decimal? PurchasePrice { get; set; } 
        }

        public class PurchaseData
        {
            public PurchaseHeader PurchaseHeader { get; set; }
            public List<PurchaseDetails> PurchaseDetails { get; set; }
            public List<PurchaseBatch> PurchaseBatches { get; set; } 
        }
    }
}
