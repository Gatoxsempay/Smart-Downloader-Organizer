using Microsoft.Win32;
using System;
using System.Reflection;

namespace OrganizadorDescargas.Services;

public static class AutostartService
{
    private const string RegPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string AppName = "OrganizadorDescargas";

    public static bool IsEnabled()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(RegPath);
            return key?.GetValue(AppName) != null;
        }
        catch { return false; }
    }

    public static void SetEnabled(bool enable)
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(RegPath, writable: true);
            if (key == null) return;
            if (enable)
            {
                var exe = Environment.ProcessPath ?? Assembly.GetExecutingAssembly().Location;
                key.SetValue(AppName, $"\"{exe}\"");
            }
            else
            {
                key.DeleteValue(AppName, throwOnMissingValue: false);
            }
        }
        catch { }
    }
}
