using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Automation;

namespace CodexTokenQuest.Desktop;

internal static class NativeMethods
{
    private const int GwlHwndParent = -8;
    private const int MinimumHostWidth = 640;
    private const int MinimumHostHeight = 480;
    private static AutomationElement? _modeButton;
    private static nint _modeButtonHost;

    internal delegate bool EnumWindowsProc(nint window, nint parameter);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool EnumWindows(EnumWindowsProc callback, nint parameter);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool IsWindowVisible(nint window);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool IsIconic(nint window);

    [DllImport("user32.dll")]
    internal static extern nint GetForegroundWindow();

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool GetWindowRect(nint window, out WindowRect rectangle);

    [DllImport("user32.dll")]
    internal static extern uint GetWindowThreadProcessId(nint window, out uint processId);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    internal static extern int GetWindowText(nint window, StringBuilder text, int maxCount);

    [DllImport("user32.dll", EntryPoint = "SetWindowLongPtrW", SetLastError = true)]
    private static extern nint SetWindowLongPtr(nint window, int index, nint value);

    internal static void SetOwnerWindow(nint window, nint owner) =>
        SetWindowLongPtr(window, GwlHwndParent, owner);

    internal static nint FindCodexWindow(nint preferredWindow = 0)
    {
        var candidates = new Dictionary<nint, long>();
        var foregroundWindow = GetForegroundWindow();
        EnumWindows((window, _) =>
        {
            if (!IsWindowVisible(window) || IsIconic(window))
            {
                return true;
            }

            GetWindowThreadProcessId(window, out var processId);
            if (processId == Environment.ProcessId)
            {
                return true;
            }

            try
            {
                using var process = Process.GetProcessById((int)processId);
                var processName = process.ProcessName;
                if (!GetWindowRect(window, out var hostBounds))
                {
                    return true;
                }
                var width = Math.Max(0, hostBounds.Right - hostBounds.Left);
                var height = Math.Max(0, hostBounds.Bottom - hostBounds.Top);
                if (width < MinimumHostWidth || height < MinimumHostHeight)
                {
                    return true;
                }
                var executablePath = string.Empty;
                try
                {
                    executablePath = process.MainModule?.FileName ?? string.Empty;
                }
                catch
                {
                    // Process metadata can be unavailable across privilege boundaries.
                }
                var title = new StringBuilder(256);
                GetWindowText(window, title, title.Capacity);

                var isCodexProcess = processName.Equals("Codex", StringComparison.OrdinalIgnoreCase) ||
                                     processName.StartsWith("Codex.", StringComparison.OrdinalIgnoreCase);
                var isPackagedCodex = processName.Equals("ChatGPT", StringComparison.OrdinalIgnoreCase) &&
                                      executablePath.Contains("OpenAI.Codex_", StringComparison.OrdinalIgnoreCase);
                var isCodexDesktopHost = processName.Equals("ChatGPT", StringComparison.OrdinalIgnoreCase) &&
                                         title.ToString().Equals("ChatGPT", StringComparison.OrdinalIgnoreCase);
                var isCodexWindow = title.ToString().Contains("Codex", StringComparison.OrdinalIgnoreCase) &&
                                    processName.Contains("Codex", StringComparison.OrdinalIgnoreCase);
                var isCodexHost = isCodexProcess || isPackagedCodex || isCodexDesktopHost || isCodexWindow;
                // The packaged Windows host now exposes a generic ChatGPT mode label even while
                // displaying Codex. Its package identity is the stable signal for this companion.
                var requiresModeCheck = processName.Equals("ChatGPT", StringComparison.OrdinalIgnoreCase) &&
                                        !isPackagedCodex &&
                                        !isCodexDesktopHost;
                var modeAllowsHud = !requiresModeCheck ||
                                    !TryGetCodexDesktopMode(window, out var isCodexMode) ||
                                    isCodexMode;
                if (isCodexHost && modeAllowsHud)
                {
                    candidates[window] = (long)width * height;
                }
            }
            catch (ArgumentException)
            {
                // The process closed while windows were being enumerated.
            }

            return true;
        }, 0);

        if (candidates.ContainsKey(foregroundWindow))
        {
            return foregroundWindow;
        }

        if (preferredWindow != 0 && candidates.ContainsKey(preferredWindow))
        {
            return preferredWindow;
        }

        return candidates.Count == 0
            ? 0
            : candidates.MaxBy(candidate => candidate.Value).Key;
    }

