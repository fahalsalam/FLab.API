namespace Fluxion_Lab.Models.Razorpay
{
    public class PaymentGateway
    {
        public class PaymentDetails
        {
            public string Amount { get; set; }
            public string Method { get; set; }
            public string Currency { get; set; }
            public string Status { get; set; }
            public string OrderId { get; set; }
            public string CreatedAt { get; set; }
            public string Payment_Id { get; set; }
        }

        public class ConfirmPaymentPayload
        {
            public string razorpay_payment_id { get; set; }
            public string razorpay_order_id { get; set; }
            public string razorpay_signature { get; set; }
            public long? TempAID { get; set; }
        }

        public class RazorpayInitilize
        {
            public decimal amount { get; set; } 
        }

    }
}
