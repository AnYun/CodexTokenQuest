using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using Avalonia.Controls;

namespace CodexTokenQuest.Desktop;

internal sealed class WindowsDesktopAdapter : IHostWindowTracker, IHudWindowIntegration
{
    private nint _preferred;
    public HostState Read()
    {
        var processes = new HashSet<int>();
        foreach (var process in Process.GetProcesses())
        {
            using (process)
            {
                try
                {
                    var name = process.ProcessName;
                    if (((name.Equals("Codex", StringComparison.OrdinalIgnoreCase) || name.StartsWith("Codex.", StringComparison.OrdinalIgnoreCase)) && process.MainWindowHandle != 0)
                        || (name.Equals("ChatGPT", StringComparison.OrdinalIgnoreCase)
                            && (process.MainModule?.FileName?.Contains("OpenAI.Codex_", StringComparison.OrdinalIgnoreCase) == true
                                || process.MainWindowTitle.Contains("Codex", StringComparison.OrdinalIgnoreCase))))
                        processes.Add(process.Id);
                }
                catch (Exception e) when (e is InvalidOperationException or System.ComponentModel.Win32Exception or NotSupportedException) { }
            }
        }
        var foreground = GetForegroundWindow();
        GetWindowThreadProcessId(foreground, out var activePid);
        var candidates = new List<(nint Handle, HostBounds Bounds)>();
        EnumWindows((window, _) =>
        {
            GetWindowThreadProcessId(window, out var pid);
            if (!processes.Contains((int)pid) || !IsWindowVisible(window) || IsIconic(window)) return true;
            if (DwmGetWindowAttribute(window, 14, out int cloaked, sizeof(int)) == 0 && cloaked != 0) return true;
            if (DwmGetWindowBounds(window, 9, out var rect, Marshal.SizeOf<NativeRect>()) != 0 && !GetWindowRect(window, out rect)) return true;
            var width = rect.Right - rect.Left; var height = rect.Bottom - rect.Top;
            var scale = Math.Max(96, GetDpiForWindow(window)) / 96d;
            var className = new StringBuilder(256); GetClassName(window, className, className.Capacity);
            var bounds = new HostBounds(rect.Left, rect.Top, width, height);
            if (IsMainWindow(bounds, scale, className.ToString(), GetWindowLongPtr(window, -20).ToInt64(), GetWindow(window, 4)))
                candidates.Add((window, bounds));
            return true;
        }, 0);
        var selected = candidates.OrderBy(x => x.Handle == foreground ? 0 : x.Handle == _preferred ? 1 : 2)
            .ThenByDescending(x => x.Bounds.Width * x.Bounds.Height).FirstOrDefault();
        if (selected.Handle == 0) return new(processes.Count > 0, true, false);
        _preferred = selected.Handle;
        var scale = Math.Max(96, GetDpiForWindow(selected.Handle)) / 96d;
        return new(true, true, foreground == selected.Handle || activePid == Environment.ProcessId,
            selected.Bounds, selected.Handle, scale);
    }
    // Menus can belong to the Codex process and become foreground windows. They
    // must never replace the remembered main window, including at high DPI.
    internal static bool IsMainWindow(HostBounds bounds, double scale, string className, long extendedStyle, nint owner) =>
        bounds.Width / scale >= 640 && bounds.Height / scale >= 480
        && owner == 0 && (extendedStyle & 0x80) == 0
        && className != "#32768" && !className.Equals("tooltips_class32", StringComparison.OrdinalIgnoreCase);

    public void Configure(Window window)
    {
        var handle = window.TryGetPlatformHandle()?.Handle ?? 0;
        if (handle != 0) SetWindowLongPtr(handle, -20, GetWindowLongPtr(handle, -20) | 0x80 | 0x08000000);
    }
    public void Attach(Window window, HostState host)
    {
        var handle = window.TryGetPlatformHandle()?.Handle ?? 0;
        if (handle != 0) SetWindowLongPtr(handle, -8, host.Window);
    }
    public void RequestPermission() { }
    public void Dispose() { }
    private delegate bool EnumWindowsCallback(nint hwnd, nint data);
    [StructLayout(LayoutKind.Sequential)] private struct NativeRect { public int Left, Top, Right, Bottom; }
    [DllImport("user32.dll")] private static extern bool EnumWindows(EnumWindowsCallback callback, nint data);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)] private static extern int GetClassName(nint window, StringBuilder text, int count);
    [DllImport("user32.dll")] private static extern nint GetWindow(nint window, uint command);
    [DllImport("user32.dll")] private static extern bool IsWindowVisible(nint hwnd);
    [DllImport("user32.dll")] private static extern bool IsIconic(nint hwnd);
    [DllImport("user32.dll")] private static extern nint GetForegroundWindow();
    [DllImport("user32.dll")] private static extern uint GetWindowThreadProcessId(nint hwnd, out uint pid);
    [DllImport("user32.dll")] private static extern uint GetDpiForWindow(nint hwnd);
    [DllImport("user32.dll")] private static extern bool GetWindowRect(nint hwnd, out NativeRect rect);
    [DllImport("dwmapi.dll")] private static extern int DwmGetWindowAttribute(nint hwnd, int attribute, out int value, int size);
    [DllImport("dwmapi.dll", EntryPoint = "DwmGetWindowAttribute")] private static extern int DwmGetWindowBounds(nint hwnd, int attribute, out NativeRect value, int size);
    [DllImport("user32.dll", EntryPoint = "SetWindowLongPtrW")] private static extern nint SetWindowLongPtr(nint hwnd, int index, nint value);
    [DllImport("user32.dll", EntryPoint = "GetWindowLongPtrW")] private static extern nint GetWindowLongPtr(nint hwnd, int index);
}
