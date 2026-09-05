using System.Runtime.InteropServices;
using System.Diagnostics;
using Avalonia.Controls;

namespace CodexTokenQuest.Desktop;

// Only public AppKit, Accessibility and CoreGraphics APIs. No UI scripting, screen
// capture, private AX window-number API, or process-name assumption is needed.
internal sealed class MacDesktopAdapter : IHostWindowTracker, IHudWindowIntegration
{
    private const string AppKit = "/System/Library/Frameworks/AppKit.framework/AppKit";
    private const string AX = "/System/Library/Frameworks/ApplicationServices.framework/ApplicationServices";
    private const string CF = "/System/Library/Frameworks/CoreFoundation.framework/CoreFoundation";
    private const string CG = "/System/Library/Frameworks/CoreGraphics.framework/CoreGraphics";
    private const string ObjC = "/usr/lib/libobjc.A.dylib";
    private nint _application;
    private int _pid;
    private DateTimeOffset _lastError;

    public HostState Read()
    {
        var pool = Send(Send(objc_getClass("NSAutoreleasePool"), Sel("alloc")), Sel("init"));
        try
        {
            var apps = SendPtr(objc_getClass("NSRunningApplication"), Sel("runningApplicationsWithBundleIdentifier:"), String("com.openai.codex"));
            var count = (long)Send(apps, Sel("count"));
            if (count == 0) { ClearApplication(); return new(false, AXIsProcessTrusted(), false); }
            var app = SendPtr(apps, Sel("objectAtIndex:"), 0);
            var pid = (int)Send(app, Sel("processIdentifier"));
            var workspace = Send(objc_getClass("NSWorkspace"), Sel("sharedWorkspace"));
            var active = Send(workspace, Sel("frontmostApplication"));
            var activePid = (int)Send(active, Sel("processIdentifier"));
            var foreground = activePid == pid || activePid == Environment.ProcessId;
            var permission = AXIsProcessTrusted();
            if (!permission || Send(app, Sel("isHidden")) != 0)
                return new(true, permission, false);
            if (_pid != pid)
            {
                ClearApplication(); _pid = pid; _application = AXUIElementCreateApplication(pid);
                AXUIElementSetMessagingTimeout(_application, 0.2f);
            }
            var windows = Copy(_application, "AXWindows");
            var focused = Copy(_application, "AXFocusedWindow");
            try
            {
                var choices = new List<(nint Window, HostBounds Bounds, bool Focused)>();
                var length = windows == 0 ? 0 : CFArrayGetCount(windows);
                for (nint i = 0; i < length; i++)
                {
                    var window = CFArrayGetValueAtIndex(windows, i);
                    if (ReadBool(window, "AXMinimized")) continue;
                    var pos = Copy(window, "AXPosition"); var size = Copy(window, "AXSize");
                    try
                    {
                        if (pos == 0 || size == 0 || !AXValueGetValue(pos, 1, out Pair point) || !AXValueGetValue(size, 2, out Pair dimensions)) continue;
                        if (dimensions.X < 280 || dimensions.Y < 160) continue;
                        choices.Add((window, new(point.X, point.Y, dimensions.X, dimensions.Y), focused != 0 && CFEqual(window, focused)));
                    }
                    finally { Release(pos); Release(size); }
                }
                // On-screen metadata is available without requesting Screen Recording.
                // It excludes windows on other Spaces; window titles/pixels are never read.
                var onscreen = OnScreenBounds(pid);
                var selected = choices.Where(c => onscreen.Any(r => Math.Abs(r.X - c.Bounds.X) < 3 && Math.Abs(r.Y - c.Bounds.Y) < 3
                        && Math.Abs(r.Width - c.Bounds.Width) < 3 && Math.Abs(r.Height - c.Bounds.Height) < 3))
                    .OrderBy(c => c.Focused ? 0 : 1).ThenByDescending(c => c.Bounds.Width * c.Bounds.Height).FirstOrDefault();
                return selected.Window == 0 ? new(true, true, false) : new(true, true, foreground, selected.Bounds);
            }
            finally { Release(windows); Release(focused); }
        }
        catch (Exception e)
        {
            if (DateTimeOffset.Now - _lastError > TimeSpan.FromMinutes(1)) { AppPaths.Log($"Mac window tracking: {e.Message}"); _lastError = DateTimeOffset.Now; }
            return new(true, AXIsProcessTrusted(), false, Reliable: false);
        }
        finally { Send(pool, Sel("drain")); }
    }

    private static List<HostBounds> OnScreenBounds(int pid)
    {
        var result = new List<HostBounds>();
        var list = CGWindowListCopyWindowInfo(1 | 16, 0); // on-screen, excluding desktop elements
        if (list == 0) return result;
        try
        {
            for (nint i = 0; i < CFArrayGetCount(list); i++)
            {
                var entry = CFArrayGetValueAtIndex(list, i);
                var owner = CFDictionaryGetValue(entry, String("kCGWindowOwnerPID"));
                if (owner == 0 || !CFNumberGetValue(owner, 9, out int ownerPid) || ownerPid != pid) continue;
                var bounds = CFDictionaryGetValue(entry, String("kCGWindowBounds"));
                if (bounds != 0 && CGRectMakeWithDictionaryRepresentation(bounds, out NativeRect rect))
                    result.Add(new(rect.X, rect.Y, rect.Width, rect.Height));
            }
        }
        finally { Release(list); }
        return result;
    }

