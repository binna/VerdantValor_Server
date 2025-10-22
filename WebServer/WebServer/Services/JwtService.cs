using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace WebServer.Services
{
    public class JwtService
    {
        private readonly SymmetricSecurityKey signingKey;
        private readonly SigningCredentials signingCredentials;

        private readonly ILogger<JwtService> logger;

        private readonly string issuer;
        private readonly string audience;
        private readonly string secretKey;
        private readonly ushort expireMinutes;

        public JwtService(ILogger<JwtService> logger,
                          IConfiguration configuration)
        {
            this.logger = logger;

            this.secretKey = configuration["JWT:SecretKey"];
            this.expireMinutes = ushort.Parse(configuration["JWT:ExpireMinutes"]);
            this.issuer = configuration["JWT:Issuer"];
            this.audience = configuration["JWT:Audience"];

            if (this.secretKey == null 
                    || this.issuer == null 
                    || this.audience == null)
            {
                // TODO
                logger.LogError("JWT configuration is missing required fields");
            }

            signingKey = new(Encoding.UTF8.GetBytes(this.secretKey));
            signingCredentials = new(signingKey, SecurityAlgorithms.HmacSha256);
        }

        public string CreateToken(string id)
        {
            var claims = new[]
            {
                new Claim("id", id)
            };

            var token = new JwtSecurityToken(
                issuer: this.issuer,
                audience: this.audience,
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(this.expireMinutes),
                signingCredentials: signingCredentials);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
