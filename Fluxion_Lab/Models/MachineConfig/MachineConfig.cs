namespace Fluxion_Lab.Models.MachineConfig
{
    public class MachineConfig
    {
        public class TestResult
        {
            public string MachineName { get; set; }
            public string SystemName { get; set; }
            public string Value { get; set; }
        }

        public class PatientAnalyzerResult
        {
            public string PatientID { get; set; }
            public string Name { get; set; }
            public string SampleID { get; set; }
            public List<TestResult> TestResults { get; set; }
        }

    }
}
