using System.Text.Json;

namespace Fluxion_Lab.Models.Masters.GeneralSettings
{
    public class GeneralSettings
    { 
        public string? TopMargin { get; set; }
        public string? BottomMargin { get; set; }
        public string? AbnormalAlert { get; set; }
        public string? AutoBillPrint { get; set; }
        public string? PdfTopMargin { get; set; }
        public string? PdfBottomMargin { get; set; }
        public string? ReportVerificationSteps { get; set; }
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? MobileNo1 { get; set; }
        public string? MobileNo2 { get; set; }
        public string? AddressLine1 { get; set; }
        public string? AddressLine2 { get; set; } 
        public string? Place { get; set; }
        public string? WorkingTime { get; set; }
        public bool? ShowItemLevelDiscount { get; set; }
        public bool? AutoBarcodePrint { get; set; }
        public bool? BarcodePrinting { get; set; }
        public int? ExpiryDays { get; set; }
        public bool? SyringeSelection { get; set; }
        public bool? IsSampleCollectionNeeded { get; set; }
        public bool? PaymentMarkOnBillSave { get; set; }
        public bool? ShowReferLab { get; set; }
        public bool? ShowPrivilageCard { get; set; }
        public decimal? ZoomFactor { get; set; } 
        public bool? ShowQRonBill { get; set; }
        public bool? ShowApproveDetailsInReport { get; set; }
        public string? BillTemplate { get; set; }
        public string? ResultTemplate { get; set; }
        public string? BillType { get; set; } 
        public JsonElement? ReceptionSettings { get; set; } 
        public string? outsourceReciepttype { get; set; } 
        public bool? autoPrintoutsourceReciept { get; set; }
        public bool? OpNumseatchoption { get; set; }
        public string? Lab_technologistname1 { get; set; }
        public string? designation1 { get; set; }
        public string? sign_url1 { get; set; }
        public string? Lab_technologistname2 { get; set; }
        public string? designation2 { get; set; }
        public string? sign_url2 { get; set; } 
        public string? ReportLeft_margin { get; set; }
        public string? ReportRight_margin { get; set; } 
    }
}
