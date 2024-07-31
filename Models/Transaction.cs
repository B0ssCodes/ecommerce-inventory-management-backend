namespace Inventory_Management_Backend.Models
{
    public class Transaction
    {
        public int TransactionID { get; set; }
        public int? Amount { get; set; }
        public DateTime? Date { get; set; }
        public int? VendorID { get; set; }
        public int? TransactionTypeID { get; set; }
        public int? TransactionStatusID { get; set; }
        public List<TransactionItem>? TransactionItems { get; set; }
        
    }
}
    