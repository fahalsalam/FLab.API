using Newtonsoft.Json;

namespace Fluxion_Lab.Models.Transactions.Purchase
{
    public class PurchaseReturnDraftItem
    {
        [JsonProperty("SI_No")]
        public int SI_No { get; set; }

        [JsonProperty("ItemId")]
        public long ItemId { get; set; }

        [JsonProperty("ItemName")]
        public string ItemName { get; set; } = string.Empty;

        [JsonProperty("BatchNo")]
        public string BatchNo { get; set; } = string.Empty;

        [JsonProperty("BatchCode")]
        public string BatchCode { get; set; } = string.Empty;

        [JsonProperty("Qty")]
        public decimal Qty { get; set; }

        [JsonProperty("ExpiryDate")]
        public string ExpiryDate { get; set; } = string.Empty;

        [JsonProperty("ExpiryDisplay")]
        public string ExpiryDisplay { get; set; } = string.Empty;

        [JsonProperty("SupplierName")]
        public string SupplierName { get; set; } = string.Empty;

        [JsonProperty("SupplierId")]
        public long SupplierId { get; set; }

        [JsonProperty("PurchaseRefNo")]
        public string PurchaseRefNo { get; set; } = string.Empty;

        [JsonProperty("PurchasePrice")]
        public decimal PurchasePrice { get; set; }

        [JsonProperty("TaxCode")]
        public string TaxCode { get; set; } = string.Empty;

        [JsonProperty("TaxAmount")]
        public decimal TaxAmount { get; set; }
    }
}
