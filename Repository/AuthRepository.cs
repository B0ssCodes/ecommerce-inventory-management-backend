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
                // Check if the user already exists by email
                bool emailExists = await connection.QueryFirstOrDefaultAsync<bool>(
                    "SELECT EXISTS(SELECT 1 FROM public.\"user\" WHERE user_email = @Email)",
                    new { Email = registerDTO.Email });

                if (emailExists)
                {
                    throw new Exception("User already exists");
                }

                User newUser = new User
                {
                    FirstName = registerDTO.FirstName,
                    LastName = registerDTO.LastName,
                    Email = registerDTO.Email,
                    Password = PasswordHelper.HashPassword(registerDTO.Password),
                    Birthday = registerDTO.Birthday,
                    UserRoleID = registerDTO.UserRoleID
                };

                // Convert DateOnly to DateTime
                var birthdayDateTime = new DateTime(newUser.Birthday.Year, newUser.Birthday.Month, newUser.Birthday.Day);

                // Insert the new user and return the user ID
                var query = @"
        INSERT INTO public.""user"" (user_first_name, user_last_name, user_email, user_password, user_birth_date, user_role_id)
        VALUES (@FirstName, @LastName, @Email, @Password, @Birthday, @UserRoleID)
        RETURNING user_id_pkey AS UserID;";

                int? userId = await connection.QuerySingleOrDefaultAsync<int>(query, new
                {
                    newUser.FirstName,
                    newUser.LastName,
                    newUser.Email,
                    newUser.Password,
                    Birthday = birthdayDateTime,
                    newUser.UserRoleID
                });

                if (!userId.HasValue)
                {
                    throw new Exception("User registration failed");
                }
            }
        }

    }
}

