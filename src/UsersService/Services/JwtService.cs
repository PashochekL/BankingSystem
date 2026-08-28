using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using UsersService.Entities;
using UsersService.Options;

namespace UsersService.Services;

public sealed class JwtService(IOptions<JwtOptions> jwtOptions) : IJwtService
{
    public string GenerateAccessToken(User user)
    {
        var options = jwtOptions.Value;
        if (string.IsNullOrWhiteSpace(options.Secret))
        {
            throw new InvalidOperationException("JWT secret is not configured.");
        }

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim("role", user.Role.ToString())
        };

        var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(options.Secret));
        var credentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);
        var expiresAt = DateTime.UtcNow.AddMinutes(options.AccessTokenExpirationMinutes);

        var token = new JwtSecurityToken(
            issuer: options.Issuer,
            audience: options.Audience,
            claims: claims,
            expires: expiresAt,
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
