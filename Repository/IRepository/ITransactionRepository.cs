using Inventory_Management_Backend.Models;
using Inventory_Management_Backend.Models.Dto;

namespace Inventory_Management_Backend.Repository.IRepository
{
    public interface ITransactionRepository
    {
        public Task<(List<AllTransactionResponseDTO>, int itemCount)> GetTransactions(string? vendorEmail, PaginationParams paginationParams);
        public Task<InboundTransactionResponseDTO> GetInboundTransaction(int transactionID);
        public Task<OutboundTransactionResponseDTO> GetOutboundTransaction(int transactionID);
        public Task<int> CreateTransaction(TransactionCreateDTO createDTO);
        public Task SubmitTransaction(TransactionSubmitDTO transactionDTO);
        public Task DeleteTransaction(int transactionID);
    }
}
