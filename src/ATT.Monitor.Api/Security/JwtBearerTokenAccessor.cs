namespace ATT.Monitor.Api.Security;

/// <summary>
/// Obtiene el JWT compacto del header Authorization (entrada de <c>dbo.SP_ValidarToken</c>).
/// </summary>
public static class JwtBearerTokenAccessor
{
    private const string BearerPrefix = "Bearer ";

    public static bool TryGetRawJwt(HttpRequest request, out string? jwt)
    {
        jwt = null;
        if (!request.Headers.TryGetValue("Authorization", out var values))
            return false;

        var auth = values.FirstOrDefault();
        if (string.IsNullOrWhiteSpace(auth) ||
            !auth.StartsWith(BearerPrefix, StringComparison.OrdinalIgnoreCase))
            return false;

        jwt = auth.AsSpan(BearerPrefix.Length).Trim().ToString();
        return !string.IsNullOrWhiteSpace(jwt);
    }
}