    public void Configure(Window window)
    {
        var native = NativeWindow(window);
        if (native == 0) return;
        SendPtr(native, Sel("setHidesOnDeactivate:"), 0);
        // Move with the active Space and allow the companion beside full-screen Codex.
        SendPtr(native, Sel("setCollectionBehavior:"), (nint)(2 | 256));
    }
    public void Attach(Window window, HostState host)
    {
        var native = NativeWindow(window);
        if (native != 0) SendPtr(native, Sel("setLevel:"), 3); // Keep the HUD above Codex when focus moves to another application.
    }
    private static nint NativeWindow(Window window)
    {
        var handle = window.TryGetPlatformHandle();
        if (handle is null) return 0;
        return handle.HandleDescriptor == "NSView" ? Send(handle.Handle, Sel("window"))
            : handle.HandleDescriptor == "NSWindow" ? handle.Handle : 0;
    }
    public void RequestPermission()
    {
        // Prompt only on explicit interaction. AXIsProcessTrusted() is used while polling.
        var options = SendPtr(objc_getClass("NSDictionary"), Sel("dictionaryWithObject:forKey:"),
            SendPtr(objc_getClass("NSNumber"), Sel("numberWithBool:"), 1), String("AXTrustedCheckOptionPrompt"));
        AXIsProcessTrustedWithOptions(options);
        Process.Start(new ProcessStartInfo("/usr/bin/open") { ArgumentList = { "x-apple.systempreferences:com.apple.preference.security?Privacy_Accessibility" }, UseShellExecute = false });
    }
    private void ClearApplication() { Release(_application); _application = 0; _pid = 0; }
    public void Dispose() => ClearApplication();
    private static nint Sel(string value) => sel_registerName(value);
    private static nint String(string value) => SendString(objc_getClass("NSString"), Sel("stringWithUTF8String:"), value);
    private static nint Copy(nint element, string name) => AXUIElementCopyAttributeValue(element, String(name), out var value) == 0 ? value : 0;
    private static bool ReadBool(nint element, string name) { var value = Copy(element, name); try { return value != 0 && CFBooleanGetValue(value); } finally { Release(value); } }
    private static void Release(nint value) { if (value != 0) CFRelease(value); }
    [StructLayout(LayoutKind.Sequential)] private struct Pair { public double X, Y; }
    [StructLayout(LayoutKind.Sequential)] private struct NativeRect { public double X, Y, Width, Height; }
    [DllImport(ObjC)] private static extern nint objc_getClass(string name);
    [DllImport(ObjC)] private static extern nint sel_registerName(string name);
    [DllImport(ObjC, EntryPoint = "objc_msgSend")] private static extern nint Send(nint target, nint selector);
    [DllImport(ObjC, EntryPoint = "objc_msgSend")] private static extern nint SendPtr(nint target, nint selector, nint value);
    [DllImport(ObjC, EntryPoint = "objc_msgSend")] private static extern nint SendPtr(nint target, nint selector, nint value, nint other);
    [DllImport(ObjC, EntryPoint = "objc_msgSend")] private static extern nint SendString(nint target, nint selector, [MarshalAs(UnmanagedType.LPUTF8Str)] string value);
    [DllImport(AX)] [return: MarshalAs(UnmanagedType.I1)] private static extern bool AXIsProcessTrusted();
    [DllImport(AX)] [return: MarshalAs(UnmanagedType.I1)] private static extern bool AXIsProcessTrustedWithOptions(nint options);
    [DllImport(AX)] private static extern nint AXUIElementCreateApplication(int pid);
    [DllImport(AX)] private static extern int AXUIElementSetMessagingTimeout(nint app, float seconds);
    [DllImport(AX)] private static extern int AXUIElementCopyAttributeValue(nint element, nint name, out nint value);
    [DllImport(AX)] [return: MarshalAs(UnmanagedType.I1)] private static extern bool AXValueGetValue(nint value, int type, out Pair result);
    [DllImport(CF)] private static extern void CFRelease(nint value);
    [DllImport(CF)] private static extern nint CFArrayGetCount(nint array);
    [DllImport(CF)] private static extern nint CFArrayGetValueAtIndex(nint array, nint index);
    [DllImport(CF)] private static extern nint CFDictionaryGetValue(nint dictionary, nint key);
    [DllImport(CF)] [return: MarshalAs(UnmanagedType.I1)] private static extern bool CFBooleanGetValue(nint value);
    [DllImport(CF)] [return: MarshalAs(UnmanagedType.I1)] private static extern bool CFEqual(nint a, nint b);
    [DllImport(CF)] [return: MarshalAs(UnmanagedType.I1)] private static extern bool CFNumberGetValue(nint value, int type, out int result);
    [DllImport(CG)] private static extern nint CGWindowListCopyWindowInfo(uint options, uint relativeToWindow);
    [DllImport(CG)] [return: MarshalAs(UnmanagedType.I1)] private static extern bool CGRectMakeWithDictionaryRepresentation(nint dictionary, out NativeRect rect);
}
