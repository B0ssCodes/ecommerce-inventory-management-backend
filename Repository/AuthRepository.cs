using Dapper;
using Inventory_Management_Backend.Data;
using Inventory_Management_Backend.Models;
using Inventory_Management_Backend.Models.Dto;
using Inventory_Management_Backend.Repository.IRepository;
using Inventory_Management_Backend.Utilities;
using Microsoft.Extensions.Configuration;

namespace Inventory_Management_Backend.Repository
{
    public class AuthRepository : IAuthRepository
    {
        private readonly DapperContext _db;
        private readonly IConfiguration _configuration;

        public AuthRepository(DapperContext db, IConfiguration configuration)
        {
            _db = db;
            _configuration = configuration;
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

        public async Task<LoginResponseDTO> Login(LoginRequestDTO loginDTO)
        {
            using (var connection = _db.CreateConnection())
            {
                if (string.IsNullOrEmpty(loginDTO.Email) || string.IsNullOrEmpty(loginDTO.Password))
                {
                    throw new Exception("Email and password are required");
                }


                var query = @"
            SELECT u.user_id_pkey AS UserId, u.user_first_name AS FirstName, u.user_last_name AS LastName, 
                   u.user_email AS Email, u.user_password AS Password, u.user_role_id AS UserRoleID,
                   r.role AS UserRole
            FROM public.""user"" u
            JOIN public.""user_role"" r ON u.user_role_id = r.user_role_id_pkey
            WHERE u.user_email = @Email";

                var userWithRole = await connection.QuerySingleOrDefaultAsync<UserWithRole>(query, new { Email = loginDTO.Email });

                if (userWithRole == null)
                {
                    throw new Exception("Invalid email or password");
                }

                // Validate the password using Password Helper
                if (!PasswordHelper.VerifyPassword(loginDTO.Password, userWithRole.Password))
                {
                    throw new Exception("Invalid email or password");
                }

                // Generate the token using Token Helper, it takes the configuration (appsettings.json)
                var tokenService = new TokenHelper(_configuration);
                var token = tokenService.GenerateAccessToken(userWithRole.UserId.ToString(), userWithRole.FirstName);

                var response = new LoginResponseDTO
                {
                    Token = token,
                    FirstName = userWithRole.FirstName,
                    Email = userWithRole.Email,
                    Role = userWithRole.UserRole 
                };

                return response;
            }
        }

    }
}

