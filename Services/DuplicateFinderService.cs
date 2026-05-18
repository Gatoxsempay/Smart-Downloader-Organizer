using OrganizadorDescargas.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;

namespace OrganizadorDescargas.Services;

public sealed class DuplicateFinderService
{
    // Busca duplicados reales en rootPath usando el algoritmo de dos pasos:
    // 1) agrupar por tamaño, 2) hash MD5 para confirmar igualdad de contenido.
    public async Task<List<DuplicateGroup>> FindAsync(
        IEnumerable<string> rootPaths,
        IProgress<string> progress,
        CancellationToken ct)
    {
        return await Task.Run(() => Find(rootPaths, progress, ct), ct);
    }

    private static List<DuplicateGroup> Find(
        IEnumerable<string> rootPaths, IProgress<string> progress, CancellationToken ct)
    {
        // Paso 1: enumerar todos los archivos de todas las carpetas
        progress.Report("Enumerando archivos…");
        var allFiles = new List<FileInfo>();
        foreach (var rootPath in rootPaths)
        {
            try
            {
                foreach (var path in Directory.EnumerateFiles(rootPath, "*", SearchOption.AllDirectories))
                {
                    ct.ThrowIfCancellationRequested();
                    try { allFiles.Add(new FileInfo(path)); }
                    catch { }
                }
            }
            catch (OperationCanceledException) { throw; }
            catch { }
        }

        // Paso 2: agrupar por tamaño; descartar tamaños únicos
        progress.Report($"{allFiles.Count} archivos encontrados. Agrupando por tamaño…");
        var sizeGroups = allFiles
            .GroupBy(f => f.Length)
            .Where(g => g.Count() > 1)
            .ToList();

        if (sizeGroups.Count == 0) return [];

        // Paso 3: calcular hash MD5 solo para grupos con tamaño coincidente
        int totalToHash = sizeGroups.Sum(g => g.Count());
        int hashed      = 0;
        var result      = new List<DuplicateGroup>();

        foreach (var sizeGroup in sizeGroups)
        {
            ct.ThrowIfCancellationRequested();

            var hashBuckets = new Dictionary<string, List<string>>(StringComparer.Ordinal);

            foreach (var fi in sizeGroup)
            {
                ct.ThrowIfCancellationRequested();
                try
                {
                    string hash = ComputePartialMd5(fi.FullName, fi.Length);
                    if (!hashBuckets.TryGetValue(hash, out var list))
                        hashBuckets[hash] = list = new();
                    list.Add(fi.FullName);
                }
                catch { }

                hashed++;
                if (hashed % 200 == 0)
                    progress.Report($"Calculando hashes… {hashed}/{totalToHash}");
            }

            foreach (var (hash, paths) in hashBuckets)
            {
                if (paths.Count > 1)
                    result.Add(new DuplicateGroup
                    {
                        FileSize = sizeGroup.Key,
                        Hash     = hash,
                        Files    = paths,
                    });
            }
        }

        // Ordenar: primero los grupos que desperdician más espacio
        return result.OrderByDescending(g => g.FileSize * (g.Files.Count - 1)).ToList();
    }

    // Full MD5 for small files; first+last 64 KB for large files (> 50 MB)
    private static string ComputePartialMd5(string path, long fileSize)
    {
        const long threshold = 50L * 1024 * 1024;
        const int  chunk     = 64 * 1024;

        if (fileSize < threshold)
        {
            using var md5f    = MD5.Create();
            using var streamf = File.OpenRead(path);
            return Convert.ToHexString(md5f.ComputeHash(streamf));
        }

        using var md5    = MD5.Create();
        using var stream = File.OpenRead(path);
        var buffer = new byte[chunk];

        int read = stream.Read(buffer, 0, chunk);
        md5.TransformBlock(buffer, 0, read, null, 0);

        if (fileSize > chunk * 2)
        {
            stream.Seek(-chunk, SeekOrigin.End);
            read = stream.Read(buffer, 0, chunk);
        }
        md5.TransformFinalBlock(buffer, 0, read);
        return Convert.ToHexString(md5.Hash!);
    }
}
