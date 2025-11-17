namespace Fluxion_Lab.Models.Masters.ItemMaster
{
    public class ItemMaster
    { 
        public string Item_Name { get; set; }
        public string Company { get; set; }
        public string Category { get; set; }
        public decimal Package { get; set; }
        public string Unit { get; set; }
        public decimal CostPerTest { get; set; }
        public string CostPerTestUnit { get; set; }
        public decimal ReorderLevel { get; set; }
        public string ReorderLevelUnit { get; set; }
        public string ItemType { get; set; }
        public string? ItemCode { get; set; }
        public decimal? Rate { get; set; } 
        public int? Chemical { get; set; }
        public string? Section { get; set; }
        public string? Schedule { get; set; }
        public string? Packing_Size { get; set; }
        public decimal? MRP { get; set; }
        public int? Supplier { get; set; }
        public string? Shelf { get; set; }
        public string? HSNCode { get; set; }
        public string? Barcode { get; set; } 
        public string? TaxCode { get; set; }
        public string? Remarks { get; set; }
        public decimal? Discount1 { get; set; }
        public decimal? Discount2 { get; set; }
        public decimal? Discount3 { get; set; } 
    }

}
