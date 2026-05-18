using System;

namespace OrganizadorDescargas.Models;

public sealed class FileEntry
{
    public required string   FullPath  { get; init; }
    public required string   Name      { get; init; }
    public required string   Extension { get; init; }
    public required long     SizeBytes { get; init; }
    public required DateTime Created   { get; init; }
    public required DateTime Modified  { get; init; }
    public          int      OpenCount { get; set; }

    public string SizeDisplay => SizeBytes switch
    {
        >= 1_073_741_824 => $"{SizeBytes / 1_073_741_824.0:F1} GB",
        >= 1_048_576     => $"{SizeBytes / 1_048_576.0:F1} MB",
        >= 1_024         => $"{SizeBytes / 1_024.0:F0} KB",
        _                => $"{SizeBytes} B",
    };

    public string ModifiedDisplay => Modified.ToString("dd/MM/yyyy  HH:mm");
    public string CreatedDisplay  => Created.ToString("dd/MM/yyyy  HH:mm");
}
