namespace Fluxion_Lab.Models.Masters.PrivilegeCards
{
    public class PrivilageCards
    {

        public class PrivilageCardsHd
        {
            public List<PrivilageCardsRates> PrivilegeCardsRates { get; set; }
        }

        public class PrivilageCardsRates 
        {
            public long CardID { get; set; }
            public long TestID { get; set; }
            public decimal Discount { get; set; } 
        }
    }
}
