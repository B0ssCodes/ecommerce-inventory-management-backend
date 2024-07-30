namespace Inventory_Management_Backend.Models
{
    public class InventoryTransaction
    {
        public int InventoryTransactionID { get; set; }
        public int InventoryID { get; set; }
        public Inventory Inventory { get; set; }
        public int TransactionID { get; set; }
        public Transaction Transaction { get; set; }
    }
}