    private static bool TryGetCodexDesktopMode(nint hostWindow, out bool isCodexMode)
    {
        isCodexMode = false;
        try
        {
            if (_modeButtonHost == hostWindow && _modeButton is not null && TryReadMode(_modeButton, out var cachedMode))
            {
                isCodexMode = cachedMode;
                return true;
            }

            _modeButton = null;
            _modeButtonHost = 0;
            var root = AutomationElement.FromHandle(hostWindow);
            if (root is null)
            {
                return false;
            }

            var buttons = root.FindAll(
                TreeScope.Descendants,
                new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.Button));
            for (var index = 0; index < buttons.Count; index++)
            {
                var button = buttons[index];
                if (!TryIdentifyModeButton(button, root, out var detectedMode))
                {
                    continue;
                }

                _modeButton = button;
                _modeButtonHost = hostWindow;
                isCodexMode = detectedMode;
                return true;
            }
        }
        catch (ElementNotAvailableException)
        {
            _modeButton = null;
            _modeButtonHost = 0;
        }
        catch (InvalidOperationException)
        {
            _modeButton = null;
            _modeButtonHost = 0;
        }
        catch (COMException)
        {
            _modeButton = null;
            _modeButtonHost = 0;
        }

        return false;
    }

    private static bool TryReadMode(AutomationElement button, out bool isCodexMode)
    {
        var name = button.Current.Name;
        if (IsModeLabel(name))
        {
            isCodexMode = name.Contains("Codex", StringComparison.OrdinalIgnoreCase);
            return true;
        }

        isCodexMode = false;
        return false;
    }

    private static bool TryIdentifyModeButton(AutomationElement button, AutomationElement root, out bool isCodexMode)
    {
        var current = button.Current;
        var name = current.Name;
        if (IsModeLabel(name))
        {
            isCodexMode = name.Contains("Codex", StringComparison.OrdinalIgnoreCase);
            return true;
        }

        var buttonBounds = current.BoundingRectangle;
        var rootBounds = root.Current.BoundingRectangle;
        var isTopLeftModeControl = buttonBounds.Left >= rootBounds.Left &&
                                   buttonBounds.Left - rootBounds.Left < 180 &&
                                   buttonBounds.Top >= rootBounds.Top &&
                                   buttonBounds.Top - rootBounds.Top < 110;
        if (isTopLeftModeControl &&
            (name.Contains("Codex", StringComparison.OrdinalIgnoreCase) ||
             name.Contains("ChatGPT", StringComparison.OrdinalIgnoreCase)))
        {
            isCodexMode = name.Contains("Codex", StringComparison.OrdinalIgnoreCase);
            return true;
        }

        isCodexMode = false;
        return false;
    }

    private static bool IsModeLabel(string name) =>
        name.Contains("目前模式", StringComparison.OrdinalIgnoreCase) ||
        name.Contains("current mode", StringComparison.OrdinalIgnoreCase) ||
        name.Contains("mode:", StringComparison.OrdinalIgnoreCase);

    internal static bool IsCodexRunning()
    {
        foreach (var process in Process.GetProcesses())
        {
            using (process)
            {
                try
                {
                    var processName = process.ProcessName;
                    if (processName.Equals("Codex", StringComparison.OrdinalIgnoreCase) ||
                        processName.StartsWith("Codex.", StringComparison.OrdinalIgnoreCase))
                    {
                        return true;
                    }

                    if (!processName.Equals("ChatGPT", StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    var executablePath = string.Empty;
                    try
                    {
                        executablePath = process.MainModule?.FileName ?? string.Empty;
                    }
                    catch
                    {
                        // Fall back to the window title across privilege boundaries.
                    }

                    if (executablePath.Contains("OpenAI.Codex_", StringComparison.OrdinalIgnoreCase) ||
                        process.MainWindowTitle.Equals("ChatGPT", StringComparison.OrdinalIgnoreCase))
                    {
                        return true;
                    }
                }
                catch (InvalidOperationException)
                {
                    // The process exited while it was being inspected.
                }
            }
        }

        return false;
    }
}

[StructLayout(LayoutKind.Sequential)]
internal struct WindowRect
{
    internal int Left;
    internal int Top;
    internal int Right;
    internal int Bottom;
}
