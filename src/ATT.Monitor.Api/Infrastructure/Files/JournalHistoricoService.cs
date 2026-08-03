using System.Globalization;
using System.IO.Compression;
using System.Text;
using ATT.Monitor.Api.Abstractions;
using ATT.Monitor.Api.Configuration;
using ATT.Monitor.Api.Models.JournalHistorico;
using Microsoft.Extensions.Options;

namespace ATT.Monitor.Api.Infrastructure.Files;

public sealed class JournalHistoricoService(
    IMonitorFilePaths filePaths,
    IOptionsMonitor<JournalHistoricoOptions> options) : IJournalHistoricoService
{
    private static readonly string[] MergeableExtensions = [".txt", ".log", ".journal", ".csv", ".json"];

    private const int MaxEpLength = 80;

    private static readonly char[] AdditionalInvalidPathChars =
    [
        Path.DirectorySeparatorChar,
        Path.AltDirectorySeparatorChar,
        ':'
    ];

    public Task<JournalHistoricoListResponse> ListarAsync(JournalHistoricoListRequest request, CancellationToken cancellationToken = default)
    {
        _ = cancellationToken;
        var opt = options.CurrentValue;
        var eps = ResolveEpsForList(request);

        var desde = request.Desde.Kind == DateTimeKind.Utc ? request.Desde : request.Desde.ToUniversalTime();
        var hasta = request.Hasta.Kind == DateTimeKind.Utc ? request.Hasta : request.Hasta.ToUniversalTime();
        if (hasta < desde)
            (desde, hasta) = (hasta, desde);

        var maxDays = Math.Max(1, opt.MaxRangeDays);
        if ((hasta - desde).TotalDays > maxDays)
            hasta = desde.AddDays(maxDays);

        var journalRoot = filePaths.GetJournalDiaRoot();
        var list = new List<JournalHistoricoItemDto>();
        if (!string.IsNullOrWhiteSpace(journalRoot) && Directory.Exists(journalRoot))
        {
            var rootFull = Path.GetFullPath(journalRoot);
            foreach (var ep in eps)
            {
                if (list.Count >= opt.MaxListScanFiles)
                    break;
                CollectNewLayout(rootFull, ep, desde, hasta, list, opt.MaxListScanFiles);
                if (list.Count < opt.MaxListScanFiles)
                    CollectLegacyRootFiles(rootFull, ep, desde, hasta, list, opt.MaxListScanFiles);
            }
        }

        list.Sort((a, b) =>
        {
            var c = string.Compare(a.Ep, b.Ep, StringComparison.OrdinalIgnoreCase);
            if (c != 0)
                return c;
            return b.ModificadoUtc.CompareTo(a.ModificadoUtc);
        });

        var total = list.Count;
        var tam = Math.Clamp(request.TamPagina, 1, 200);
        var pagina = Math.Max(0, request.Pagina);
        var skip = pagina * tam;
        var page = list.Skip(skip).Take(tam).ToList();

        return Task.FromResult(new JournalHistoricoListResponse
        {
            Items = page,
            Total = total,
            RangoMaximoDias = maxDays
        });
    }

    private static List<string> ResolveEpsForList(JournalHistoricoListRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (request.Eps is { Count: > 0 })
        {
            var eps = request.Eps
                .Select(NormalizeAndValidateEp)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (eps.Count == 0)
                throw new ArgumentException("Indique al menos una estación válida.", nameof(request));

            return eps;
        }

        if (string.IsNullOrWhiteSpace(request.Ep))
            throw new ArgumentException("Indique al menos una estación (eps o ep).", nameof(request));

        return [NormalizeAndValidateEp(request.Ep)];
    }

    private static string NormalizeAndValidateEp(string? value)
    {
        var ep = (value ?? string.Empty).Trim();

        if (string.IsNullOrWhiteSpace(ep))
            throw new ArgumentException("El identificador de estación no puede estar vacío.");

        if (ep.Length > MaxEpLength)
            throw new ArgumentException($"El identificador de estación no puede superar {MaxEpLength} caracteres.");

        if (ep is "." or ".." || ep.Contains("..", StringComparison.Ordinal))
            throw new ArgumentException("El identificador de estación contiene una secuencia no permitida.");

        if (Path.IsPathRooted(ep) ||
            ep.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0 ||
            ep.IndexOfAny(AdditionalInvalidPathChars) >= 0)
        {
            throw new ArgumentException("El identificador de estación contiene caracteres no permitidos.");
        }

        return ep;
    }

    private static bool IsSafeRelativePath(string value)
    {
        if (string.IsNullOrWhiteSpace(value) || Path.IsPathRooted(value))
            return false;

        var segments = value
            .Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar)
            .Split(Path.DirectorySeparatorChar, StringSplitOptions.RemoveEmptyEntries);

        return segments.Length > 0 &&
               segments.All(segment =>
                   segment is not "." and not ".." &&
                   segment.IndexOfAny(Path.GetInvalidFileNameChars()) < 0);
    }

    private static string? FindChildDirectory(string parentDirectory, string expectedName)
    {
        if (!Directory.Exists(parentDirectory))
            return null;

        return Directory.EnumerateDirectories(parentDirectory)
            .FirstOrDefault(path => string.Equals(
                Path.GetFileName(path),
                expectedName,
                StringComparison.OrdinalIgnoreCase));
    }

    private static void CollectNewLayout(
        string rootFull,
        string ep,
        DateTime desdeUtc,
        DateTime hastaUtc,
        List<JournalHistoricoItemDto> list,
        int maxItems)
    {
        var safeEp = NormalizeAndValidateEp(ep);

        var epDir = FindChildDirectory(rootFull, safeEp);
        if (epDir is null)
            return;

        foreach (var loteDir in Directory.EnumerateDirectories(epDir))
        {
            if (list.Count >= maxItems)
                return;

            var loteId = Path.GetFileName(loteDir);
            if (string.IsNullOrWhiteSpace(loteId))
                continue;

            var loteFullPath = Path.GetFullPath(loteDir)
                .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                + Path.DirectorySeparatorChar;

            foreach (var file in Directory.EnumerateFiles(loteDir, "*", SearchOption.AllDirectories))
            {
                if (list.Count >= maxItems)
                    return;

                var fileFullPath = Path.GetFullPath(file);
                if (!fileFullPath.StartsWith(loteFullPath, StringComparison.OrdinalIgnoreCase))
                    continue;

                var fi = new FileInfo(fileFullPath);
                var t = fi.LastWriteTimeUtc;
                if (t < desdeUtc || t > hastaUtc)
                    continue;

                var rel = Path.GetRelativePath(loteDir, fileFullPath).Replace(Path.DirectorySeparatorChar, '/');
                var id = "n:" + ToBase64Url($"{safeEp}|{loteId}|{rel}");
                list.Add(new JournalHistoricoItemDto
                {
                    Id = id,
                    Ep = safeEp,
                    LoteId = loteId,
                    NombreArchivo = fi.Name,
                    RutaRelativa = string.IsNullOrEmpty(rel) ? fi.Name : rel,
                    TamanoBytes = fi.Length,
                    ModificadoUtc = t
                });
            }
        }
    }

    private static void CollectLegacyRootFiles(
        string rootFull,
        string ep,
        DateTime desdeUtc,
        DateTime hastaUtc,
        List<JournalHistoricoItemDto> list,
        int maxItems)
    {
        foreach (var file in Directory.EnumerateFiles(rootFull))
        {
            if (list.Count >= maxItems)
                return;
            var name = Path.GetFileName(file);
            if (name.Length < ep.Length + 2 || !name.StartsWith(ep + "_", StringComparison.OrdinalIgnoreCase))
                continue;

            var fi = new FileInfo(file);
            var t = fi.LastWriteTimeUtc;
            if (t < desdeUtc || t > hastaUtc)
                continue;

            var id = "l:" + ToBase64Url(name);
            list.Add(new JournalHistoricoItemDto
            {
                Id = id,
                Ep = ep,
                LoteId = "legacy",
                NombreArchivo = name,
                RutaRelativa = name,
                TamanoBytes = fi.Length,
                ModificadoUtc = t
            });
        }
    }

    /// <inheritdoc />
    public async Task<(Stream Stream, string FileName, JournalHistoricoDownloadSummaryDto Summary, bool DeletePath)> CrearDescargaZipAsync(
        JournalHistoricoDownloadRequest request,
        CancellationToken cancellationToken = default)
    {
        var opt = options.CurrentValue;
        var temp = Path.Combine(Path.GetTempPath(), "journal-hist-" + Guid.NewGuid().ToString("N", CultureInfo.InvariantCulture) + ".zip");
        var (_, summary) = await WriteZipArchiveToPathAsync(request, temp, opt, cancellationToken).ConfigureAwait(false);
        var stream = new FileStream(temp, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, FileOptions.DeleteOnClose);
        return (stream, summary.NombreZip, summary, false);
    }

    /// <inheritdoc />
    public async Task<(string ZipFileName, JournalHistoricoDownloadSummaryDto Summary)> WriteExportZipToFileAsync(
        JournalHistoricoDownloadRequest request,
        string outputPath,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(outputPath))
            throw new ArgumentException("La ruta de salida es obligatoria.", nameof(outputPath));
        var dir = Path.GetDirectoryName(Path.GetFullPath(outputPath));
        if (!string.IsNullOrEmpty(dir))
            Directory.CreateDirectory(dir);

        var opt = options.CurrentValue;
        return await WriteZipArchiveToPathAsync(request, outputPath, opt, cancellationToken).ConfigureAwait(false);
    }

    private async Task<(string ZipFileName, JournalHistoricoDownloadSummaryDto Summary)> WriteZipArchiveToPathAsync(
        JournalHistoricoDownloadRequest request,
        string zipPath,
        JournalHistoricoOptions opt,
        CancellationToken cancellationToken)
    {
        var ids = request.Ids?.Where(s => !string.IsNullOrWhiteSpace(s)).Select(s => s.Trim()).ToList() ?? [];
        if (ids.Count == 0)
            throw new ArgumentException("Debe indicar al menos un id.", nameof(request));
        if (ids.Count > opt.MaxDownloadSelection)
            throw new ArgumentException($"Máximo {opt.MaxDownloadSelection} archivos por descarga.", nameof(request));

        var journalRoot = filePaths.GetJournalDiaRoot();
        if (string.IsNullOrWhiteSpace(journalRoot) || !Directory.Exists(journalRoot))
            throw new InvalidOperationException("Journal diario no configurado o carpeta inexistente.");

        var rootFull = Path.GetFullPath(journalRoot);
        if (!rootFull.EndsWith(Path.DirectorySeparatorChar))
            rootFull += Path.DirectorySeparatorChar;

        var resolved = ResolverIds(ids, rootFull, NormalizeAllowedEps(request.AllowedEps), cancellationToken);

        var distinctEps = resolved.Select(r => r.Ep).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        var multiEpLayout = distinctEps.Count > 1;

        var zipFileName = ConstruirNombreZip(distinctEps);

        await EscribirZipAsync(request, zipPath, opt, resolved, multiEpLayout, cancellationToken).ConfigureAwait(false);

        var summary = new JournalHistoricoDownloadSummaryDto
        {
            NombreZip = zipFileName,
            Ep = multiEpLayout ? ConstruirEtiquetaEps(distinctEps) : distinctEps[0],
            ArchivosIncluidos = ListarArchivos(resolved, multiEpLayout),
            Modo = ResolverModo(request.Unificar, multiEpLayout),
            Partes = ContarPartes(request.Unificar, multiEpLayout, zipPath, resolved.Count)
        };

        return (zipFileName, summary);
    }

    private static List<ResolvedJournalFile> ResolverIds(
        List<string> ids,
        string rootFull,
        HashSet<string>? allowed,
        CancellationToken cancellationToken)
    {
        var resolved = new List<ResolvedJournalFile>(ids.Count);
        foreach (var id in ids)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!TryResolveId(rootFull, id, allowed, out var ep, out var full, out var display))
                throw new ArgumentException($"Id no válido o fuera de alcance: {id}");
            resolved.Add(new ResolvedJournalFile(ep, full, display));
        }

        return resolved;
    }

    private static string ConstruirNombreZip(List<string> distinctEps)
    {
        var stamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss", CultureInfo.InvariantCulture);
        return distinctEps.Count == 1
            ? $"journal_{SanitizeZipSegment(distinctEps[0])}_{stamp}.zip"
            : $"journal_export_{stamp}.zip";
    }

    /// <summary>Escribe el zip segun unificacion y cantidad de EPs. Unificar exige que todo sea texto.</summary>
    private static async Task EscribirZipAsync(
        JournalHistoricoDownloadRequest request,
        string zipPath,
        JournalHistoricoOptions opt,
        List<ResolvedJournalFile> resolved,
        bool multiEpLayout,
        CancellationToken cancellationToken)
    {
        if (!request.Unificar)
        {
            if (multiEpLayout)
                await WriteMultiZipEpFoldersAsync(zipPath, resolved, cancellationToken).ConfigureAwait(false);
            else
                await WriteMultiZipFlatAsync(zipPath, resolved, cancellationToken).ConfigureAwait(false);
            return;
        }

        if (resolved.Exists(r => !IsMergeable(r.FullPath)))
        {
            throw new InvalidOperationException(
                "Unificar solo aplica a archivos de texto (.txt, .log, .journal, .csv, .json). Quite archivos binarios o desactive unificar.");
        }

        if (multiEpLayout)
            await WriteUnifiedZipPerEpFoldersAsync(zipPath, resolved, opt, cancellationToken).ConfigureAwait(false);
        else
            await WriteUnifiedZipSingleEpAsync(zipPath, resolved, opt, cancellationToken).ConfigureAwait(false);
    }

    private static string ResolverModo(bool unificar, bool multiEpLayout)
    {
        if (unificar)
            return multiEpLayout ? "unificado_por_ep" : "unificado";

        return multiEpLayout ? "zip_con_carpetas" : "zip";
    }

    private static string ConstruirEtiquetaEps(List<string> distinctEps) =>
        distinctEps.Count <= 5
            ? string.Join(", ", distinctEps.OrderBy(e => e, StringComparer.OrdinalIgnoreCase))
            : string.Join(", ", distinctEps.Take(3)) + "…";

    private static int ContarPartes(bool unificar, bool multiEpLayout, string zipPath, int totalArchivos)
    {
        if (!unificar)
            return totalArchivos;

        return multiEpLayout ? CountZipEntries(zipPath) : CountUnifiedTxtParts(zipPath);
    }

    private static List<string> ListarArchivos(List<ResolvedJournalFile> resolved, bool multiEpLayout) =>
        multiEpLayout
            ? resolved.Select(r => $"{r.Ep}/{r.DisplayName}").ToList()
            : resolved.Select(r => r.DisplayName).ToList();

    private sealed record ResolvedJournalFile(string Ep, string FullPath, string DisplayName);

    private static HashSet<string>? NormalizeAllowedEps(IReadOnlyList<string>? allowed)
    {
        if (allowed is not { Count: > 0 })
            return null;
        var hs = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var a in allowed)
        {
            var t = (a ?? "").Trim();
            if (t.Length > 0)
                hs.Add(t);
        }

        return hs.Count > 0 ? hs : null;
    }

    private static int CountUnifiedTxtParts(string zipPath)
    {
        using var z = ZipFile.OpenRead(zipPath);
        return z.Entries.Count(e =>
            Path.GetFileName(e.FullName).StartsWith("unificado", StringComparison.OrdinalIgnoreCase));
    }

    private static int CountZipEntries(string zipPath)
    {
        using var z = ZipFile.OpenRead(zipPath);
        return z.Entries.Count;
    }

    private static async Task WriteMultiZipFlatAsync(string temp, List<ResolvedJournalFile> resolved, CancellationToken ct)
    {
        await using (var fs = new FileStream(temp, FileMode.Create, FileAccess.Write, FileShare.None, 4096, FileOptions.Asynchronous))
        using (var zip = new ZipArchive(fs, ZipArchiveMode.Create, leaveOpen: false))
        {
            var used = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var r in resolved)
            {
                ct.ThrowIfCancellationRequested();
                var entryName = SanitizeZipEntryName(r.DisplayName);
                entryName = MakeUnique(entryName, used);
                var entry = zip.CreateEntry(entryName, CompressionLevel.Optimal);
                await using (var entryStream = entry.Open())
                await using (var src = new FileStream(r.FullPath, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, FileOptions.Asynchronous))
                {
                    await src.CopyToAsync(entryStream, ct).ConfigureAwait(false);
                }
            }
        }
    }

    private static async Task WriteMultiZipEpFoldersAsync(string temp, List<ResolvedJournalFile> resolved, CancellationToken ct)
    {
        await using (var fs = new FileStream(temp, FileMode.Create, FileAccess.Write, FileShare.None, 4096, FileOptions.Asynchronous))
        using (var zip = new ZipArchive(fs, ZipArchiveMode.Create, leaveOpen: false))
        {
            var used = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var r in resolved)
            {
                ct.ThrowIfCancellationRequested();
                var folder = SanitizeZipSegment(r.Ep);
                var inner = SanitizeZipEntryName(r.DisplayName);
                var entryName = $"{folder}/{inner}";
                entryName = MakeUnique(entryName, used);
                var entry = zip.CreateEntry(entryName, CompressionLevel.Optimal);
                await using (var entryStream = entry.Open())
                await using (var src = new FileStream(r.FullPath, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, FileOptions.Asynchronous))
                {
                    await src.CopyToAsync(entryStream, ct).ConfigureAwait(false);
                }
            }
        }
    }

    private static string MakeUnique(string name, HashSet<string> used)
    {
        var baseName = name;
        var n = 1;
        while (!used.Add(name))
        {
            var ext = Path.GetExtension(baseName);
            var stem = ext.Length > 0 ? baseName[..^ext.Length] : baseName;
            name = $"{stem}_{n}{ext}";
            n++;
        }

        return name;
    }

    private static async Task WriteUnifiedZipSingleEpAsync(
        string temp,
        List<ResolvedJournalFile> resolved,
        JournalHistoricoOptions opt,
        CancellationToken ct)
    {
        await using var ms = await BuildUnifiedMemoryStreamAsync(resolved, opt, ct).ConfigureAwait(false);
        var fullBytes = ms.ToArray();
        var partSize = Math.Max(4096, opt.UnifiedPartMaxBytes);

        await using (var fs = new FileStream(temp, FileMode.Create, FileAccess.Write, FileShare.None, 4096, FileOptions.Asynchronous))
        using (var zip = new ZipArchive(fs, ZipArchiveMode.Create, leaveOpen: false))
        {
            var partIndex = 0;
            for (var offset = 0; offset < fullBytes.Length; offset += partSize)
            {
                partIndex++;
                var len = Math.Min(partSize, fullBytes.Length - offset);
                var name = partIndex == 1 ? "unificado.txt" : $"unificado_parte_{partIndex}.txt";
                var e = zip.CreateEntry(name, CompressionLevel.Optimal);
                await using var es = e.Open();
                await es.WriteAsync(fullBytes.AsMemory(offset, len), ct).ConfigureAwait(false);
            }
        }
    }

    private static async Task WriteUnifiedZipPerEpFoldersAsync(
        string temp,
        List<ResolvedJournalFile> resolved,
        JournalHistoricoOptions opt,
        CancellationToken ct)
    {
        var groups = resolved.GroupBy(r => r.Ep, StringComparer.OrdinalIgnoreCase).OrderBy(g => g.Key, StringComparer.OrdinalIgnoreCase).ToList();
        await using (var fs = new FileStream(temp, FileMode.Create, FileAccess.Write, FileShare.None, 4096, FileOptions.Asynchronous))
        using (var zip = new ZipArchive(fs, ZipArchiveMode.Create, leaveOpen: false))
        {
            foreach (var grp in groups)
            {
                ct.ThrowIfCancellationRequested();
                await using var ms = await BuildUnifiedMemoryStreamAsync(grp.ToList(), opt, ct).ConfigureAwait(false);
                var fullBytes = ms.ToArray();
                var partSize = Math.Max(4096, opt.UnifiedPartMaxBytes);
                var folder = SanitizeZipSegment(grp.Key);
                var partIndex = 0;
                for (var offset = 0; offset < fullBytes.Length; offset += partSize)
                {
                    partIndex++;
                    var len = Math.Min(partSize, fullBytes.Length - offset);
                    var name = partIndex == 1
                        ? $"{folder}/unificado.txt"
                        : $"{folder}/unificado_parte_{partIndex}.txt";
                    var e = zip.CreateEntry(name, CompressionLevel.Optimal);
                    await using var es = e.Open();
                    await es.WriteAsync(fullBytes.AsMemory(offset, len), ct).ConfigureAwait(false);
                }
            }
        }
    }

    private static async Task<MemoryStream> BuildUnifiedMemoryStreamAsync(List<ResolvedJournalFile> resolved, JournalHistoricoOptions opt, CancellationToken ct)
    {
        long total = 0;
        foreach (var r in resolved)
        {
            var fi = new FileInfo(r.FullPath);
            total += Math.Min(fi.Length, opt.MaxSourceFileReadBytes);
        }

        if (total > opt.UnifiedPartMaxBytes * 4L)
            throw new InvalidOperationException("La selección es demasiado grande para unificar en memoria; descargue sin unificar o reduzca archivos.");

        var sb = new StringBuilder();
        foreach (var r in resolved.OrderBy(x => x.DisplayName, StringComparer.OrdinalIgnoreCase))
        {
            ct.ThrowIfCancellationRequested();
            sb.AppendLine($"--- {r.DisplayName} ---");
            var bytes = await ReadFileCappedAsync(r.FullPath, opt.MaxSourceFileReadBytes, ct).ConfigureAwait(false);
            sb.Append(Encoding.UTF8.GetString(bytes));
            sb.AppendLine();
        }

        var text = sb.ToString();
        var enc = new UTF8Encoding(encoderShouldEmitUTF8Identifier: true);
        return new MemoryStream(enc.GetBytes(text));
    }

    private static async Task<byte[]> ReadFileCappedAsync(string path, int maxBytes, CancellationToken ct)
    {
        await using var s = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, FileOptions.Asynchronous);
        var len = (int)Math.Min(maxBytes, s.Length);
        var buf = new byte[len];
        var read = await s.ReadAsync(buf.AsMemory(0, len), ct).ConfigureAwait(false);
        return read < len ? buf.AsSpan(0, read).ToArray() : buf;
    }

    private static bool IsMergeable(string path)
    {
        var ext = Path.GetExtension(path);
        return MergeableExtensions.Contains(ext, StringComparer.OrdinalIgnoreCase);
    }

    private static bool TryResolveId(
        string rootFull,
        string id,
        HashSet<string>? allowedEps,
        out string ep,
        out string fullPath,
        out string displayName)
    {
        ep = string.Empty;
        fullPath = string.Empty;
        displayName = string.Empty;

        if (string.IsNullOrWhiteSpace(id))
            return false;

        if (id.StartsWith("n:", StringComparison.Ordinal))
            return TryResolveNuevoLayout(rootFull, id[2..], allowedEps, ref ep, out fullPath, out displayName);

        if (id.StartsWith("l:", StringComparison.Ordinal))
            return TryResolveLegacy(rootFull, id[2..], allowedEps, ref ep, out fullPath, out displayName);

        return false;
    }

    /// <summary>
    /// Identificador del layout nuevo: base64url de "ep|lote|rutaRelativa".
    /// Cada validacion evita salir del directorio raiz; no eliminar ninguna.
    /// </summary>
    private static bool TryResolveNuevoLayout(
        string rootFull,
        string payloadCodificado,
        HashSet<string>? allowedEps,
        ref string ep,
        out string fullPath,
        out string displayName)
    {
        fullPath = string.Empty;
        displayName = string.Empty;

        var payload = FromBase64Url(payloadCodificado);
        if (payload is null)
            return false;

        var parts = payload.Split('|', 3);
        if (parts.Length != 3)
            return false;

        try
        {
            ep = NormalizeAndValidateEp(parts[0]);
        }
        catch (ArgumentException)
        {
            return false;
        }

        var lote = parts[1].Trim();
        var rel = parts[2].Replace('/', Path.DirectorySeparatorChar).Trim();

        if (!EsSegmentoLoteValido(lote))
            return false;

        if (string.IsNullOrWhiteSpace(rel) || Path.IsPathRooted(rel) || !IsSafeRelativePath(rel))
            return false;

        if (allowedEps is not null && !allowedEps.Contains(ep))
            return false;

        var epDirectory = FindChildDirectory(rootFull, ep);
        if (epDirectory is null)
            return false;

        var loteDirectory = FindChildDirectory(epDirectory, lote);
        if (loteDirectory is null)
            return false;

        var normalizedRelative = rel
            .Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);

        var matchedFile = Directory.EnumerateFiles(loteDirectory, "*", SearchOption.AllDirectories)
            .FirstOrDefault(path => string.Equals(
                Path.GetRelativePath(loteDirectory, path),
                normalizedRelative,
                StringComparison.OrdinalIgnoreCase));

        if (matchedFile is null)
            return false;

        fullPath = matchedFile;
        displayName = normalizedRelative.Replace(Path.DirectorySeparatorChar, '/');
        return true;
    }

    /// <summary>
    /// Identificador legacy: base64url del nombre de archivo en la raiz, con prefijo de EP.
    /// Cada validacion evita salir del directorio raiz; no eliminar ninguna.
    /// </summary>
    private static bool TryResolveLegacy(
        string rootFull,
        string nombreCodificado,
        HashSet<string>? allowedEps,
        ref string ep,
        out string fullPath,
        out string displayName)
    {
        fullPath = string.Empty;
        displayName = string.Empty;

        var name = FromBase64Url(nombreCodificado);
        if (string.IsNullOrEmpty(name) || !EsNombreArchivoPlano(name))
            return false;

        var epPrefix = ExtractEpLegacyPrefix(name);
        if (epPrefix is null)
            return false;

        ep = epPrefix;

        if (allowedEps is not null && !allowedEps.Contains(epPrefix))
            return false;

        if (!name.StartsWith(epPrefix + "_", StringComparison.OrdinalIgnoreCase))
            return false;

        var matchedFile = Directory.EnumerateFiles(rootFull)
            .FirstOrDefault(path => string.Equals(
                Path.GetFileName(path),
                name,
                StringComparison.OrdinalIgnoreCase));

        if (matchedFile is null)
            return false;

        fullPath = matchedFile;
        displayName = name;
        return true;
    }

    /// <summary>El lote debe ser un nombre de carpeta simple, sin rutas ni referencias al padre.</summary>
    private static bool EsSegmentoLoteValido(string lote) =>
        !string.IsNullOrWhiteSpace(lote) &&
        lote is not ("." or "..") &&
        !lote.Contains("..", StringComparison.Ordinal) &&
        lote.IndexOfAny(Path.GetInvalidFileNameChars()) < 0 &&
        !Path.IsPathRooted(lote);

    /// <summary>El nombre debe ser un archivo suelto, sin componentes de ruta.</summary>
    private static bool EsNombreArchivoPlano(string name) =>
        name.IndexOfAny(Path.GetInvalidFileNameChars()) < 0 &&
        string.Equals(Path.GetFileName(name), name, StringComparison.Ordinal) &&
        !Path.IsPathRooted(name) &&
        !name.Contains("..", StringComparison.Ordinal);

    private static string? ExtractEpLegacyPrefix(string fileName)
    {
        var underscore = fileName.IndexOf('_');
        return underscore <= 0 ? null : fileName[..underscore];
    }

    private static string SanitizeZipSegment(string segment)
    {
        var s = (segment ?? "ep").Trim();
        foreach (var c in Path.GetInvalidFileNameChars())
            s = s.Replace(c, '_');
        foreach (var c in @"\/:*?""<>|".ToCharArray())
            s = s.Replace(c, '_');
        return string.IsNullOrWhiteSpace(s) ? "ep" : (s.Length > 80 ? s[..80] : s);
    }

    private static string SanitizeZipEntryName(string name)
    {
        var s = name.Replace(Path.DirectorySeparatorChar, '_').Replace(Path.AltDirectorySeparatorChar, '_');
        foreach (var c in Path.GetInvalidFileNameChars())
            s = s.Replace(c, '_');
        if (s.Length > 120)
            s = s[..120];
        return string.IsNullOrWhiteSpace(s) ? "archivo" : s;
    }

    private static string ToBase64Url(string s)
    {
        var bytes = Encoding.UTF8.GetBytes(s);
        return Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
    }

    private static string? FromBase64Url(string input)
    {
        try
        {
            var s = input.Replace('-', '+').Replace('_', '/');
            switch (s.Length % 4)
            {
                case 2: s += "=="; break;
                case 3: s += "="; break;
            }

            return Encoding.UTF8.GetString(Convert.FromBase64String(s));
        }
        catch
        {
            return null;
        }
    }
}
