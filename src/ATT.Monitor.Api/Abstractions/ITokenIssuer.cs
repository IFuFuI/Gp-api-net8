namespace ATT.Monitor.Api.Abstractions;

/// <summary>Emisión de JWT (login admin / ATM).</summary>
public interface ITokenIssuer
{
    string GenerateToken(string subject);
}
