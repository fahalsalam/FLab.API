namespace Fluxion_Lab.Models.Masters.UserMaster
{
    public class UserMaster
    {  
        public string UserCode { get; set; }
        public string UserName { get; set; }
        public string PasswordHash { get; set; }
        public string? Designation { get; set; }
        public string JoiningDate { get; set; }
        public string? SinatureUrl { get; set; } 
        public bool IsActive { get; set; }
        public decimal? MaxDiscount { get; set; } 
    }
}
