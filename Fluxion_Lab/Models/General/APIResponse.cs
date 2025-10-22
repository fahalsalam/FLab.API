namespace Fluxion_Lab.Models.General
{
    public class APIResponse
    {
        public bool isSucess { get; set; } = true;
        public string? message { get; set; }
        public object? data { get; set; } 
    }

    public class TenantDetails
    {
        public int UserId { get; set; }
        public string UserName { get; set; }
        public string ClientID { get; set; }
        public string HiveConnection { get; set; }
    }

    public class TokenClaims
    {
        public string UserId { get; set; }
        public string ClientId { get; set; }
      
    }

}
