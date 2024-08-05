using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Inventory_Management_Backend.Utilities
{
    public class TokenHelper
    {
        private readonly IConfiguration _configuration;

        public TokenHelper(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public string GenerateAccessToken(string userId, string firstName, int userRoleId, string roleName, string userMail)
        {
            var claims = new[]
            {
                new Claim (ClaimTypes.NameIdentifier, userId),
                new Claim (ClaimTypes.Name, firstName),
                new Claim (ClaimTypes.Role, userRoleId.ToString()),
                new Claim ("RoleName", roleName),
                new Claim (ClaimTypes.Email, userMail)
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha512);

            var token = new JwtSecurityToken(
            claims: claims,
            expires: DateTime.Now.AddDays(7),
            signingCredentials: creds);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
