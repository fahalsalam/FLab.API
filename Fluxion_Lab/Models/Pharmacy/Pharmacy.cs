namespace Fluxion_Lab.Models.Pharmacy
{
    public class Pharmacy
    {
        public class OpeningStock
        {
            public int sequence { get; set; }
            public int InvNo { get; set; }
            public string EntryDate { get; set; }
            public string Notes { get; set; }
            public List<OpeningStockItems> Items { get; set; } 
        }
        public class OpeningStockItems
        {
            public int ItemNo { get; set; }
            public string? ItemName { get; set; }
            public string? BatchNo { get; set; }
            public string? ExpiryDate { get; set; }
            public string? Packing { get; set; }
            public decimal? MRP { get; set; }
            public decimal? QTY { get; set; }
            public decimal? Rate { get; set; }
            public decimal? Amount { get; set; }
            public decimal? Discount { get; set; }
            public string? Taxcode { get; set; }
            public decimal? TaxAmount { get; set; } 
            public decimal? Total { get; set; } 
        }
        public class OpeningStockListItem
        {
            public int Sequence { get; set; }
            public long InvoiceNo { get; set; }
            public string EntryDate { get; set; }
            public string Remarks { get; set; }
            public List<OpeningStockItems> Items { get; set; }
        }

        public class StockAdjustment
        {
            public string EntryDate { get; set; }
            public string Notes { get; set; }
            public string ProductName { get; set; }
            public List<StockAdjustmentItems> Items { get; set; }
        }
        public class StockAdjustmentItems
        {
            public int SINo { get; set; }
            public int? ItemNo { get; set; } 
            public string? BatchNo { get; set; }
            public string? ExpiryDate { get; set; } 
            public decimal? MRP { get; set; } 
            public decimal? Rate { get; set; }
            public decimal? CurrentStock { get; set; }
            public decimal? PlusQty { get; set; }
            public decimal? MinusQty { get; set; }
            public decimal? AdjustedQty { get; set; }  
            public string? Taxcode { get; set; }  
        }

        public class SalesInvoiceItem
        {
            public long Item_No { get; set; }
            public string Item_Name { get; set; }
            public string Company { get; set; }
            public string TaxCode { get; set; }
            public decimal? Discount1 { get; set; }
            public decimal? Discount2 { get; set; }
            public decimal? Discount3 { get; set; }
            public decimal? MRP { get; set; }
            public decimal? SalesPrice { get; set; }
            public decimal? LastPurchasePrice { get; set; }
            public string Section { get; set; }
            public string Chemical { get; set; }
            public string HSNCode { get; set; }
            public string Shelf { get; set; }
            public decimal? ItemStock { get; set; }
            public List<ItemBatch> ItemBatch { get; set; }
        }

        public class ItemBatch
        {
            public string BatchCode { get; set; }
            public string ExpiryDate { get; set; }
            public decimal Onhand { get; set; }
            public decimal SalesPrice { get; set; }
            public decimal PurchasePrice { get; set; }
        }

        public class SalesInvoice
        {
            public long? PatientID { get; set; }
            public long? OPNo { get; set; }
            public int? RefDoctorID { get; set; }   
            public string EntryDate { get; set; }
            public decimal? TotalAmount { get; set; }
            public decimal? NetAmount { get; set; }
            public decimal? TaxAmount { get; set; }
            public decimal? Discount_Percentage { get; set; }
            public decimal? DiscountAmount { get; set; }
            public decimal? GPAmount { get; set; }
            public decimal? GP_Percentage { get; set; }
            public decimal? CashAmount { get; set; }
            public decimal? CardAmount { get; set; }
            public decimal? BankAmount { get; set; }
            public decimal? BalanceDue { get; set; }
            public string? PatientName { get; set; }
            public string? MobileNo { get; set; } 
            public List<SalesInvoiceDetail> Items { get; set; }
        }

        public class SalesInvoiceDetail
        {
            public int? SI_No { get; set; }
            public long? ItemNo { get; set; }
            public string ItemName { get; set; }
            public string TaxCode { get; set; }
            public string BatchCode { get; set; }
            public decimal? Qty { get; set; }
            public decimal? FOC { get; set; }
            public decimal? SalesPrice { get; set; }
            public decimal? PurchasePrice { get; set; }
            public decimal? MRP { get; set; }
            public decimal? Total { get; set; }
            public decimal? Disc_Percentage { get; set; }
            public decimal? Disc_Amount { get; set; }
            public decimal? TaxAmount { get; set; }
            public decimal? NetTotal { get; set; }
            public decimal? PostTaxTotal { get; set; }
            public decimal? GP_Percentage { get; set; }
            public decimal? GP_Amount { get; set; }
            public decimal? Onhand { get; set; }
        }
    }
}
