using Dapper;
using Inventory_Management_Backend.Data;
using Inventory_Management_Backend.Models;
using Inventory_Management_Backend.Models.Dto;
using Inventory_Management_Backend.Repository.IRepository;
using Inventory_Management_Backend.Utilities;

namespace Inventory_Management_Backend.Repository
{
    public class AuthRepository : IAuthRepository
    {
        private readonly DapperContext _db;

        public AuthRepository(DapperContext db)
        {
            _db = db;
        }
        public async Task Register(RegisterRequestDTO registerDTO)
        {
            using (var connection = _db.CreateConnection())
            {
                // Check if the user already exists
                int? existingUserId = await connection.QueryFirstOrDefaultAsync<int?>(
                    "SELECT user_id FROM user WHERE user_email = @Email",
                    new { Email = registerDTO.Email });

                if (existingUserId.HasValue)
                {
                    throw new Exception("User already exists");
                }

                // Create a new user object
                User newUser = new User
                {
                    FirstName = registerDTO.FirstName,
                    LastName = registerDTO.LastName,
                    Email = registerDTO.Email,
                    Password = PasswordHelper.HashPassword(registerDTO.Password),
                    Birthday = registerDTO.Birthday,
                    UserRoleID = registerDTO.UserRoleID
                };

                // Insert the new user and return the user ID
                var query = @"
            INSERT INTO user (user_first_name, user_last_name, user_email, user_password, user_birthday, user_role_id)
            VALUES (@FirstName, @LastName, @Email, @Password, @Birthday, @UserRoleID)
            RETURNING user_id;";

                int? userId = await connection.QuerySingleOrDefaultAsync<int>(query, newUser);

                if (!userId.HasValue)
                {
                    throw new Exception("User registration failed");
                }
            }
        }

    }
}

