﻿namespace Inventory_Management_Backend.Models.Dto
{
    public class OutboundTransactionResponseDTO
    {
        public int TransactionID { get; set; }
        public int Amount { get; set; }
        public DateTime Date { get; set; }
        public UserResponseDTO User { get; set; }
        public string TransactionType { get; set; }
        public string TransactionStatus { get; set; }
        public List<TransactionItemResponseDTO> TransactionItems { get; set; }
    }
}
