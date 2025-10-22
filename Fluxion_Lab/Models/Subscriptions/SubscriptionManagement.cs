namespace Fluxion_Lab.Models.Subscriptions
{
    public class TenantDetals
    {
        public string companyName { get; set; }
        public string email { get; set; }
        public string mobileNo { get; set; }
        public string address { get; set; }
        public string pincode { get; set; }
        public string place { get; set; }
        public string district { get; set; }
        public string state { get; set; }
        public string country { get; set; }
    }

    public class PlanDetails
    {
        public string planID { get; set; }
        public string planName { get; set; }
        public decimal amount { get; set; }
    }

    public class RazorPayPaymentDetails
    {
        public string Amount { get; set; }
        public string Method { get; set; }
        public string Currency { get; set; }
        public string Status { get; set; }
        public string OrderId { get; set; }
        public string CreatedAt { get; set; }
        public string Payment_Id { get; set; }
    }

    public class TenantRegistration
    {
        public TenantDetals tenantDetails { get; set; }
        public PlanDetails planDetails { get; set; }
        public RazorPayPaymentDetails razorPayPaymentDetails { get; set; }
    }
}
