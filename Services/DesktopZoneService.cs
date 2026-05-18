using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Win32;
using OrganizadorDescargas.Models;

namespace OrganizadorDescargas.Services;

public enum DesktopZone { Apps, Dev, Documents, Media, Other }

public record DesktopIconInfo(string Name, int OriginalX, int OriginalY, DesktopZone Zone);

public record ZoneInfo(DesktopZone Zone, int X, int Y, int Width, int Height);
public record DesktopLayoutInfo(int ScreenW, int ScreenH, int IconW, int IconH, List<ZoneInfo> Zones);
public record StressTestResult(int Requested, int Arranged, int Overlaps, long ElapsedMs);

public class DesktopZoneService
{
    #region Win32
    const uint LVM_GETITEMCOUNT    = 0x1004;
    const uint LVM_GETITEMPOSITION = 0x1010;
    const uint LVM_SETITEMPOSITION = 0x100F;
    const uint LVM_GETITEMTEXTW    = 0x1073;
    const uint LVIF_TEXT           = 0x0001;
    const int  GWL_STYLE           = -16;
    const uint LVS_AUTOARRANGE     = 0x0100;
    const uint PROCESS_VM_READ     = 0x0010;
    const uint PROCESS_VM_WRITE    = 0x0020;
    const uint PROCESS_VM_OP       = 0x0008;
    const uint MEM_COMMIT          = 0x1000;
    const uint MEM_RELEASE         = 0x8000;
    const uint PAGE_READWRITE      = 0x04;
    const int  SM_CXICONSPACING    = 38;
    const int  SM_CYICONSPACING    = 39;
    const int  SM_CXSCREEN         = 0;
    const int  SM_CYSCREEN         = 1;

    [StructLayout(LayoutKind.Sequential)]
    struct POINT { public int X; public int Y; }

    [StructLayout(LayoutKind.Explicit)]
    struct LVITEMW
    {
        [FieldOffset( 0)] public uint   mask;
        [FieldOffset( 4)] public int    iItem;
        [FieldOffset( 8)] public int    iSubItem;
        [FieldOffset(12)] public uint   state;
        [FieldOffset(16)] public uint   stateMask;
        [FieldOffset(24)] public IntPtr pszText;
        [FieldOffset(32)] public int    cchTextMax;
        [FieldOffset(36)] public int    iImage;
        [FieldOffset(40)] public IntPtr lParam;
    }

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    static extern IntPtr FindWindow(string lpClassName, string? lpWindowName);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    static extern IntPtr FindWindowEx(IntPtr hWndParent, IntPtr hWndChildAfter,
                                      string lpszClass, string? lpszWindow);

    [DllImport("user32.dll")]
    static extern IntPtr SendMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll")]
    static extern IntPtr SendMessageTimeout(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam,
                                            uint fuFlags, uint uTimeout, out IntPtr lpdwResult);

    [DllImport("user32.dll")]
    static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

    [DllImport("kernel32.dll")]
    static extern IntPtr OpenProcess(uint dwDesiredAccess, bool bInheritHandle, uint dwProcessId);

    [DllImport("kernel32.dll")]
    static extern IntPtr VirtualAllocEx(IntPtr hProcess, IntPtr lpAddress,
                                        uint dwSize, uint flAllocationType, uint flProtect);

    [DllImport("kernel32.dll")]
    static extern bool VirtualFreeEx(IntPtr hProcess, IntPtr lpAddress, uint dwSize, uint dwFreeType);

