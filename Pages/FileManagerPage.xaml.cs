using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using OrganizadorDescargas.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace OrganizadorDescargas.Pages;

public sealed partial class FileManagerPage : Page
{
    private readonly List<string>    _history    = new();
    private int                      _historyPos = -1;
    private string?                  _currentPath;
    private CancellationTokenSource? _loadCts;
    private FileSystemWatcher?       _watcher;
    private Timer?                   _debounce;

    private List<FileSystemItem> _allItems    = new();
    private List<FileSystemItem> _currentView = new();
    private bool                 _sortAscending = true;
    private string               _sortKey       = "Name";
    private readonly object      _debounceLock  = new();
    private bool                 _pageReady     = false;

    private const int MaxFiles = 50_000;

    public FileManagerPage()
    {
        InitializeComponent();
        Loaded += async (_, _) =>
        {
            _pageReady = true;
            await LoadDrivesAsync();
            var downloads = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads");
            NavigateTo(Directory.Exists(downloads) ? downloads : Environment.GetFolderPath(Environment.SpecialFolder.UserProfile));
        };
        Unloaded += (_, _) => Cleanup();
    }

    private void Cleanup()
    {
        _loadCts?.Cancel();
        lock (_debounceLock)
        {
            _debounce?.Dispose();
            _debounce = null;
        }
        var w = _watcher;
        _watcher = null;
        try { w?.Dispose(); } catch { }
    }

    // ── TreeView ──────────────────────────────────────────────────────────────

    private async Task LoadDrivesAsync()
    {
        var drives = await Task.Run(() =>
        {
            var result = new List<(string Path, string Label)>();
            foreach (var d in DriveInfo.GetDrives())
            {
                try
                {
                    if (!d.IsReady) continue;
                    var label = string.IsNullOrEmpty(d.VolumeLabel)
                        ? d.Name.TrimEnd('\\')
                        : $"{d.VolumeLabel} ({d.Name.TrimEnd('\\')})";
                    result.Add((d.RootDirectory.FullName, label));
                }
                catch { }
            }
            return result;
        });

        FolderTree.RootNodes.Clear();
        foreach (var (path, label) in drives)
        {
            FolderTree.RootNodes.Add(new TreeViewNode
            {
                Content               = new FolderNode(path, label),
                HasUnrealizedChildren = true,
            });
        }
    }

    private void FolderTree_Expanding(TreeView sender, TreeViewExpandingEventArgs args)
    {
        var node = args.Node;
        if (node.Content is not FolderNode fn || !node.HasUnrealizedChildren) return;

        node.HasUnrealizedChildren = false;
        node.Children.Clear();

        try
        {
            foreach (var dir in Directory.GetDirectories(fn.Path)
                .OrderBy(d => Path.GetFileName(d), StringComparer.OrdinalIgnoreCase))
            {
                try
                {
                    var di = new DirectoryInfo(dir);
                    node.Children.Add(new TreeViewNode
                    {
                        Content               = new FolderNode(dir, di.Name),
                        HasUnrealizedChildren = HasSubfolders(dir),
                    });
                }
                catch { }
            }
        }
        catch { }
    }

    private void FolderTree_ItemInvoked(TreeView sender, TreeViewItemInvokedEventArgs args)
    {
        try
        {
            if (args.InvokedItem is TreeViewNode node && node.Content is FolderNode fn
                && fn.Path != _currentPath)
                NavigateTo(fn.Path);
        }
        catch { }
    }

    // ── Navegación ────────────────────────────────────────────────────────────

    private void NavigateTo(string path, bool addToHistory = true)
    {
        try
        {
            if (!Directory.Exists(path)) return;

            if (addToHistory && (_historyPos < 0 || _history[_historyPos] != path))
            {
                if (_historyPos < _history.Count - 1)
                    _history.RemoveRange(_historyPos + 1, _history.Count - _historyPos - 1);
                _history.Add(path);
                _historyPos = _history.Count - 1;
            }

            _currentPath    = path;
            AddressBar.Text = path;
            UpdateNavButtons();
            StartWatcher(path);
            _ = LoadDirectoryAsync(path);
        }
        catch { }
    }

    private void UpdateNavButtons()
    {
        BackBtn.IsEnabled    = _historyPos > 0;
        ForwardBtn.IsEnabled = _historyPos < _history.Count - 1;
        UpBtn.IsEnabled      = _currentPath != null
                               && Path.GetPathRoot(_currentPath) != _currentPath;
    }

    private void BackBtn_Click(object sender, RoutedEventArgs e)
    {
        if (_historyPos <= 0) return;
        _historyPos--;
        NavigateTo(_history[_historyPos], addToHistory: false);
    }

    private void ForwardBtn_Click(object sender, RoutedEventArgs e)
    {
        if (_historyPos >= _history.Count - 1) return;
        _historyPos++;
        NavigateTo(_history[_historyPos], addToHistory: false);
    }

