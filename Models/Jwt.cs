using System.Security.Claims;
using twitter_clone.Models;

namespace twitter_clone.Models
{
    public class Jwt
    {
        public string Key { get; set; } = null!;
        public string Issuer { get; set; } = null!;
        public string Audience { get; set; } = null!;

        public static dynamic Validar(ClaimsIdentity identity, Supabase.Client _context)
        {
            try
            {
                if (!identity.Claims.Any())
                {
                    return new
                    {
                        success = false
                    };
                }

                var id = identity?.Claims?.FirstOrDefault(u => u.Type == "id")?.Value;

                if (string.IsNullOrEmpty(id) || !Guid.TryParse(id, out Guid parsedId))
                {
                    return new
                    {
                        success = false,
                        message = "Invalid or missing ID"
                    };
                }

                var user = _context
                            .From<UsersModel>()
                            .Where(u => u.User_id == parsedId)
                            .Get();

                if (user == null)
                {
                    return new
                    {
                        success = false,
                        message = "Invalid or missing ID"
                    };
                }

                return new
                {
                    success = true,
                    id = user.Id
                };

            }
            catch (Exception)
            {
                return new
                {
                    success = false,
                    message = "Internal server error"
                };
            }
        }
    }
}