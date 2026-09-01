namespace CodexTokenQuest.Desktop;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        // The HUD owns its pixel sizing, including its fonts. Keep the window
        // DPI-aware so Windows does not bitmap-scale the complete HUD.
        Application.SetHighDpiMode(HighDpiMode.PerMonitorV2);
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);

        using var singleInstance = new Mutex(true, "Local\\CodexTokenQuest.Desktop", out var isFirstInstance);
        if (!isFirstInstance)
        {
            return;
        }

        Application.Run(new UsageWindow());
    }
}
