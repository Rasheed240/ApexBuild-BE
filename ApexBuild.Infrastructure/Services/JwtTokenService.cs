using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using ApexBuild.Application.Common.Interfaces;
using ApexBuild.Domain.Entities;


namespace ApexBuild.Infrastructure.Services
{
    public class JwtTokenService : IJwtTokenService
    {
        private readonly IConfiguration _configuration;

        public JwtTokenService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public string GenerateAccessToken(User user)
        {
            var securityKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(_configuration["Jwt:SecretKey"]!));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Name, user.FullName),
            new Claim("email_confirmed", user.EmailConfirmed.ToString())
        };

            // Add roles with context information
            foreach (var userRole in user.UserRoles.Where(ur => ur.IsActive))
            {
                var roleType = userRole.Role.RoleType.ToString();
                
                // Add the role claim
                claims.Add(new Claim(ClaimTypes.Role, roleType));

                // Add context-specific role claims for better authorization checking
                if (userRole.OrganizationId.HasValue && userRole.ProjectId.HasValue)
                {
                    // Role in a specific project within an organization
                    claims.Add(new Claim("role_scope", $"{roleType}:org:{userRole.OrganizationId}:proj:{userRole.ProjectId}"));
                    claims.Add(new Claim("organization_id", userRole.OrganizationId.Value.ToString()));
                    claims.Add(new Claim("project_id", userRole.ProjectId.Value.ToString()));
                }
                else if (userRole.OrganizationId.HasValue)
                {
                    // Role in an organization (not project-specific)
                    claims.Add(new Claim("role_scope", $"{roleType}:org:{userRole.OrganizationId}"));
                    claims.Add(new Claim("organization_id", userRole.OrganizationId.Value.ToString()));
                }
                else if (userRole.ProjectId.HasValue)
                {
                    // Role in a project (no organization)
                    claims.Add(new Claim("role_scope", $"{roleType}:proj:{userRole.ProjectId}"));
                    claims.Add(new Claim("project_id", userRole.ProjectId.Value.ToString()));
                }
                else
                {
                    // Global/platform role (no specific context)
                    claims.Add(new Claim("role_scope", $"{roleType}:global"));
                }
            }

            var token = new JwtSecurityToken(
                issuer: _configuration["Jwt:Issuer"],
                audience: _configuration["Jwt:Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddHours(24),
                signingCredentials: credentials
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        public string GenerateRefreshToken()
        {
            var randomNumber = new byte[64];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(randomNumber);
            return Convert.ToBase64String(randomNumber);
        }

        public Guid? ValidateToken(string token)
        {
            try
            {
                var tokenHandler = new JwtSecurityTokenHandler();
                var key = Encoding.UTF8.GetBytes(_configuration["Jwt:SecretKey"]!);

                tokenHandler.ValidateToken(token, new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ValidateIssuer = true,
                    ValidIssuer = _configuration["Jwt:Issuer"],
                    ValidateAudience = true,
                    ValidAudience = _configuration["Jwt:Audience"],
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero
                }, out SecurityToken validatedToken);

                var jwtToken = (JwtSecurityToken)validatedToken;
                var userIdClaim = jwtToken.Claims.First(x => x.Type == ClaimTypes.NameIdentifier).Value;

                return Guid.Parse(userIdClaim);
            }
            catch
            {
                return null;
            }
        }
    }
}
