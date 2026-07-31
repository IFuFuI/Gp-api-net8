using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using ATT.Monitor.Api.Abstractions;
using ATT.Monitor.Api.Configuration;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace ATT.Monitor.Api.Services;

/// <summary>
/// Emisión JWT con misma política de expiración que API v1 (<c>JWT_EXPIRATION_MINUTES</c> / <c>TimeExpToken</c>).
/// </summary>
public sealed class JwtTokenIssuer(IOptionsMonitor<JwtSettings> jwtOptions) : ITokenIssuer
{
    public string GenerateToken(string subject)
    {
        var jwt = jwtOptions.CurrentValue;
        if (string.IsNullOrWhiteSpace(jwt.Secret) ||
            string.IsNullOrWhiteSpace(jwt.Issuer) ||
            string.IsNullOrWhiteSpace(jwt.Audience))
        {
            throw new InvalidOperationException(
                "JWT no configurado: Jwt:Secret, Jwt:Issuer, Jwt:Audience (o variables de entorno).");
        }

        var claims = new[] { new Claim(ClaimTypes.Name, subject) };
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Secret));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            issuer: jwt.Issuer,
            audience: jwt.Audience,
            claims: claims,
            expires: ResolveExpirationUtc(),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private static DateTime ResolveExpirationUtc()
    {
        var minutesEnv = Environment.GetEnvironmentVariable("JWT_EXPIRATION_MINUTES");
        if (!string.IsNullOrWhiteSpace(minutesEnv) &&
            int.TryParse(minutesEnv, out var minutes) &&
            minutes > 0)
        {
            minutes = Math.Clamp(minutes, 5, 10080);
            return DateTime.UtcNow.AddMinutes(minutes);
        }

        var yearsStr = Environment.GetEnvironmentVariable("TimeExpToken");
        var years = 1;
        if (!string.IsNullOrWhiteSpace(yearsStr) && int.TryParse(yearsStr, out var y) && y >= 1)
            years = Math.Clamp(y, 1, 10);

        return DateTime.UtcNow.AddYears(years);
    }
}
