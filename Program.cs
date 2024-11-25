using H.NotifyIcon.Core;
using Microsoft.Win32;
using System.Diagnostics;
using System.Drawing;

Process[] existingProcesses = Process.GetProcessesByName(Process.GetCurrentProcess().ProcessName);
if (existingProcesses.Length > 1)
{
    Environment.Exit(0);
}

using Process WSLSleep = new Process();
WSLSleep.StartInfo.FileName = "wsl.exe";
WSLSleep.StartInfo.Arguments = "sleep infinity";
WSLSleep.StartInfo.CreateNoWindow = true;
WSLSleep.StartInfo.UseShellExecute = false;
WSLSleep.Start();

var executableFileName = Process.GetCurrentProcess().MainModule?.FileName;
if(executableFileName is null)
{
    Environment.Exit(1);
}
using var icon = Icon.ExtractAssociatedIcon(executableFileName);
if (icon is null)
{
    Environment.Exit(1);
}
using var trayIcon = new TrayIconWithContextMenu
{
    Icon = icon.Handle,
    ToolTip = "WSL Keep Alive"
};

var AutoStartMenuItem = new PopupMenuItem("Start on boot", (sender, _) =>
{
    if(sender is not PopupMenuItem) return;
    var menuItem = (PopupMenuItem)sender;
    menuItem.Checked = ToggleAutoStart();
});

trayIcon.ContextMenu = new PopupMenu
{
    Items = {
        AutoStartMenuItem,
        new PopupMenuItem("Exit", (_,_) =>
        {
            WSLSleep.Kill();
            WSLSleep.Dispose();
            trayIcon.Dispose();
            Environment.Exit(0);
        })
    }
};

AutoStartMenuItem.Checked = GetAutoStart();

trayIcon.Create();

WSLSleep.WaitForExit();

bool GetAutoStart()
{
    using var key = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", true);
    return key?.GetValue(Process.GetCurrentProcess().ProcessName) != null;
}

bool ToggleAutoStart()
{
    using var key = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", true);
    if(key is null) return false;
    if (GetAutoStart())
    {
        key.DeleteValue(Process.GetCurrentProcess().ProcessName);
        return false;
    }
    else
    {
        key.SetValue(Process.GetCurrentProcess().ProcessName, $"\"{executableFileName}\"");
        return true;
    }
}

