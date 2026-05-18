using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace OrganizadorDescargas.Services;

public record UpdateInfo(string Version, string Url);

public static class UpdaterService
{
    private static readonly HttpClient Http = new();

    static UpdaterService()
    {
        Http.DefaultRequestHeaders.UserAgent.ParseAdd("OrganizadorDescargas/1.0");
    }

    public static async Task<UpdateInfo?> CheckAsync(string currentVersion, string url)
    {
        if (string.IsNullOrEmpty(url)) return null;
        try
        {
            var json = await Http.GetStringAsync(url);
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            var tag     = root.GetProperty("tag_name").GetString()?.TrimStart('v') ?? "";
            var htmlUrl = root.GetProperty("html_url").GetString() ?? "";
            if (!string.IsNullOrEmpty(tag) && !string.IsNullOrEmpty(htmlUrl)
                && System.Version.TryParse(tag, out var remote)
                && System.Version.TryParse(currentVersion, out var current)
                && remote > current)
                return new UpdateInfo(tag, htmlUrl);
        }
        catch { }
        return null;
    }
}
