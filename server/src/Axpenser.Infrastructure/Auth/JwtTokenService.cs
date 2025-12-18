using Axpenser.Infrastructure.Identity;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Axpenser.Infrastructure.Auth
{
    public interface IJwtTokenService
    {
        string CreateAccessToken(AppUser user, IList<string> roles);
    }

    public class JwtTokenService : IJwtTokenService
    {
        private readonly JwtOptions _opt;
        public JwtTokenService(IOptions<JwtOptions> opt) => _opt = opt.Value;

        public string CreateAccessToken(AppUser user, IList<string> roles)
        {
            var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email ?? ""),
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.Email ?? ""),
            new("name", user.FullName ?? "")
        };

            foreach (var r in roles) claims.Add(new Claim(ClaimTypes.Role, r));

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_opt.Key));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: _opt.Issuer,
                audience: _opt.Audience,
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(_opt.AccessTokenMinutes),
                signingCredentials: creds);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}