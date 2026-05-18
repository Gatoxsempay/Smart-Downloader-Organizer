using System;

namespace OrganizadorDescargas.Models;

public sealed class GhostFile
{
    public string   FullPath   { get; set; } = "";
    public string   Name       { get; set; } = "";
    public long     SizeBytes  { get; set; }
    public DateTime LastAccess { get; set; }
    public DateTime Modified   { get; set; }

    public string SizeDisplay       => FormatSize(SizeBytes);
    public string LastAccessDisplay => LastAccess.ToString("dd/MM/yyyy");
    public string ModifiedDisplay   => Modified.ToString("dd/MM/yyyy");

    private static string FormatSize(long bytes) => bytes switch
    {
        >= 1_073_741_824 => $"{bytes / 1_073_741_824.0:F1} GB",
        >= 1_048_576     => $"{bytes / 1_048_576.0:F1} MB",
        >= 1_024         => $"{bytes / 1_024.0:F0} KB",
        _                => $"{bytes} B",
    };
}
