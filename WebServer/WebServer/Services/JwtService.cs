using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace WebServer.Services
{
    public class JwtService
    {
        private readonly ILogger<JwtService> logger;

        private readonly SymmetricSecurityKey signingKey;
        private readonly SigningCredentials signingCredentials;

        private readonly string secretKey;
        private readonly string issuer;
        private readonly string audience;
        private readonly ushort expireMinutes;

        public JwtService(ILogger<JwtService> logger,
                          IConfiguration configuration)
        {
            this.logger = logger;

            var secretKey = configuration["JWT:SecretKey"];
            var issuer = configuration["JWT:Issuer"];
            var audience = configuration["JWT:Audience"];
            var expireMinutes = configuration["JWT:ExpireMinutes"];

            if (secretKey == null 
                    || issuer == null || audience == null 
                    || expireMinutes == null)
            {
                logger.LogCritical("JWT configuration is missing required fields");
                Environment.Exit(1);
            }

            this.secretKey = secretKey;
            this.issuer = issuer;
            this.audience = audience;

            try
            {
                this.expireMinutes = ushort.Parse(expireMinutes);
            }
            catch (Exception)
            {
                logger.LogCritical("JWT configuration is missing required fields");
                Environment.Exit(1);
            }
            
            signingKey = new(Encoding.UTF8.GetBytes(this.secretKey));
            signingCredentials = new(signingKey, SecurityAlgorithms.HmacSha256);
        }

        public string CreateToken(ulong userId, string nickname)
        {
            var claims = new[]
            {
                new Claim("userId", $"{userId}"),
                new Claim("nickname", nickname)
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