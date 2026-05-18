using OrganizadorDescargas.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace OrganizadorDescargas.Services;

public sealed class GhostFilesService
{
    // Detecta archivos mayores a minBytes que no han sido accedidos en los
    // últimos monthsOld meses. Ordena de mayor a menor tamaño.
    public async Task<List<GhostFile>> FindAsync(
        IEnumerable<string> rootPaths,
        long minBytes,
        int monthsOld,
        IProgress<string> progress,
        CancellationToken ct)
    {
        return await Task.Run(() => Find(rootPaths, minBytes, monthsOld, progress, ct), ct);
    }

    private static List<GhostFile> Find(
        IEnumerable<string> rootPaths, long minBytes, int monthsOld,
        IProgress<string> progress, CancellationToken ct)
    {
        var cutoff = DateTime.Now.AddMonths(-monthsOld);
        var result = new List<GhostFile>();
        int scanned = 0;

        progress.Report("Escaneando archivos…");

        foreach (var rootPath in rootPaths)
        {
            try
            {
                foreach (var path in Directory.EnumerateFiles(rootPath, "*", SearchOption.AllDirectories))
                {
                    ct.ThrowIfCancellationRequested();
                    try
                    {
                        var fi = new FileInfo(path);
                        scanned++;

                        if (scanned % 500 == 0)
                            progress.Report($"Analizando… {scanned} archivos revisados");

                        if (fi.Length >= minBytes && fi.LastAccessTime < cutoff)
                        {
                            result.Add(new GhostFile
                            {
                                FullPath   = fi.FullName,
                                Name       = fi.Name,
                                SizeBytes  = fi.Length,
                                LastAccess = fi.LastAccessTime,
                                Modified   = fi.LastWriteTime,
                            });
                        }
                    }
                    catch { }
                }
            }
            catch (OperationCanceledException) { throw; }
            catch { }
        }

        result.Sort((a, b) => b.SizeBytes.CompareTo(a.SizeBytes));
        return result;
    }
}
