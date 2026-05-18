using OrganizadorDescargas.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace OrganizadorDescargas.Services;

public sealed class FileMetadataService(FileUsageTracker tracker)
{
    public async Task<List<FileEntry>> ScanAsync(
        string folderPath,
        bool   recursive,
        IProgress<(int done, int total)>? progress = null,
        CancellationToken ct = default)
    {
        return await Task.Run(() =>
        {
            string[] files;
            try
            {
                files = Directory.GetFiles(
                    folderPath, "*.*",
                    recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly);
            }
            catch { return new List<FileEntry>(); }

            var result = new List<FileEntry>(files.Length);
            for (int i = 0; i < files.Length; i++)
            {
                ct.ThrowIfCancellationRequested();
                try
                {
                    var fi = new FileInfo(files[i]);
                    result.Add(new FileEntry
                    {
                        FullPath  = files[i],
                        Name      = fi.Name,
                        Extension = fi.Extension.ToLowerInvariant(),
                        SizeBytes = fi.Length,
                        Created   = fi.CreationTime,
                        Modified  = fi.LastWriteTime,
                        OpenCount = tracker.GetCount(files[i]),
                    });
                }
                catch { }

                if (i % 200 == 0) progress?.Report((i, files.Length));
            }
            progress?.Report((files.Length, files.Length));
            return result;
        }, ct);
    }
}
