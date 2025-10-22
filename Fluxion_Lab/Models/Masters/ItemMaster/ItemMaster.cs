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

    }
}
