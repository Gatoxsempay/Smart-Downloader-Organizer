namespace OrganizadorDescargas.Models;

public sealed class FileSystemItem
{
    public required string   FullPath    { get; init; }
    public required string   Name        { get; init; }
    public required bool     IsDirectory { get; init; }
    public string   Extension   { get; init; } = "";
    public long     SizeBytes   { get; init; }
    public DateTime Modified    { get; init; }
    public DateTime Created     { get; init; }
    public int      OpenCount   { get; set; }

    public string TypeDisplay     => IsDirectory ? "Carpeta" : Extension.ToUpperInvariant();
    public string SizeDisplay     => IsDirectory ? "—" : FormatSize(SizeBytes);
    public string ModifiedDisplay => Modified.ToString("dd/MM/yyyy  HH:mm");
    public string OpenDisplay     => IsDirectory ? "—" : OpenCount.ToString();

    public string Glyph => IsDirectory ? "\uE8B7" : Extension.ToLowerInvariant() switch
    {
        ".jpg" or ".jpeg" or ".png" or ".gif" or ".bmp" or ".webp" or ".svg" or ".ico" => "\uEB9F",
        ".mp4" or ".mkv" or ".avi" or ".mov" or ".wmv" or ".flv" or ".m4v"            => "\uE714",
        ".mp3" or ".wav" or ".flac" or ".aac" or ".ogg" or ".wma" or ".m4a"           => "\uEC4F",
        ".pdf"                                                                         => "\uEA90",
        ".doc" or ".docx" or ".txt" or ".rtf" or ".odt" or ".md"                      => "\uE8A5",
        ".xls" or ".xlsx" or ".csv" or ".ods"                                         => "\uE9D2",
        ".zip" or ".rar" or ".7z" or ".tar" or ".gz" or ".bz2"                        => "\uE8C8",
        ".exe" or ".msi" or ".bat" or ".cmd" or ".ps1"                                => "\uE756",
        ".cs" or ".py" or ".js" or ".ts" or ".html" or ".css" or ".cpp"
            or ".c" or ".h" or ".java" or ".go" or ".rs" or ".json"
            or ".xml" or ".yaml" or ".yml" or ".toml" or ".ini" or ".config" => "\uE943",
        _ => "\uE8A5",
    };

    private static string FormatSize(long bytes) => bytes switch
    {
        >= 1_073_741_824 => $"{bytes / 1_073_741_824.0:F1} GB",
        >= 1_048_576     => $"{bytes / 1_048_576.0:F1} MB",
        >= 1_024         => $"{bytes / 1_024.0:F0} KB",
        _                => $"{bytes} B",
    };
}
