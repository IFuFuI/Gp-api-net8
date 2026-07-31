namespace ATT.Monitor.Api.Abstractions;

/// <summary>
/// Valida el token JWT contra SQL Server (<c>dbo.SP_ValidarToken</c>), con caché en memoria.
/// </summary>
public interface ITokenDbValidator
{
    /// <summary>
    /// Resultado alineado al SP: <c>Id</c> = -1 indica token bloqueado/rechazado en BD.
    /// </summary>
    Task<TokenDbValidationResult> ValidateAsync(string bearerToken, CancellationToken cancellationToken = default);
}

/// <param name="Id">Identificador devuelto por el SP (p. ej. PN000001); -1 = bloqueado.</param>
/// <param name="Token">Mensaje o metadato según el SP.</param>
public sealed record TokenDbValidationResult(int Id, string Token);
