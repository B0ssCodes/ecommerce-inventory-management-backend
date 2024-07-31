using Inventory_Management_Backend.Models;
using Inventory_Management_Backend.Models.Dto;

namespace Inventory_Management_Backend.Repository.IRepository
{
    public interface ITransactionRepository
    {
        public Task<List<AllTransactionResponseDTO>> GetTransactions(PaginationParams paginationParams);
        public Task<TransactionResponseDTO> GetTransaction(int transactionID);
        public Task<int> CreateTransaction(TransactionCreateDTO createDTO);
        public Task SubmitTransaction(TransactionSubmitDTO transactionDTO);
    }
}
