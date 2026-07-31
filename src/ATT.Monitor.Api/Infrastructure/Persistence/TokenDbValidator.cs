using System.Data;
using System.Security.Cryptography;
using System.Text;
using ATT.Monitor.Api.Abstractions;
using ATT.Monitor.Api.Infrastructure.Resilience;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Caching.Memory;

namespace ATT.Monitor.Api.Infrastructure.Persistence;

/// <summary>
/// Ejecuta <c>dbo.SP_ValidarToken</c> con reintentos transitorios y caché (TTL 45 s, paridad API v1).
/// </summary>
public sealed class TokenDbValidator(IConfiguration configuration, IMemoryCache cache) : ITokenDbValidator
{
    private const string SpValidarToken = "dbo.SP_ValidarToken";
    private const string TokenParam = "@TOKEN";
    private const string ErrorMessage = "ERROR";
    private static readonly TimeSpan CacheTtl = TimeSpan.FromSeconds(45);

    public async Task<TokenDbValidationResult> ValidateAsync(string bearerToken, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(bearerToken))
            return new TokenDbValidationResult(0, ErrorMessage);

        var connectionString = configuration.GetConnectionString("SqlServer");
        if (string.IsNullOrWhiteSpace(connectionString))
            return new TokenDbValidationResult(0, ErrorMessage);

        var cacheKey = BuildCacheKey(bearerToken);
        if (cache.TryGetValue(cacheKey, out TokenDbValidationResult? cached) && cached is not null)
            return cached;

        try
        {
            var final = await SqlTransientRetry.ExecuteAsync(async () =>
            {
                await using var connection = new SqlConnection(connectionString);
                var p = new DynamicParameters();
                p.Add(TokenParam, bearerToken, DbType.String, ParameterDirection.Input);
                var row = await connection.QueryFirstOrDefaultAsync<TokenRow>(
                    SpValidarToken,
                    p,
                    commandType: CommandType.StoredProcedure).ConfigureAwait(false);
                return row is null
                    ? new TokenDbValidationResult(0, ErrorMessage)
                    : new TokenDbValidationResult(row.Id, row.Token ?? string.Empty);
            }).ConfigureAwait(false);

            cache.Set(cacheKey, final, CacheTtl);
            return final;
        }
        catch (Exception)
        {
            return new TokenDbValidationResult(0, ErrorMessage);
        }
    }

    private static string BuildCacheKey(string token)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(token));
        return "jwtval:" + Convert.ToHexString(hash);
    }

    private sealed class TokenRow
    {
        public int Id { get; set; }
        public string? Token { get; set; }
    }
}