    private void UpBtn_Click(object sender, RoutedEventArgs e)
    {
        if (_currentPath == null) return;
        var parent = Path.GetDirectoryName(_currentPath);
        if (parent != null) NavigateTo(parent);
    }

    private void AddressBar_KeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (e.Key == Windows.System.VirtualKey.Enter)
            NavigateTo(AddressBar.Text.Trim());
    }

    // ── FileSystemWatcher — detecta cambios en tiempo real ────────────────────

    private void StartWatcher(string path)
    {
        var old = _watcher;
        _watcher = null;
        old?.Dispose();
        _debounce?.Dispose();
        _debounce = null;

        try
        {
            var w = new FileSystemWatcher(path)
            {
                NotifyFilter           = NotifyFilters.FileName | NotifyFilters.DirectoryName | NotifyFilters.Size,
                IncludeSubdirectories  = false,
                EnableRaisingEvents    = true,
            };
            w.Created += OnFileSystemChanged;
            w.Deleted += OnFileSystemChanged;
            w.Renamed += OnFileSystemChanged;
            _watcher = w;
        }
        catch { }
    }

    private void OnFileSystemChanged(object sender, FileSystemEventArgs e)
    {
        lock (_debounceLock)
        {
            _debounce?.Dispose();
            _debounce = new Timer(_ =>
            {
                if (_watcher == null) return;
                DispatcherQueue.TryEnqueue(() =>
                {
                    if (_currentPath != null && _watcher != null)
                        _ = LoadDirectoryAsync(_currentPath);
                });
            }, null, 800, Timeout.Infinite);
        }
    }

    // ── Carga de directorio ───────────────────────────────────────────────────

    private async Task LoadDirectoryAsync(string path)
    {
        _loadCts?.Cancel();
        _loadCts = new CancellationTokenSource();
        var ct = _loadCts.Token;

        EmptyLabel.Visibility   = Visibility.Collapsed;
        FilesList.Visibility    = Visibility.Collapsed;
        StatsBar.Visibility     = Visibility.Collapsed;
        var folderName = Path.GetFileName(path);
        LoadingLabel.Text = $"Cargando {(string.IsNullOrEmpty(folderName) ? path : folderName)}…";
        LoadingPanel.Visibility = Visibility.Visible;

        List<FileSystemItem>? items = null;
        Exception? error = null;
        try
        {
            bool recursive = RecursiveCheck.IsChecked == true;
            items = await Task.Run(() => ScanDirectory(path, recursive, ct), ct);
        }
        catch (OperationCanceledException) { return; }
        catch (Exception ex) { error = ex; }

        LoadingPanel.Visibility = Visibility.Collapsed;

        if (error != null)
        {
            EmptyLabel.Text       = $"Error al cargar: {error.Message}";
            EmptyLabel.Visibility = Visibility.Visible;
            return;
        }

        _allItems = items ?? new();
        RefreshView();
    }

    private static List<FileSystemItem> ScanDirectory(string path, bool recursive, CancellationToken ct)
    {
        var items     = new List<FileSystemItem>();
        var searchOpt = recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;

        if (!recursive)
        {
            try
            {
                foreach (var dir in Directory.GetDirectories(path)
                    .OrderBy(d => Path.GetFileName(d), StringComparer.OrdinalIgnoreCase))
                {
                    ct.ThrowIfCancellationRequested();
                    try
                    {
                        var di = new DirectoryInfo(dir);
                        items.Add(new FileSystemItem
                        {
                            FullPath    = dir,
                            Name        = di.Name,
                            IsDirectory = true,
                            Modified    = di.LastWriteTime,
                            Created     = di.CreationTime,
                        });
                    }
                    catch { }
                }
            }
            catch (OperationCanceledException) { throw; }
            catch { }
        }

        int fileCount = 0;
        try
        {
            foreach (var file in Directory.EnumerateFiles(path, "*", searchOpt))
            {
                ct.ThrowIfCancellationRequested();
                if (++fileCount > MaxFiles) break;
                try
                {
                    var fi = new FileInfo(file);
                    items.Add(new FileSystemItem
                    {
                        FullPath    = file,
                        Name        = recursive ? Path.GetRelativePath(path, file) : fi.Name,
                        IsDirectory = false,
                        Extension   = fi.Extension.ToLowerInvariant(),
                        SizeBytes   = fi.Length,
                        Modified    = fi.LastWriteTime,
                        Created     = fi.CreationTime,
                        OpenCount   = MainWindow.UsageTracker.GetCount(file),
                    });
                }
                catch { }
            }
        }
        catch (OperationCanceledException) { throw; }
        catch { }

        return items;
    }

    // ── Filtro & Orden ────────────────────────────────────────────────────────

    private void RecursiveCheck_Changed(object sender, RoutedEventArgs e)
    {
        if (!_pageReady) return;
        if (_currentPath != null) _ = LoadDirectoryAsync(_currentPath);
    }

    private void FilterBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (!_pageReady) return;
        RefreshView();
    }

    private void SortCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!_pageReady) return;
        _sortKey = (SortCombo.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "Name";
        RefreshView();
    }

    private void SortDirBtn_Click(object sender, RoutedEventArgs e)
    {
        if (!_pageReady) return;
        _sortAscending     = !_sortAscending;
        SortDirBtn.Content = _sortAscending ? "↑ Asc" : "↓ Desc";
        RefreshView();
    }

    private void RefreshView()
    {
        if (!_pageReady) return;
        if (_currentPath == null)
        {
            LoadingPanel.Visibility = Visibility.Collapsed;
            EmptyLabel.Text       = "Selecciona una carpeta en el árbol de la izquierda";
            EmptyLabel.Visibility = Visibility.Visible;
            FilesList.Visibility  = Visibility.Collapsed;
            StatsBar.Visibility   = Visibility.Collapsed;
            return;
        }

        var filter  = FilterBox?.Text?.Trim() ?? "";
        var visible = string.IsNullOrEmpty(filter)
            ? _allItems
            : _allItems.Where(f =>
                f.Name.Contains(filter, StringComparison.OrdinalIgnoreCase) ||
                f.TypeDisplay.Contains(filter, StringComparison.OrdinalIgnoreCase))
              .ToList();

        IEnumerable<FileSystemItem> sorted = _sortKey switch
        {
            "Type"     => _sortAscending
                ? visible.OrderBy(f => f.IsDirectory ? 0 : 1).ThenBy(f => f.TypeDisplay).ThenBy(f => f.Name, StringComparer.OrdinalIgnoreCase)
                : visible.OrderBy(f => f.IsDirectory ? 0 : 1).ThenByDescending(f => f.TypeDisplay).ThenBy(f => f.Name, StringComparer.OrdinalIgnoreCase),
            "Size"     => _sortAscending
                ? visible.OrderBy(f => f.IsDirectory ? 0 : 1).ThenBy(f => f.SizeBytes)
                : visible.OrderBy(f => f.IsDirectory ? 0 : 1).ThenByDescending(f => f.SizeBytes),
            "Modified" => _sortAscending
                ? visible.OrderBy(f => f.IsDirectory ? 0 : 1).ThenBy(f => f.Modified)
                : visible.OrderBy(f => f.IsDirectory ? 0 : 1).ThenByDescending(f => f.Modified),
            "Opens"    => _sortAscending
                ? visible.OrderBy(f => f.IsDirectory ? 0 : 1).ThenBy(f => f.OpenCount).ThenBy(f => f.Name, StringComparer.OrdinalIgnoreCase)
                : visible.OrderBy(f => f.IsDirectory ? 0 : 1).ThenByDescending(f => f.OpenCount).ThenBy(f => f.Name, StringComparer.OrdinalIgnoreCase),
            _          => _sortAscending
                ? visible.OrderBy(f => f.IsDirectory ? 0 : 1).ThenBy(f => f.Name, StringComparer.OrdinalIgnoreCase)
                : visible.OrderBy(f => f.IsDirectory ? 0 : 1).ThenByDescending(f => f.Name, StringComparer.OrdinalIgnoreCase),
        };

        _currentView          = sorted.ToList();
        FilesList.ItemsSource = _currentView;

        var files = visible.Where(f => !f.IsDirectory).ToList();
        var dirs  = visible.Where(f =>  f.IsDirectory).ToList();

        FileCountLabel.Text   = files.Count.ToString();
        FolderCountLabel.Text = dirs.Count.ToString();
        SizeLabel.Text        = FormatSize(files.Sum(f => f.SizeBytes));
        OpensLabel.Text       = files.Sum(f => f.OpenCount).ToString();

        StatsBar.Visibility   = Visibility.Visible;
        FilesList.Visibility  = _currentView.Count > 0 ? Visibility.Visible : Visibility.Collapsed;
        EmptyLabel.Text       = string.IsNullOrEmpty(filter)
            ? "Esta carpeta está vacía"
            : "No hay elementos que coincidan con el filtro";
        EmptyLabel.Visibility = _currentView.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
    }

    // ── Doble clic ────────────────────────────────────────────────────────────

    private void FilesList_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
    {
        try
        {
            if (FilesList.SelectedItem is not FileSystemItem item) return;

            if (item.IsDirectory)
            {
                NavigateTo(item.FullPath);
            }
            else
            {
                Process.Start(new ProcessStartInfo(item.FullPath) { UseShellExecute = true });
                MainWindow.UsageTracker.RecordOpen(item.FullPath);
                item.OpenCount++;
                FilesList.ItemsSource = null;
                FilesList.ItemsSource = _currentView;
            }
        }
        catch { }
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static bool HasSubfolders(string path)
    {
        try { return Directory.EnumerateDirectories(path).Any(); }
        catch { return false; }
    }

    private static string FormatSize(long bytes) => bytes switch
    {
        >= 1_073_741_824 => $"{bytes / 1_073_741_824.0:F1} GB",
        >= 1_048_576     => $"{bytes / 1_048_576.0:F1} MB",
        >= 1_024         => $"{bytes / 1_024.0:F0} KB",
        _                => $"{bytes} B",
    };
}
