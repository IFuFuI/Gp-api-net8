using System.IO.Compression;
using ATT.Monitor.Api.Abstractions;
using ATT.Monitor.Api.Models.Campana;
using Microsoft.AspNetCore.Http;

namespace ATT.Monitor.Api.Infrastructure.Files;

/// <summary>Paridad <c>SaveDoc</c> (ZIP campaña sobre <c>ARCHIVOS_ATM_PATH</c>).</summary>
public sealed class CampanaDocumentService(IMonitorFilePaths filePaths) : ICampanaDocumentService
{
    public async Task<DocumentoResponse> GuardarArchivoAsync(IFormFile archivo, CancellationToken cancellationToken = default)
    {
        if (archivo is null || archivo.Length == 0)
        {
            return new DocumentoResponse
            {
                Exito = false,
                Mensaje = "Archivo vacío o nulo"
            };
        }

        try
        {
            var carpetaDestino = filePaths.GetArchivosAtmRoot();
            if (string.IsNullOrWhiteSpace(carpetaDestino))
            {
                return new DocumentoResponse
                {
                    Exito = false,
                    Mensaje = "Variable de entorno ARCHIVOS_ATM_PATH no configurada o FileStorage:ArchivosAtmPath vacío."
                };
            }

            if (!Directory.Exists(carpetaDestino))
                Directory.CreateDirectory(carpetaDestino);

            var nombreBase = $"{Guid.NewGuid()}_{DateTime.Now:yyyyMMddHHmmss}";
            var rutaZipTemporal = Path.Combine(carpetaDestino, $"{nombreBase}_temp.zip");

            await using (var stream = new FileStream(rutaZipTemporal, FileMode.Create, FileAccess.Write, FileShare.None))
                await archivo.CopyToAsync(stream, cancellationToken).ConfigureAwait(false);

            var carpetaPlano = Path.Combine(carpetaDestino, nombreBase);
            if (!Directory.Exists(carpetaPlano))
                Directory.CreateDirectory(carpetaPlano);

            using (var archive = ZipFile.OpenRead(rutaZipTemporal))
            {
                foreach (var entry in archive.Entries)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    if (string.IsNullOrEmpty(entry.Name))
                        continue;

                    var nombrePlano = Path.GetFileName(entry.Name);
                    var rutaDestinoArchivo = Path.Combine(carpetaPlano, nombrePlano);

                    var contador = 1;
                    while (System.IO.File.Exists(rutaDestinoArchivo))
                    {
                        var nombreSinExt = Path.GetFileNameWithoutExtension(nombrePlano);
                        var ext = Path.GetExtension(nombrePlano);
                        var nuevoNombre = $"{nombreSinExt}_{contador}{ext}";
                        rutaDestinoArchivo = Path.Combine(carpetaPlano, nuevoNombre);
                        contador++;
                    }

                    await using var entryStream = entry.Open();
                    await using var fileStream = new FileStream(rutaDestinoArchivo, FileMode.Create, FileAccess.Write, FileShare.None);
                    await entryStream.CopyToAsync(fileStream, cancellationToken).ConfigureAwait(false);
                }
            }

            var rutaZipFinal = Path.Combine(carpetaDestino, $"{nombreBase}.zip");
            ZipFile.CreateFromDirectory(carpetaPlano, rutaZipFinal);

            System.IO.File.Delete(rutaZipTemporal);
            Directory.Delete(carpetaPlano, recursive: true);

            return new DocumentoResponse
            {
                Exito = true,
                Mensaje = "ZIP generado correctamente",
                RutaArchivo = rutaZipFinal,
                NombreArchivo = Path.GetFileName(rutaZipFinal)
            };
        }
        catch (Exception ex)
        {
            return new DocumentoResponse
            {
                Exito = false,
                Mensaje = $"Error al procesar archivo: {ex.Message}"
            };
        }
    }

    public async Task<DocumentoResponse> ActualizarZipAsync(string rutaZipExistente, IFormFile archivo, CancellationToken cancellationToken = default)
    {
        try
        {
            var carpetaBase = Path.GetDirectoryName(rutaZipExistente)!;
            var tempOriginal = Path.Combine(carpetaBase, "temp_original");
            var tempNuevo = Path.Combine(carpetaBase, "temp_nuevo");

            if (Directory.Exists(tempOriginal))
                Directory.Delete(tempOriginal, recursive: true);
            if (Directory.Exists(tempNuevo))
                Directory.Delete(tempNuevo, recursive: true);

            Directory.CreateDirectory(tempOriginal);
            Directory.CreateDirectory(tempNuevo);

            ZipFile.ExtractToDirectory(rutaZipExistente, tempOriginal);

            var rutaNuevoZip = Path.Combine(carpetaBase, $"{Guid.NewGuid()}_temp.zip");
            await using (var stream = new FileStream(rutaNuevoZip, FileMode.Create, FileAccess.Write, FileShare.None))
                await archivo.CopyToAsync(stream, cancellationToken).ConfigureAwait(false);

            using (var archive = ZipFile.OpenRead(rutaNuevoZip))
            {
                foreach (var entry in archive.Entries)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    if (string.IsNullOrEmpty(entry.Name))
                        continue;

                    var nombrePlano = Path.GetFileName(entry.Name);
                    var destino = Path.Combine(tempNuevo, nombrePlano);
                    await using var entryStream = entry.Open();
                    await using var fileStream = new FileStream(destino, FileMode.Create, FileAccess.Write, FileShare.None);
                    await entryStream.CopyToAsync(fileStream, cancellationToken).ConfigureAwait(false);
                }
            }

            foreach (var fileNuevo in Directory.GetFiles(tempNuevo))
            {
                var nombre = Path.GetFileName(fileNuevo);
                var destino = Path.Combine(tempOriginal, nombre);
                System.IO.File.Copy(fileNuevo, destino, overwrite: true);
            }

            System.IO.File.Delete(rutaZipExistente);
            ZipFile.CreateFromDirectory(tempOriginal, rutaZipExistente);

            Directory.Delete(tempOriginal, recursive: true);
            Directory.Delete(tempNuevo, recursive: true);
            System.IO.File.Delete(rutaNuevoZip);

            return new DocumentoResponse
            {
                Exito = true,
                Mensaje = "ZIP actualizado correctamente",
                RutaArchivo = rutaZipExistente
            };
        }
        catch (Exception ex)
        {
            return new DocumentoResponse
            {
                Exito = false,
                Mensaje = ex.Message
            };
        }
    }
}
