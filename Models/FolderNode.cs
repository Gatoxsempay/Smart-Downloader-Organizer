namespace OrganizadorDescargas.Models;

public sealed class FolderNode
{
    public string Path        { get; }
    public string DisplayName { get; }
    public FolderNode(string path, string displayName) { Path = path; DisplayName = displayName; }
}
