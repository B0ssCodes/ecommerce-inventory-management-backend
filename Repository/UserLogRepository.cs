using Dapper;
using Inventory_Management_Backend.Data;
using Inventory_Management_Backend.Models;
using Inventory_Management_Backend.Models.Dto;
using Inventory_Management_Backend.Repository.IRepository;
using System.Data;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace Inventory_Management_Backend.Repository
{
    public class UserLogRepository : IUserLogRepository
    {
        private readonly DapperContext _db;
        private readonly ILogger<UserLogRepository> _logger;

        public UserLogRepository(DapperContext db, ILogger<UserLogRepository> logger)
        {
            _db = db;
            _logger = logger;
        }

        public async Task CreateUserLog(UserLogRequestDTO requestDTO)
        {
            try
            {
                using (IDbConnection connection = _db.CreateConnection())
                {
                    var query = @"
            INSERT INTO user_log (user_log_name, user_log_action, user_log_model, before_state, after_state)
            VALUES (@LogName, @Action, @Model, @BeforeState::jsonb, @AfterState::jsonb);";

                    var parameters = new
                    {
                        LogName = requestDTO.LogName,
                        Action = requestDTO.Action,
                        Model = requestDTO.Model,
                        BeforeState = JsonSerializer.Serialize(requestDTO.BeforeState),
                        AfterState = JsonSerializer.Serialize(requestDTO.AfterState),
                    };

                    await connection.ExecuteAsync(query, parameters);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while creating the user log.");
                throw;
            }
        }

        public Task<UserLogResponseDTO> GetUserLog(int id)
        {
            throw new NotImplementedException();
        }

        public Task<List<UserLogResponseDTO>> GetUserLogs(PaginationParams paginationParams)
        {
            throw new NotImplementedException();
        }
    }
}