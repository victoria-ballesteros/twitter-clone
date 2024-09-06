using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using twitter_clone.Models;

namespace twitter_clone.Services
{
    public class Authentication
    {
        public static string? GenerateJWTAuthentication(Guid userId)
        {
            var claims = new List<Claim>{
                new(ClaimTypes.NameIdentifier, userId.ToString())
            };

            var jwt = ConfigurationSingleton.Configuration.GetSection("JwtSettings").Get<Jwt>();

            if (jwt == null)
            {
                return null;
            }

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(Convert.ToString(jwt.Key)));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var expires = DateTime.Now.AddMinutes(60);

            var token = new JwtSecurityToken(
                Convert.ToString(jwt.Issuer),
                Convert.ToString(jwt.Audience),
                claims,
                expires: expires,
                signingCredentials: creds
            );
            return new JwtSecurityTokenHandler().WriteToken(token);
        }


        public static Guid? ValidateToken(string token)
        {
            if (string.IsNullOrWhiteSpace(token))
                return null;

            var jwt = ConfigurationSingleton.Configuration.GetSection("JwtSettings").Get<Jwt>();

            if (jwt == null)
            {
                return null;
            }

            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(Convert.ToString(jwt.Key));
            try
            {
                tokenHandler.ValidateToken(token, new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ValidateIssuer = false,
                    ValidateAudience = false,
                    ClockSkew = TimeSpan.Zero
                }, out SecurityToken validatedToken);

                var jwtToken = (JwtSecurityToken)validatedToken;
                var userIdClaim = jwtToken.Claims.FirstOrDefault(claim => claim.Type == ClaimTypes.NameIdentifier)?.Value;

                if (Guid.TryParse(userIdClaim, out var userId))
                {
                    return userId;
                }
                return null;
            }
            catch
            {
                return null;
            }
        }
    }
}