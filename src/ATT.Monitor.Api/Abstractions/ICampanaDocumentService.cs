using ATT.Monitor.Api.Models.Campana;

namespace ATT.Monitor.Api.Abstractions;

/// <summary>Guardado de ZIP de campaña (paridad <c>SaveDoc</c> API v1).</summary>
public interface ICampanaDocumentService
{
    Task<DocumentoResponse> GuardarArchivoAsync(IFormFile archivo, CancellationToken cancellationToken = default);

    Task<DocumentoResponse> ActualizarZipAsync(string rutaZipExistente, IFormFile archivo, CancellationToken cancellationToken = default);
}
