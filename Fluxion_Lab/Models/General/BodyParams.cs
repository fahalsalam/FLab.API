namespace Fluxion_Lab.Models.General
{
    public class BodyParams
    {
        public class ReceiptTestEntryRequest
        {
            public long InvoiceNo { get; set; }
            public long Sequence { get; set; }
            public int EditNo { get; set; }
            public decimal CashAmount { get; set; }
            public decimal BankAmount { get; set; }
            public string PayMode { get; set; } 
            public decimal DrAmount { get; set; }
            public decimal CrAmount { get; set; }
            public string TransType { get; set; } 

        }

        public class EntryOrderChangeRequest
        {
            public List<EntryOrderChange> entryOrderChanges { get; set; }
        }

        public class EntryOrderChange
        {
            public int ID { get; set; }
            public int currentOrder { get; set; }
            public int updatedOrder { get; set; } 

        }

        public class PurchaseList
        {
            public string FromDate { get; set; }
            public string ToDate { get; set; }
            public long? SupplierID { get; set; }
            public long? ItemNo { get; set; }
            public int PageNo { get; set; }
            public int PageSize { get; set; }
        }

        public class PurchaseReport
        {
            public string? fromDate { get; set; }
            public string? toDate { get; set; }
            public int? pageNo { get; set; }
            public int? pageSize { get; set; }
            public string? groupby { get; set; }
            public long? searchKey { get; set; }
        }

        public class PrivilageCardsMapping
        {
            public long patientID { get; set; }
            public long cardNumber { get; set; }
            public long cardID { get; set; }  
        }

        public class ClientMasterPUT
        {
            public string? headerImage { get; set; }
            public string? headerICloudmage { get; set; }
            public string? footerImage { get; set; }
            public string? footerCloudImage { get; set; } 
            public string? signature { get; set; } 
            public string? letterheading_url { get; set; }
            public string? backupPath { get; set; } 
        }

        public class DeviceConfig
        {
            public string? sysID { get; set; }
            public string? sysName { get; set; }
            public string? billPrinter { get; set; }
            public string? resultPrinter { get; set; }
            public string? barcodePrinter { get; set; }
            public decimal? zoomFactor { get; set; }
            public string? opBillPrinter { get; set; }
            public string? opBillCardPrinter { get; set; }
            public string? opRecieptPrinter { get; set; }
            public string? outsourceRecieptPrinter { get; set; } 
        }

        public class ResultEntryParams
        {
            public int? _sequence { get; set;}
            public long? invoiceNo { get; set;}
            public int? editNo { get; set;}  
        }

        public class ReceiptEntryRequest
        { 
            public long? PatientID { get; set; }
            public string? PatientName { get; set; } 
            public decimal CashAmount { get; set; }
            public decimal BankAmount { get; set; }
            public string PayMode { get; set; }
            public decimal? DrAmount { get; set; }
            public decimal? CrAmount { get; set; }
            public List<ReceiptEntryLines> Lines { get; set; } 
        }


        public class ReceiptEntryLines
        {
            public long InvoiceNo { get; set; }
            public int Sequence { get; set; }
            public int EditNo { get; set; } 
            public decimal TotalAmount { get; set; }
            public decimal PayingAmout { get; set; }
            public string TransType { get; set; }
            
        }

        // Removed OPBillingEntryRequest and OPBillingEntryLine classes (now defined in Reception.cs)

    }
}
