using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace WebServer.Services
{
    public class JwtService
    {
        private static readonly SymmetricSecurityKey signingKey
            = new(Encoding.UTF8.GetBytes(Contexts.AppConstant.Jwt.SECRET_KEY));

        private static readonly SigningCredentials signingCredentials
            = new(signingKey, SecurityAlgorithms.HmacSha256);

        public string CreateToken(string id)
        {
            var claims = new[]
            {
                new Claim("id", id)
            };

            var token = new JwtSecurityToken(
                issuer: Contexts.AppConstant.Jwt.ISSUER,
                audience: Contexts.AppConstant.Jwt.AUDIENCE,
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(Contexts.AppConstant.Jwt.EXPIRE_MINUTES),
                signingCredentials: signingCredentials);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
