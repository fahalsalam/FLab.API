namespace Fluxion_Lab.Models.Masters.PatientMaster
{
    public class Patients
    {
        public class PatientMaster
        {
            public string PatientName { get; set; }
            public string? MobileNo { get; set; }
            public string? Gender { get; set; }
            public int? Days { get; set; }
            public int? Months { get; set; }
            public int? Year { get; set; } 
            public int? Age { get; set; }
            public string? DOB { get; set; }
            public string? Place { get; set; }
        }
    }
}
