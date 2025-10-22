namespace Fluxion_Lab.Models.Transactions.Expense
{
    public class ExpenseEntry
    {
        public class ExpenseEntryRequest
        {
            public string EntryDate { get; set; }
            public decimal GrandTotal { get; set; }
            public List<ExpenseItem> ExpenseItems { get; set; }
        }

        public class ExpenseItem
        {
            public string Category { get; set; }
            public decimal Amount { get; set; }
            public string Description { get; set; }
        }

        public class ExpenseEntryResponse
        {
            public long EntryID { get; set; }
            public long ClientID { get; set; }
            public string EntryDate { get; set; }
            public decimal GrandTotal { get; set; }
            public string ExpenseItems { get; set; }
            public DateTime CreatedAt { get; set; }
        }
    }
}
