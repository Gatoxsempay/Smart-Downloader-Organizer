using System.Runtime.InteropServices;
using System.Text;

namespace OrganizadorDescargas.Services;

public static class WallpaperService
{
    const uint SPI_GETDESKWALLPAPER = 0x0073;

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    static extern bool SystemParametersInfo(uint uAction, uint uParam, StringBuilder lpvParam, uint fuWinIni);

    public static string? GetWallpaperPath()
    {
        var sb = new StringBuilder(260);
        return SystemParametersInfo(SPI_GETDESKWALLPAPER, 260, sb, 0) && sb.Length > 0
            ? sb.ToString()
            : null;
    }
}
