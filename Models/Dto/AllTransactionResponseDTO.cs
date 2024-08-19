namespace Inventory_Management_Backend.Models.Dto
{
    public class AllTransactionResponseDTO
    {
        public int TransactionID { get; set; }
        public int Amount { get; set; }
        public DateTime Date { get; set; }
        public int TypeID { get; set; }
        public string Type { get; set; }
        public string Status { get; set; }
        public string Email { get; set; }
    }
}
