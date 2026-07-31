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
        if (request.Eps is { Count: > 0 })
        {
            return request.Eps
                .Select(e => (e ?? string.Empty).Trim())
                .Where(e => e.Length > 0)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        var one = (request.Ep ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(one))
            throw new ArgumentException("Indique al menos una estación (eps o ep).", nameof(request));
        return [one];
    }

    private static void CollectNewLayout(
        string rootFull,
        string ep,
        DateTime desdeUtc,
        DateTime hastaUtc,
        List<JournalHistoricoItemDto> list,
        int maxItems)
    {
        var epDir = Path.Combine(rootFull, ep);
        if (!Directory.Exists(epDir))
            return;

        foreach (var loteDir in Directory.EnumerateDirectories(epDir))
        {
            if (list.Count >= maxItems)
                return;
            var loteId = Path.GetFileName(loteDir);
            foreach (var file in Directory.EnumerateFiles(loteDir, "*", SearchOption.AllDirectories))
            {
                if (list.Count >= maxItems)
                    return;
                var fi = new FileInfo(file);
                var t = fi.LastWriteTimeUtc;
                if (t < desdeUtc || t > hastaUtc)
                    continue;

                var rel = Path.GetRelativePath(loteDir, file).Replace(Path.DirectorySeparatorChar, '/');
                var id = "n:" + ToBase64Url($"{ep}|{loteId}|{rel}");
                list.Add(new JournalHistoricoItemDto
                {
                    Id = id,
                    Ep = ep,
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

        var allowed = NormalizeAllowedEps(request.AllowedEps);

        var resolved = new List<ResolvedJournalFile>();
        foreach (var id in ids)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!TryResolveId(rootFull, id, allowed, out var ep, out var full, out var display))
                throw new ArgumentException($"Id no válido o fuera de alcance: {id}");
            resolved.Add(new ResolvedJournalFile(ep, full, display));
        }

        var distinctEps = resolved.Select(r => r.Ep).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        var multiEpLayout = distinctEps.Count > 1;

        var stamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss", CultureInfo.InvariantCulture);
        string zipBaseName;
        if (distinctEps.Count == 1)
            zipBaseName = $"journal_{SanitizeZipSegment(distinctEps[0])}_{stamp}";
        else
            zipBaseName = $"journal_export_{stamp}";

        var zipFileName = zipBaseName + ".zip";

        if (request.Unificar)
        {
            foreach (var r in resolved)
            {
                if (!IsMergeable(r.FullPath))
                {
                    throw new InvalidOperationException(
                        "Unificar solo aplica a archivos de texto (.txt, .log, .journal, .csv, .json). Quite archivos binarios o desactive unificar.");
                }
            }

            if (!multiEpLayout)
                await WriteUnifiedZipSingleEpAsync(zipPath, resolved, opt, cancellationToken).ConfigureAwait(false);
            else
                await WriteUnifiedZipPerEpFoldersAsync(zipPath, resolved, opt, cancellationToken).ConfigureAwait(false);
        }
        else if (multiEpLayout)
        {
            await WriteMultiZipEpFoldersAsync(zipPath, resolved, cancellationToken).ConfigureAwait(false);
        }
        else
        {
            await WriteMultiZipFlatAsync(zipPath, resolved, cancellationToken).ConfigureAwait(false);
        }

        var modo = request.Unificar
            ? (multiEpLayout ? "unificado_por_ep" : "unificado")
            : (multiEpLayout ? "zip_con_carpetas" : "zip");

        var epsLabel = distinctEps.Count <= 5
            ? string.Join(", ", distinctEps.OrderBy(e => e, StringComparer.OrdinalIgnoreCase))
            : string.Join(", ", distinctEps.Take(3)) + "…";

        var partes = request.Unificar && !multiEpLayout
            ? CountUnifiedTxtParts(zipPath)
            : request.Unificar
                ? CountZipEntries(zipPath)
                : resolved.Count;

        List<string> archivosLista;
        if (request.Unificar && !multiEpLayout)
            archivosLista = resolved.Select(r => r.DisplayName).ToList();
        else if (multiEpLayout)
            archivosLista = resolved.Select(r => $"{r.Ep}/{r.DisplayName}").ToList();
        else
            archivosLista = resolved.Select(r => r.DisplayName).ToList();

        var summary = new JournalHistoricoDownloadSummaryDto
        {
            NombreZip = zipFileName,
            Ep = multiEpLayout ? epsLabel : distinctEps[0],
            ArchivosIncluidos = archivosLista,
            Modo = modo,
            Partes = partes
        };

        return (zipFileName, summary);
    }

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
        {
            var payload = FromBase64Url(id[2..]);
            if (payload is null)
                return false;
            var parts = payload.Split('|', 3);
            if (parts.Length != 3)
                return false;
            ep = parts[0];
            var lote = parts[1];
            var rel = parts[2].Replace('/', Path.DirectorySeparatorChar);
            if (allowedEps is not null && !allowedEps.Contains(ep))
                return false;
            var combined = Path.GetFullPath(Path.Combine(rootFull.TrimEnd(Path.DirectorySeparatorChar), ep, lote, rel));
            var rootTrim = rootFull.TrimEnd(Path.DirectorySeparatorChar);
            var baseEp = Path.GetFullPath(Path.Combine(rootTrim, ep));
            if (!baseEp.EndsWith(Path.DirectorySeparatorChar))
                baseEp += Path.DirectorySeparatorChar;
            if (!combined.StartsWith(baseEp, StringComparison.OrdinalIgnoreCase))
                return false;
            if (!File.Exists(combined))
                return false;
            fullPath = combined;
            displayName = string.IsNullOrEmpty(rel) ? Path.GetFileName(combined) : rel.Replace(Path.DirectorySeparatorChar, '/');
            return true;
        }

        if (id.StartsWith("l:", StringComparison.Ordinal))
        {
            var name = FromBase64Url(id[2..]);
            if (string.IsNullOrEmpty(name) || name.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
                return false;
            var epPrefix = ExtractEpLegacyPrefix(name);
            if (epPrefix is null)
                return false;
            ep = epPrefix;
            if (allowedEps is not null && !allowedEps.Contains(epPrefix))
                return false;
            if (!name.StartsWith(epPrefix + "_", StringComparison.OrdinalIgnoreCase))
                return false;
            var combined = Path.GetFullPath(Path.Combine(rootFull.TrimEnd(Path.DirectorySeparatorChar), name));
            var rootTrim = rootFull.TrimEnd(Path.DirectorySeparatorChar);
            if (!combined.StartsWith(rootTrim + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(combined, rootTrim, StringComparison.OrdinalIgnoreCase))
                return false;
            if (Directory.Exists(combined))
                return false;
            if (!File.Exists(combined))
                return false;
            fullPath = combined;
            displayName = name;
            return true;
        }

        return false;
    }

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
