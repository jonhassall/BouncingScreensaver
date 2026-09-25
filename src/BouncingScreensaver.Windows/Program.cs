using BouncingScreensaver.Core;
using BouncingScreensaver.Windows.Configuration;
using BouncingScreensaver.Windows.UI;

namespace BouncingScreensaver.Windows;

internal static class Program
{
    [STAThread]
    private static int Main(string[] args)
    {
        Application.SetHighDpiMode(HighDpiMode.PerMonitorV2);
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);

        var store = new SettingsStore();

        try
        {
            var command = ParseCommand(args);
            switch (command.Mode)
            {
                case LaunchMode.FullScreen:
                    ScreensaverApplication.RunFullScreen(store.Load());
                    break;
                case LaunchMode.Preview:
                    ScreensaverApplication.RunPreview(command.ParentHandle, store.Load());
                    break;
                case LaunchMode.Configure:
                    Application.Run(new SettingsForm(store));
                    break;
                case LaunchMode.Debug:
                    ScreensaverApplication.RunDebug(store.Load());
                    break;
                case LaunchMode.SetUser:
                    store.SetValue(command.SettingName!, command.SettingValue!, machine: false);
                    break;
                case LaunchMode.SetMachine:
                    store.SetValue(command.SettingName!, command.SettingValue!, machine: true);
                    break;
                case LaunchMode.SelfTest:
                    RunSelfTest(store);
                    break;
            }

            return 0;
        }
        catch (Exception ex)
        {
            if (!args.Any(a => a.Equals("--self-test", StringComparison.OrdinalIgnoreCase)) &&
                !args.Any(a => a.StartsWith("--set", StringComparison.OrdinalIgnoreCase)))
            {
                MessageBox.Show(ex.Message, "BouncingScreensaver", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            return 1;
        }
    }

    private static void RunSelfTest(SettingsStore store)
    {
        _ = store.Load();
        var topology = new DesktopTopology(new[] { new RectD(0, 0, 1920, 1080) });
        var simulation = new BounceSimulation(topology);
        var state = new LogoState(10, 10, 200, 100, -200, -200);
        _ = simulation.Step(state, 0.016, 12);
    }

    private static LaunchCommand ParseCommand(string[] args)
    {
        if (args.Length == 0)
        {
            return new LaunchCommand(LaunchMode.Configure);
        }

        var first = args[0].Trim();
        var normalized = first.TrimStart('/', '-').ToLowerInvariant();

        if (normalized == "s")
        {
            return new LaunchCommand(LaunchMode.FullScreen);
        }

        if (normalized == "c" || normalized.StartsWith("c:"))
        {
            return new LaunchCommand(LaunchMode.Configure);
        }

        if (normalized == "p" || normalized.StartsWith("p:"))
        {
            var handleText = normalized.StartsWith("p:") ? normalized[2..] : args.ElementAtOrDefault(1);
            return new LaunchCommand(LaunchMode.Preview, ParseHandle(handleText));
        }

        if (normalized == "debug")
        {
            return new LaunchCommand(LaunchMode.Debug);
        }

        if (normalized == "self-test")
        {
            return new LaunchCommand(LaunchMode.SelfTest);
        }

        if (normalized is "set" or "set-machine")
        {
            if (args.Length < 3)
            {
                throw new ArgumentException("Usage: --set <SettingName> <Value> or --set-machine <SettingName> <Value>.");
            }

            return new LaunchCommand(
                normalized == "set-machine" ? LaunchMode.SetMachine : LaunchMode.SetUser,
                SettingName: args[1],
                SettingValue: args[2]);
        }

        return new LaunchCommand(LaunchMode.Configure);
    }

    private static nint ParseHandle(string? value)
    {
        if (string.IsNullOrWhiteSpace(value) || !long.TryParse(value, out var handle))
        {
            return nint.Zero;
        }
        return new nint(handle);
    }

    private enum LaunchMode
    {
        FullScreen,
        Preview,
        Configure,
        Debug,
        SetUser,
        SetMachine,
        SelfTest
    }

    private sealed record LaunchCommand(
        LaunchMode Mode,
        nint ParentHandle = default,
        string? SettingName = null,
        string? SettingValue = null);
}