    [DllImport("kernel32.dll")]
    static extern bool ReadProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress,
                                         byte[] lpBuffer, uint nSize, out uint lpNumberOfBytesRead);

    [DllImport("kernel32.dll")]
    static extern bool WriteProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress,
                                          byte[] lpBuffer, uint nSize, out uint lpNumberOfBytesWritten);

    [DllImport("kernel32.dll")]
    static extern bool CloseHandle(IntPtr hObject);

    [DllImport("user32.dll", EntryPoint = "GetWindowLongW")]
    static extern int GetWindowLong(IntPtr hWnd, int nIndex);

    [DllImport("user32.dll", EntryPoint = "SetWindowLongW")]
    static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

    [DllImport("user32.dll")]
    static extern int GetSystemMetrics(int nIndex);
    #endregion

    // ── dev tool names ─────────────────────────────────────────────────────────

    private static readonly HashSet<string> _devNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "visual studio code", "vs code", "code", "vscode",
        "visual studio", "visual studio 2022", "visual studio 2019", "visual studio 2017",
        "android studio", "pycharm", "pycharm community", "pycharm professional",
        "intellij idea", "intellij idea community", "intellij idea ultimate",
        "rider", "webstorm", "clion", "goland", "phpstorm", "datagrip", "rubymine",
        "eclipse", "netbeans", "xcode",
        "notepad++", "sublime text", "atom", "brackets",
        "git bash", "git gui", "github desktop", "github", "gitkraken", "sourcetree",
        "windows terminal", "terminal", "powershell", "powershell 7",
        "command prompt", "cmd", "wsl", "ubuntu", "debian", "kali linux",
        "node.js", "node.js command prompt", "python", "python 3", "python 3.11", "python 3.12",
        "ruby", "rust",
        "docker desktop", "docker", "podman",
        "postman", "insomnia",
        "db browser for sqlite", "dbeaver", "mysql workbench", "pgadmin 4",
        "mongodb compass", "tableplus",
        "inno setup compiler", "inno setup",
        "filezilla", "winscp", "putty", "mobaxterm",
        "xampp", "laragon",
        "fiddler", "wireshark",
        "roblox studio", "cursor",
    };

    private static readonly HashSet<string> _devExts = new(StringComparer.OrdinalIgnoreCase)
    {
        ".py", ".js", ".ts", ".jsx", ".tsx", ".java", ".cs", ".cpp", ".c",
        ".h", ".hpp", ".go", ".rs", ".rb", ".php", ".swift", ".kt", ".dart",
        ".html", ".css", ".scss", ".sass", ".less",
        ".json", ".xml", ".yaml", ".yml", ".toml", ".ini", ".cfg", ".env",
        ".sql", ".sh", ".ps1", ".bash", ".zsh",
        ".md", ".ipynb",
    };

    // ── desktop registry helper ────────────────────────────────────────────────

    private static string? ReadRegistryDesktopPath()
    {
        try
        {
            // "User Shell Folders" has the definitive, potentially-redirected path
            using var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(
                @"Software\Microsoft\Windows\CurrentVersion\Explorer\User Shell Folders");
            var raw = key?.GetValue("Desktop") as string;
            if (string.IsNullOrEmpty(raw)) return null;
            // Expand %USERPROFILE% and other env vars stored in the registry
            return Environment.ExpandEnvironmentVariables(raw);
        }
        catch { return null; }
    }

    // ── find desktop ListView ──────────────────────────────────────────────────

    private IntPtr FindDesktopListView()
    {
        var progman = FindWindow("Progman", null);
        if (progman == IntPtr.Zero) return IntPtr.Zero;

        SendMessageTimeout(progman, 0x052C, IntPtr.Zero, IntPtr.Zero, 0, 1000, out _);

        var defView = FindWindowEx(progman, IntPtr.Zero, "SHELLDLL_DefView", null);
        if (defView != IntPtr.Zero)
        {
            var lv = FindWindowEx(defView, IntPtr.Zero, "SysListView32", null);
            if (lv != IntPtr.Zero) return lv;
        }

        var workerW = IntPtr.Zero;
        while (true)
        {
            workerW = FindWindowEx(IntPtr.Zero, workerW, "WorkerW", null);
            if (workerW == IntPtr.Zero) break;
            defView = FindWindowEx(workerW, IntPtr.Zero, "SHELLDLL_DefView", null);
            if (defView != IntPtr.Zero)
            {
                var lv = FindWindowEx(defView, IntPtr.Zero, "SysListView32", null);
                if (lv != IntPtr.Zero) return lv;
            }
        }

        return IntPtr.Zero;
    }

    // ── public async API ───────────────────────────────────────────────────────

    // Returns path of the written diagnostic file
    public async Task<string> DiagnosticAsync() => await Task.Run(WriteDiagnosticFile);

    public async Task<(List<DesktopIconInfo> Icons, bool AutoArrangeOn)> ScanAsync()
        => await Task.Run(Scan);

    public async Task<int> ArrangeAsync(List<DesktopIconInfo> icons, OrganizationMode mode = OrganizationMode.ByZone)
        => await Task.Run(() => Arrange(icons, mode));

    public async Task RestoreAsync(List<DesktopIconInfo> icons)
        => await Task.Run(() => Restore(icons));

    public DesktopLayoutInfo GetDesktopLayoutInfo()
    {
        int sw = GetSystemMetrics(SM_CXSCREEN);
        int sh = GetSystemMetrics(SM_CYSCREEN);
        int iw = Math.Max(GetSystemMetrics(SM_CXICONSPACING), 90);
        int ih = Math.Max(GetSystemMetrics(SM_CYICONSPACING), 80);
        return new DesktopLayoutInfo(sw, sh, iw, ih, BuildZoneInfoList(sw, sh));
    }

    public List<(string Name, DesktopZone Zone, int X, int Y)> ComputeArrangement(
        List<DesktopIconInfo> icons, OrganizationMode mode = OrganizationMode.ByZone)
    {
        int sw = GetSystemMetrics(SM_CXSCREEN);
        int sh = GetSystemMetrics(SM_CYSCREEN);
        int iw = Math.Max(GetSystemMetrics(SM_CXICONSPACING), 90);
        int ih = Math.Max(GetSystemMetrics(SM_CYICONSPACING), 80);

        var zoneMap  = BuildZoneMap(sw, sh, iw);
        var counters = new Dictionary<DesktopZone, int>
        {
            [DesktopZone.Apps] = 0, [DesktopZone.Dev] = 0,
            [DesktopZone.Documents] = 0, [DesktopZone.Media] = 0, [DesktopZone.Other] = 0,
        };

        var sorted = ApplyMode(icons, mode);
        var result = new List<(string, DesktopZone, int, int)>(sorted.Count);
        foreach (var icon in sorted)
        {
            var (x0, y0, maxCols) = zoneMap[icon.Zone];
            int c   = counters[icon.Zone]++;
            int col = c % maxCols;
            int row = c / maxCols;
            result.Add((icon.Name, icon.Zone, x0 + col * iw, y0 + row * ih));
        }
        return result;
    }

    public async Task<StressTestResult> StressTestAsync(int iconCount)
    {
        return await Task.Run(() =>
        {
            var rng   = new Random(42);
            var zones = Enum.GetValues<DesktopZone>();
            var fake  = Enumerable.Range(1, iconCount)
                .Select(i => new DesktopIconInfo(
                    $"Icono_{i:D4}.lnk",
                    rng.Next(1920), rng.Next(1080),
                    zones[rng.Next(zones.Length)]))
                .ToList();

            var sw = System.Diagnostics.Stopwatch.StartNew();
            var arrangement = ComputeArrangement(fake, OrganizationMode.ByZone);
            sw.Stop();

            var posSet   = arrangement.Select(a => (a.X, a.Y)).ToHashSet();
            int overlaps = arrangement.Count - posSet.Count;
            return new StressTestResult(iconCount, arrangement.Count, overlaps, sw.ElapsedMilliseconds);
        });
    }

    // ── scan ──────────────────────────────────────────────────────────────────

    private (List<DesktopIconInfo> Icons, bool AutoArrangeOn) Scan()
    {
        var result = new List<DesktopIconInfo>();
        var hLV = FindDesktopListView();
        if (hLV == IntPtr.Zero)
            throw new InvalidOperationException(
                "No se encontró el ListView del escritorio (SysListView32). " +
                "Asegúrate de que el escritorio de Windows esté visible.");

        GetWindowThreadProcessId(hLV, out uint pidCheck);
        var hProcCheck = OpenProcess(PROCESS_VM_READ | PROCESS_VM_WRITE | PROCESS_VM_OP, false, pidCheck);
        if (hProcCheck == IntPtr.Zero)
            throw new InvalidOperationException(
                $"No se pudo abrir el proceso de Explorer (PID {pidCheck}). " +
                "Puede que necesites ejecutar la app como administrador.");
        CloseHandle(hProcCheck);

        bool autoArrangeOn = (GetWindowLong(hLV, GWL_STYLE) & (int)LVS_AUTOARRANGE) != 0;

        // Resolve desktop paths fresh on each scan (avoids stale static init issues)
        var desktopPaths = GetCurrentDesktopPaths();

        GetWindowThreadProcessId(hLV, out uint pid);
        var hProc = OpenProcess(PROCESS_VM_READ | PROCESS_VM_WRITE | PROCESS_VM_OP, false, pid);
        if (hProc == IntPtr.Zero) return (result, autoArrangeOn);

        try
        {
            int count = (int)SendMessage(hLV, LVM_GETITEMCOUNT, IntPtr.Zero, IntPtr.Zero);
            int lvSz  = Marshal.SizeOf<LVITEMW>();
            const int TxtSz = 520;

            var remLv = VirtualAllocEx(hProc, IntPtr.Zero, (uint)(lvSz + TxtSz), MEM_COMMIT, PAGE_READWRITE);
            var remPt = VirtualAllocEx(hProc, IntPtr.Zero, (uint)Marshal.SizeOf<POINT>(), MEM_COMMIT, PAGE_READWRITE);
            if (remLv == IntPtr.Zero || remPt == IntPtr.Zero)
            {
                if (remLv != IntPtr.Zero) VirtualFreeEx(hProc, remLv, 0, MEM_RELEASE);
                if (remPt != IntPtr.Zero) VirtualFreeEx(hProc, remPt, 0, MEM_RELEASE);
                return (result, autoArrangeOn);
            }
            var remTxt = IntPtr.Add(remLv, lvSz);

            var zeroBuf = new byte[TxtSz];
            byte[] tb   = new byte[TxtSz];
            byte[] pb   = new byte[Marshal.SizeOf<POINT>()];
            try
            {
                for (int i = 0; i < count; i++)
                {
                    // Zero the remote text buffer before each read to prevent stale bytes
                    WriteProcessMemory(hProc, remTxt, zeroBuf, TxtSz, out _);

                    var lv = new LVITEMW { mask = LVIF_TEXT, iItem = i, pszText = remTxt, cchTextMax = 260 };
                    WriteRemote(hProc, remLv, lv);
                    SendMessage(hLV, LVM_GETITEMTEXTW, (IntPtr)i, remLv);

                    ReadProcessMemory(hProc, remTxt, tb, TxtSz, out _);
                    string raw  = Encoding.Unicode.GetString(tb);
                    int    nul  = raw.IndexOf('\0');
                    string name = (nul >= 0 ? raw[..nul] : raw).Trim();
                    if (string.IsNullOrWhiteSpace(name)) continue;

                    SendMessage(hLV, LVM_GETITEMPOSITION, (IntPtr)i, remPt);
                    ReadProcessMemory(hProc, remPt, pb, (uint)pb.Length, out _);
                    var pt = FromBytes<POINT>(pb);

                    var zone = ClassifyIconDirect(name, desktopPaths);

                    result.Add(new DesktopIconInfo(name, pt.X, pt.Y, zone));
                }
            }
            finally
            {
                VirtualFreeEx(hProc, remLv, 0, MEM_RELEASE);
                VirtualFreeEx(hProc, remPt, 0, MEM_RELEASE);
            }
        }
        finally { CloseHandle(hProc); }

        return (result, autoArrangeOn);
    }

    // ── arrange in zones ──────────────────────────────────────────────────────

    private int Arrange(List<DesktopIconInfo> icons, OrganizationMode mode)
    {
        var hLV = FindDesktopListView();
        if (hLV == IntPtr.Zero) return 0;

        int style = GetWindowLong(hLV, GWL_STYLE);
        if ((style & (int)LVS_AUTOARRANGE) != 0)
            SetWindowLong(hLV, GWL_STYLE, style & ~(int)LVS_AUTOARRANGE);

        int screenW = GetSystemMetrics(SM_CXSCREEN);
        int screenH = GetSystemMetrics(SM_CYSCREEN);
        int iconW   = Math.Max(GetSystemMetrics(SM_CXICONSPACING), 90);
        int iconH   = Math.Max(GetSystemMetrics(SM_CYICONSPACING), 80);

        var zoneMap  = BuildZoneMap(screenW, screenH, iconW);
        var counters = new Dictionary<DesktopZone, int>
        {
            [DesktopZone.Apps] = 0, [DesktopZone.Dev] = 0, [DesktopZone.Documents] = 0,
            [DesktopZone.Media] = 0, [DesktopZone.Other] = 0,
        };

        var sorted = ApplyMode(icons, mode);

        return WithRemoteListView(hLV, (hProc, hLV2, nameToIdx) =>
        {
            int moved = 0;
            foreach (var icon in sorted)
            {
                if (!nameToIdx.TryGetValue(icon.Name, out int idx)) continue;
                var (x0, y0, maxCols) = zoneMap[icon.Zone];
                int c   = counters[icon.Zone]++;
                int col = c % maxCols;
                int row = c / maxCols;
                int x   = x0 + col * iconW;
                int y   = y0 + row * iconH;
                int lp  = (y << 16) | (x & 0xFFFF);
                SendMessage(hLV2, LVM_SETITEMPOSITION, (IntPtr)idx, (IntPtr)lp);
                moved++;
            }
            return moved;
        });
    }

    // ── zone layout helpers ────────────────────────────────────────────────────

    private static Dictionary<DesktopZone, (int x0, int y0, int maxCols)> BuildZoneMap(
        int screenW, int screenH, int iconW)
    {
        const int pad = 12, taskbarH = 56;
        int usableH    = screenH - taskbarH;
        int folderColW = screenW / 5;
        int workW      = screenW - folderColW;
        int thirdWorkW = workW / 3;
        int halfH      = usableH / 2;

        return new Dictionary<DesktopZone, (int, int, int)>
        {
            [DesktopZone.Apps]      = (pad,                    pad,         Math.Max(1, (thirdWorkW - pad * 2) / iconW)),
            [DesktopZone.Dev]       = (thirdWorkW     + pad,   pad,         Math.Max(1, (thirdWorkW - pad * 2) / iconW)),
            [DesktopZone.Documents] = (thirdWorkW * 2 + pad,   pad,         Math.Max(1, (thirdWorkW - pad * 2) / iconW)),
            [DesktopZone.Media]     = (pad,                    halfH + pad, Math.Max(1, (workW - pad * 2) / iconW)),
            [DesktopZone.Other]     = (workW          + pad,   pad,         Math.Max(1, (folderColW - pad * 2) / iconW)),
        };
    }

    private static List<ZoneInfo> BuildZoneInfoList(int screenW, int screenH)
    {
        const int pad = 12, taskbarH = 56;
        int usableH    = screenH - taskbarH;
        int folderColW = screenW / 5;
        int workW      = screenW - folderColW;
        int thirdWorkW = workW / 3;
        int halfH      = usableH / 2;

        return new List<ZoneInfo>
        {
            new(DesktopZone.Apps,      pad,                    pad,         thirdWorkW - pad, halfH),
            new(DesktopZone.Dev,       thirdWorkW     + pad,   pad,         thirdWorkW - pad, halfH),
            new(DesktopZone.Documents, thirdWorkW * 2 + pad,   pad,         thirdWorkW - pad, halfH),
            new(DesktopZone.Media,     pad,                    halfH + pad, workW - pad * 2,  halfH),
            new(DesktopZone.Other,     workW          + pad,   pad,         folderColW - pad, usableH),
        };
    }

    private static List<DesktopIconInfo> ApplyMode(List<DesktopIconInfo> icons, OrganizationMode mode) =>
        mode switch
        {
            OrganizationMode.Alphabetical => icons
                .OrderBy(i => i.Zone)
                .ThenBy(i => i.Name, StringComparer.OrdinalIgnoreCase)
                .ToList(),
            OrganizationMode.ByExtension => icons
                .OrderBy(i => i.Zone)
                .ThenBy(i => Path.GetExtension(i.Name).ToLowerInvariant())
                .ThenBy(i => i.Name, StringComparer.OrdinalIgnoreCase)
                .ToList(),
            _ => icons.OrderBy(i => i.Zone).ToList(),
        };

    // ── restore original positions ─────────────────────────────────────────────

    private void Restore(List<DesktopIconInfo> icons)
    {
        var hLV = FindDesktopListView();
        if (hLV == IntPtr.Zero) return;

        WithRemoteListView(hLV, (hProc, hLV2, nameToIdx) =>
        {
            foreach (var icon in icons)
            {
                if (!nameToIdx.TryGetValue(icon.Name, out int idx)) continue;
                int lp = (icon.OriginalY << 16) | (icon.OriginalX & 0xFFFF);
                SendMessage(hLV2, LVM_SETITEMPOSITION, (IntPtr)idx, (IntPtr)lp);
            }
            return 0;
        });
    }

    // ── shared cross-process helper ───────────────────────────────────────────

    private T WithRemoteListView<T>(IntPtr hLV, Func<IntPtr, IntPtr, Dictionary<string, int>, T> action)
    {
        GetWindowThreadProcessId(hLV, out uint pid);
        var hProc = OpenProcess(PROCESS_VM_READ | PROCESS_VM_WRITE | PROCESS_VM_OP, false, pid);
        if (hProc == IntPtr.Zero) return default!;

        try
        {
            int count = (int)SendMessage(hLV, LVM_GETITEMCOUNT, IntPtr.Zero, IntPtr.Zero);
            int lvSz  = Marshal.SizeOf<LVITEMW>();
            const int TxtSz = 520;

            var remLv = VirtualAllocEx(hProc, IntPtr.Zero, (uint)(lvSz + TxtSz), MEM_COMMIT, PAGE_READWRITE);
            if (remLv == IntPtr.Zero) return default!;
            var remTxt = IntPtr.Add(remLv, lvSz);

            var zeroBuf = new byte[TxtSz];
            byte[] tb   = new byte[TxtSz];
            try
            {
                var map = new Dictionary<string, int>(count);
                for (int i = 0; i < count; i++)
                {
                    WriteProcessMemory(hProc, remTxt, zeroBuf, TxtSz, out _);
                    var lv = new LVITEMW { mask = LVIF_TEXT, iItem = i, pszText = remTxt, cchTextMax = 260 };
                    WriteRemote(hProc, remLv, lv);
                    SendMessage(hLV, LVM_GETITEMTEXTW, (IntPtr)i, remLv);
                    ReadProcessMemory(hProc, remTxt, tb, TxtSz, out _);
                    string raw = Encoding.Unicode.GetString(tb);
                    int    nul = raw.IndexOf('\0');
                    string n   = (nul >= 0 ? raw[..nul] : raw).Trim();
                    if (!string.IsNullOrWhiteSpace(n)) map[n] = i;
                }
                return action(hProc, hLV, map);
            }
            finally { VirtualFreeEx(hProc, remLv, 0, MEM_RELEASE); }
        }
        finally { CloseHandle(hProc); }
    }

    // ── classification: direct filesystem lookup ──────────────────────────────

    // Resolve desktop paths on demand — avoids issues with static initialization.
    private static string[] GetCurrentDesktopPaths()
    {
        var seen   = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var result = new List<string>();

        void TryAdd(string? p)
        {
            if (string.IsNullOrEmpty(p)) return;
            p = p.TrimEnd('\\', '/');
            if (seen.Add(p) && Directory.Exists(p))
                result.Add(p);
        }

        // Registry first — handles folder redirections via Group Policy / OneDrive
        TryAdd(ReadRegistryDesktopPath());

        // SpecialFolder enum
        TryAdd(Environment.GetFolderPath(Environment.SpecialFolder.Desktop));
        TryAdd(Environment.GetFolderPath(Environment.SpecialFolder.CommonDesktopDirectory));

        // Explicit UserProfile paths
        var profile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        if (string.IsNullOrEmpty(profile))
            profile = Path.Combine(@"C:\Users", Environment.UserName);

        TryAdd(Path.Combine(profile, "Desktop"));
        TryAdd(Path.Combine(profile, "Escritorio"));

        // OneDrive variants
        try
        {
            foreach (var dir in Directory.EnumerateDirectories(profile, "OneDrive*"))
            {
                TryAdd(Path.Combine(dir, "Desktop"));
                TryAdd(Path.Combine(dir, "Escritorio"));
            }
        }
        catch { }

        // SystemDrive last-resort
        var drive = Environment.GetEnvironmentVariable("SystemDrive") ?? "C:";
        TryAdd(Path.Combine(drive, "Users", Environment.UserName, "Desktop"));
        TryAdd(Path.Combine(drive, "Users", Environment.UserName, "Escritorio"));

        return result.ToArray();
    }

    // Classify an icon by checking the filesystem directly.
    // Checks folders first (→ Other), then tries .lnk/.url/.exe (→ Apps or Dev),
    // then enumerates any file with that base name (→ classify by extension).
    // Falls back to name-based heuristics for shell virtual items.
    private static DesktopZone ClassifyIconDirect(string iconName, string[] desktopPaths)
    {
        foreach (var desktopPath in desktopPaths)
        {
            // Is it a folder?
            if (Directory.Exists(Path.Combine(desktopPath, iconName)))
                return DesktopZone.Other;

            // Is it a known shortcut/executable type?
            foreach (var ext in new[] { ".lnk", ".url", ".exe", ".bat", ".cmd" })
            {
                if (File.Exists(Path.Combine(desktopPath, iconName + ext)))
                    return ClassifyFileEntry(iconName, ext);
            }

            // Try to find any file with this base name (doc, media, etc.)
            try
            {
                foreach (var f in Directory.EnumerateFiles(desktopPath, iconName + ".*"))
                {
                    var ext = Path.GetExtension(f).ToLowerInvariant();
                    return ClassifyFileEntry(iconName, ext);
                }
            }
            catch { }
        }

        // Fuzzy fallback: Windows shell sometimes shows a display name (via desktop.ini
        // LocalizedResourceName) that differs from the actual folder name on disk.
        // E.g. folder "music" shown as "Música" in the shell. Strip diacritics + prefix-match.
        string normIcon = StripDiacritics(iconName).ToLowerInvariant();
        foreach (var dp in desktopPaths)
        {
            try
            {
                foreach (var dir in Directory.EnumerateDirectories(dp))
                {
                    string normDir = StripDiacritics(Path.GetFileName(dir)).ToLowerInvariant();
                    if (normDir.Length >= 4 && (normIcon.StartsWith(normDir) || normDir.StartsWith(normIcon)))
                        return DesktopZone.Other;
                }
                foreach (var f in Directory.EnumerateFiles(dp))
                {
                    string baseName = Path.GetFileNameWithoutExtension(f);
                    string normBase = StripDiacritics(baseName).ToLowerInvariant();
                    if (normBase.Length >= 4 && (normIcon.StartsWith(normBase) || normBase.StartsWith(normIcon)))
                        return ClassifyFileEntry(baseName, Path.GetExtension(f).ToLowerInvariant());
                }
            }
            catch { }
        }

        // Not found on disk → shell virtual item (Recycle Bin, This PC, etc.)
        return FallbackClassify(iconName);
    }

    private static string StripDiacritics(string s)
    {
        var d  = s.Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder(d.Length);
        foreach (char c in d)
            if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                sb.Append(c);
        return sb.ToString();
    }

    // Classify a file we found on disk by its name (without extension) and extension
    private static DesktopZone ClassifyFileEntry(string nameNoExt, string ext)
    {
        if (_devExts.Contains(ext))
            return DesktopZone.Dev;

        if (ext is ".pdf" or ".doc" or ".docx" or ".xls" or ".xlsx"
                 or ".ppt" or ".pptx" or ".txt" or ".odt" or ".ods"
                 or ".csv" or ".rtf" or ".pages" or ".numbers" or ".epub")
            return DesktopZone.Documents;

        if (ext is ".jpg" or ".jpeg" or ".png" or ".gif" or ".bmp" or ".webp" or ".svg"
                 or ".mp3" or ".mp4" or ".wav" or ".avi" or ".mkv" or ".mov"
                 or ".flac" or ".aac" or ".m4a" or ".wmv" or ".m4v" or ".heic"
                 or ".psd" or ".ai" or ".xcf" or ".raw" or ".ico")
            return DesktopZone.Media;

        // Shortcuts and executables: check if the target name is a known dev tool
        if (ext is ".lnk" or ".url" or ".exe" or ".bat" or ".cmd")
            return _devNames.Contains(nameNoExt) ? DesktopZone.Dev : DesktopZone.Apps;

        if (ext is ".zip" or ".rar" or ".7z" or ".tar" or ".gz" or ".bz2" or ".xz" or ".iso")
            return DesktopZone.Other;

        return DesktopZone.Other;
    }

    // Fallback for items not found in the filesystem index (special shell items:
    // Papelera de reciclaje, Este equipo, Red, etc.)
    private static DesktopZone FallbackClassify(string name)
    {
        int dot = name.LastIndexOf('.');
        string ext = dot >= 0 ? name[dot..].ToLowerInvariant() : "";

        if (ext != "")
            return ClassifyFileEntry(dot >= 0 ? name[..dot] : name, ext);

        if (_devNames.Contains(name))
            return DesktopZone.Dev;

        // Recycle Bin, This PC, Network, etc.
        return DesktopZone.Apps;
    }

    // ── diagnostic ────────────────────────────────────────────────────────────

    private string WriteDiagnosticFile()
    {
        var sb = new StringBuilder();

        sb.AppendLine("=== RUTAS DEL ESCRITORIO ===");
        sb.AppendLine($"Usuario           : {Environment.UserName}");
        sb.AppendLine($"UserProfile       : {Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)}");
        sb.AppendLine($"SpecialFolder     : {Environment.GetFolderPath(Environment.SpecialFolder.Desktop)}");
        sb.AppendLine($"Registro (raw)    : {ReadRegistryDesktopPath() ?? "(nulo)"}");

        var paths = GetCurrentDesktopPaths();
        sb.AppendLine($"Paths activos     : {paths.Length}");
        foreach (var p in paths)
            sb.AppendLine($"  ✓ {p}");
        if (paths.Length == 0)
            sb.AppendLine("  *** NINGUNO ENCONTRADO — todas las rutas fallaron ***");

        sb.AppendLine();
        sb.AppendLine("=== ARCHIVOS EN ESCRITORIO ===");
        foreach (var desktopPath in paths)
        {
            sb.AppendLine($"--- {desktopPath} ---");
            try
            {
                foreach (var d in Directory.EnumerateDirectories(desktopPath))
                    sb.AppendLine($"  [DIR ] {Path.GetFileName(d)}");
                foreach (var f in Directory.EnumerateFiles(desktopPath))
                    sb.AppendLine($"  [FILE] {Path.GetFileName(f)}");
            }
            catch (Exception ex) { sb.AppendLine($"  ERROR: {ex.Message}"); }
        }

        sb.AppendLine();
        sb.AppendLine("=== LISTVIEW DEL ESCRITORIO ===");
        var hLV = FindDesktopListView();
        if (hLV == IntPtr.Zero)
        {
            sb.AppendLine("  ERROR: SysListView32 no encontrado");
        }
        else
        {
            GetWindowThreadProcessId(hLV, out uint pid);
            sb.AppendLine($"Explorer PID: {pid}");

            var hProc = OpenProcess(PROCESS_VM_READ | PROCESS_VM_WRITE | PROCESS_VM_OP, false, pid);
            if (hProc == IntPtr.Zero)
            {
                sb.AppendLine("  ERROR: OpenProcess falló");
            }
            else
            {
                try
                {
                    int count = (int)SendMessage(hLV, LVM_GETITEMCOUNT, IntPtr.Zero, IntPtr.Zero);
                    sb.AppendLine($"Items: {count}");
                    sb.AppendLine();

                    int lvSz = Marshal.SizeOf<LVITEMW>();
                    const int TxtSz = 520;
                    var remLv = VirtualAllocEx(hProc, IntPtr.Zero, (uint)(lvSz + TxtSz), MEM_COMMIT, PAGE_READWRITE);
                    if (remLv != IntPtr.Zero)
                    {
                        var remTxt  = IntPtr.Add(remLv, lvSz);
                        var zeroBuf = new byte[TxtSz];
                        byte[] tb   = new byte[TxtSz];
                        try
                        {
                            for (int i = 0; i < count; i++)
                            {
                                WriteProcessMemory(hProc, remTxt, zeroBuf, TxtSz, out _);
                                var lv = new LVITEMW { mask = LVIF_TEXT, iItem = i, pszText = remTxt, cchTextMax = 260 };
                                WriteRemote(hProc, remLv, lv);
                                SendMessage(hLV, LVM_GETITEMTEXTW, (IntPtr)i, remLv);
                                ReadProcessMemory(hProc, remTxt, tb, TxtSz, out _);
                                string raw  = Encoding.Unicode.GetString(tb);
                                int    nul  = raw.IndexOf('\0');
                                string name = (nul >= 0 ? raw[..nul] : raw).Trim();
                                var zone = ClassifyIconDirect(name, paths);
                                sb.AppendLine($"  [{i,2}] \"{name}\" → {zone}");
                            }
                        }
                        finally { VirtualFreeEx(hProc, remLv, 0, MEM_RELEASE); }
                    }
                }
                finally { CloseHandle(hProc); }
            }
        }

        var filePath = Path.Combine(Path.GetTempPath(), "diagnostico_escritorio.txt");
        File.WriteAllText(filePath, sb.ToString(), Encoding.UTF8);
        return filePath;
    }

    // ── marshal helpers ────────────────────────────────────────────────────────

    private static void WriteRemote<T>(IntPtr hProc, IntPtr addr, T value) where T : struct
    {
        int size = Marshal.SizeOf<T>();
        byte[] bytes = new byte[size];
        var ptr = Marshal.AllocHGlobal(size);
        try { Marshal.StructureToPtr(value, ptr, false); Marshal.Copy(ptr, bytes, 0, size); }
        finally { Marshal.FreeHGlobal(ptr); }
        WriteProcessMemory(hProc, addr, bytes, (uint)bytes.Length, out _);
    }

    private static T FromBytes<T>(byte[] bytes) where T : struct
    {
        var ptr = Marshal.AllocHGlobal(bytes.Length);
        try { Marshal.Copy(bytes, 0, ptr, bytes.Length); return Marshal.PtrToStructure<T>(ptr); }
        finally { Marshal.FreeHGlobal(ptr); }
    }
}
