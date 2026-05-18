using System.Collections.Generic;

namespace OrganizadorDescargas.Models;

public sealed class DuplicateGroup
{
    public long         FileSize { get; set; }
    public string       Hash     { get; set; } = "";
    public List<string> Files    { get; set; } = new();

    public string SizeDisplay => FormatSize(FileSize);

    public string WastedDisplay
    {
        get
        {
            long wasted = FileSize * (Files.Count - 1);
            return $"{FormatSize(wasted)} desperdiciados";
        }
    }

    private static string FormatSize(long bytes) => bytes switch
    {
        >= 1_073_741_824 => $"{bytes / 1_073_741_824.0:F1} GB",
        >= 1_048_576     => $"{bytes / 1_048_576.0:F1} MB",
        >= 1_024         => $"{bytes / 1_024.0:F0} KB",
        _                => $"{bytes} B",
    };
}
