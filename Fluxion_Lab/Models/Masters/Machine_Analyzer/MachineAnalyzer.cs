using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Fluxion_Lab.Models.Masters.Machine_Analyzer
{
    public class MachineAnalyzer
    {
        public class AnalyzerTestMappingDto
        {
            public int TestID { get; set; }
            public string TestName { get; set; }
            public string AnalyzerMachineName { get; set; }
            public string Type { get; set; }
        }

        // Updated Model Classes
        public class BarcodeTest
        {
            public string barcode { get; set; }
            public string testName { get; set; }
            public string section { get; set; }
            public List<TestDetail> tests { get; set; }
            public int? testCount { get; set; } 
            public List<MachineData> machine_data { get; set; } 
            public string note { get; set; } 
        }

        public class TestDetail
        {
            public int? id { get; set; }
            public string name { get; set; }
            public string testCode { get; set; }
            public string labelType { get; set; }
            public string type { get; set; }
            public string analyzerMachineName { get; set; }
            public List<TestDetail> subArrays { get; set; }
        }

       
        public class MachineData
        {
            public int? id { get; set; }
            public string name { get; set; }
            public string testCode { get; set; }
            public string analyzerMachineName { get; set; }
            public string labelType { get; set; }
            public string type { get; set; }
        }

        // Model Classes
        public class AnalyzerDeviceResponse
        {
            [JsonPropertyName("barcode_no")]
            public string BarcodeNo { get; set; }

            [JsonPropertyName("patient_id")]
            public string patient_id { get; set; }

            [JsonPropertyName("patient_name")]
            public string PatientName { get; set; }

            [JsonPropertyName("gender")]
            public string Gender { get; set; }

            [JsonPropertyName("specimen_type")]
            public string SpecimenType { get; set; }

            [JsonPropertyName("device_id")]
            public string DeviceId { get; set; }

            [JsonPropertyName("Assays")]
            public List<AssayInfo> Assays { get; set; } = new List<AssayInfo>();
        }

        public class AssayInfo
        {
            [JsonPropertyName("online_testcode")]
            public string OnlineTestCode { get; set; }

            [JsonPropertyName("online_testname")]
            public string OnlineTestName { get; set; }
        }

        // Raw data model for database query
        public class RawAnalyzerData
        {
            public string Barcode { get; set; }
            public string patient_id { get; set; }
            public string PatientName { get; set; }
            public string Gender { get; set; }
            public string SpecimenType { get; set; }
            public string AnalyzerMachineName { get; set; }
            public string TestName { get; set; } 
            public string DeviceID { get; set; }
        }

        public class DeviceResult
        {
            [JsonProperty("device_id")]
            public int device_id { get; set; }

            [JsonProperty("online_testcode")]
            [Required]
            [StringLength(50)]
            public string online_testcode { get; set; }

            [JsonProperty("online_testname")]
            [Required]
            [StringLength(200)]
            public string online_testname { get; set; }

            [JsonProperty("value")]
            [Required]
            public string value { get; set; }

            [JsonProperty("unit")]
            public string unit { get; set; }

            [JsonProperty("barcode_no")]
            [Required]
            [StringLength(50)]
            public string barcode_no { get; set; }

            [JsonProperty("refrange")]
            public string refrange { get; set; }

            [JsonProperty("active")]
            public bool active { get; set; }

            [JsonProperty("created_on")]
            public DateTime created_on { get; set; }
        }

        public class InvoiceDetails
        {
            public string ClientID { get; set; }
            public string Sequence { get; set; }
            public string InvoiceNo { get; set; }
            public string EditNo { get; set; }
        }

        public class ResultDetails
        {
            public int TestID { get; set; }
            public string ResultValue { get; set; }
        }

        public class BarcodeAnalyzerResponse
        {
            public List<InvoiceDetails> InvoiceDetails { get; set; }
            public List<ResultDetails> ResultDetails { get; set; }
        }

        public class OutSourceTestMappingDto
        {
            public int ID { get; set; } // TestID
            public string Name { get; set; } // TestName
            public decimal Amount { get; set; }
            public string Type { get; set; }
        }

        public class OutSourceTestDto
        {
            public int ID { get; set; }
            public string Name { get; set; }
            public string Type { get; set; }   // "O" = outsource, "P" = panel, etc.
            public DateTime? CollectionDateTime { get; set; }
            public string ContactPersonName { get; set; }
            public string ContactNo { get; set; }
        }

        public class OutSourceTestDetailDto
        {
            public int TestID { get; set; }
            public string TestName { get; set; }
            public decimal Amount { get; set; }
            public int TotalTests { get; set; }
            public decimal TotalAmount { get; set; }
        }

        public class OutSourceLabSummaryDto
        {
            public int LabID { get; set; }
            public string LabName { get; set; }
            public int TotalTests { get; set; }
            public decimal TotalAmount { get; set; }
            public DateTime? FirstCollected { get; set; }
            public DateTime? LastCollected { get; set; }
            public List<OutSourceTestDetailDto> Tests { get; set; } = new();
        }
    }
}
