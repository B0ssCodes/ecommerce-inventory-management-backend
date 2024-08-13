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

        public UserLogRepository(DapperContext db, ILogger<UserLogRepository> logger)
        {
            _db = db;
        }

        public async Task CreateUserLog(UserLogRequestDTO requestDTO)
        {

            using (IDbConnection connection = _db.CreateConnection())
            {
                var query = @"
            INSERT INTO user_log (user_log_name, user_log_action, user_log_model, user_info_id, before_state, after_state)
            VALUES (@LogName, @Action, @Model, @UserID, @BeforeState::jsonb, @AfterState::jsonb);";

                var parameters = new
                {
                    LogName = requestDTO.LogName,
                    Action = requestDTO.Action,
                    Model = requestDTO.Model,
                    UserID = requestDTO.UserID,
                    BeforeState = requestDTO.BeforeState,
                    AfterState =requestDTO.AfterState,
                };

                await connection.ExecuteAsync(query, parameters);
            }
        }


        public async Task<UserLogResponseDTO> GetUserLog(int id)
        {
            using (IDbConnection connection = _db.CreateConnection())
            {
                connection.Open();

                var query = @"
            SELECT user_log_id_pkey AS LogID,
                   user_log_name AS LogName,
                   user_log_action AS Action,
                   user_log_model AS Model,
                   user_info_id AS UserID,
                   before_state AS BeforeState,
                   after_state AS AfterState
            FROM user_log
            WHERE user_log_id_pkey = @LogID;";

                var parameters = new { LogID = id };
                var result = await connection.QueryFirstOrDefaultAsync<UserLogResponseDTO>(query, parameters);

                if (result == null)
                {
                    throw new Exception("User log not found.");
                }

                return result;
            }
        }

        public async Task<(List<AllUserLogResponseDTO>, int)> GetUserLogs(PaginationParams paginationParams)
        {
            using (IDbConnection connection = _db.CreateConnection())
            {
                connection.Open();

                var offset = (paginationParams.PageNumber - 1) * paginationParams.PageSize;
                var searchQuery = paginationParams.Search;

                var query = @"
                    WITH FilteredLogs AS (
                        SELECT 
                            user_log_id_pkey AS LogID,
                            user_log_name AS LogName,
                            user_log_action AS Action,
                            user_log_model AS Model,
                            user_info_id AS UserID
                        FROM user_log
                        WHERE (@SearchQuery IS NULL OR 
                               user_log_name ILIKE '%' || @SearchQuery || '%' OR 
                               user_log_action ILIKE '%' || @SearchQuery || '%' OR 
                               user_log_model ILIKE '%' || @SearchQuery || '%')
                       
                    )
                    SELECT 
                        LogID, 
                        LogName, 
                        Action, 
                        Model,
                        UserID, 
                        COUNT(*) OVER() AS TotalCount
                    FROM FilteredLogs
                    ORDER BY LogID DESC
                    OFFSET @Offset ROWS
                    FETCH NEXT @PageSize ROWS ONLY;";

                var parameters = new
                {
                    Offset = offset,
                    PageSize = paginationParams.PageSize,
                    SearchQuery = searchQuery
                };

                var result = await connection.QueryAsync<AllUserLogResponseDTO, long, (AllUserLogResponseDTO, long)>(
                    query,
                    (log, totalCount) => (log, totalCount),
                    parameters,
                    splitOn: "TotalCount"
                );

                var logs = result.Select(r => r.Item1).ToList();
                int totalCount = result.Any() ? (int)result.First().Item2 : 0;

                return (logs, totalCount);
            }
        }
    }
}