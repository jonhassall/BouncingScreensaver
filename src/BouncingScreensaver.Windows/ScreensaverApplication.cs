using BouncingScreensaver.Core;
using BouncingScreensaver.Windows.Assets;
using BouncingScreensaver.Windows.Configuration;
using BouncingScreensaver.Windows.Native;
using BouncingScreensaver.Windows.UI;

namespace BouncingScreensaver.Windows;

internal static class ScreensaverApplication
{
    private static readonly string SidecarLogoPath = Path.Combine(AppContext.BaseDirectory, "logo.png");

    public static void RunFullScreen(ScreensaverSettings settings)
    {
        var screens = Screen.AllScreens;
        if (screens.Length == 0)
        {
            return;
        }

        var topology = new DesktopTopology(screens.Select(ToRect));
        var primary = Screen.PrimaryScreen ?? screens[0];
        var repository = new LogoRepository();
        var cachedLogo = repository.TryLoadCached();
        using var host = new SimulationHost(topology, ToRect(primary), settings, cachedLogo);
        var windows = screens.Select(s => new RenderWindow(host, ToRect(s), fullScreen: true)).ToArray();

        var primaryIndex = Array.IndexOf(screens, primary);
        var primaryWindow = windows[primaryIndex >= 0 ? primaryIndex : 0];
        var secondaryWindows = windows.Where(w => !ReferenceEquals(w, primaryWindow)).ToArray();

        void CloseAll()
        {
            foreach (var window in windows)
            {
                if (!window.IsDisposed)
                {
                    window.Close();
                }
            }
        }

        primaryWindow.Shown += async (_, _) =>
        {
            foreach (var secondary in secondaryWindows)
            {
                secondary.Show();
            }
            primaryWindow.Activate();
            await RefreshLogoAsync(repository, host, settings, primaryWindow);
        };
        primaryWindow.FormClosed += (_, _) =>
        {
            foreach (var secondary in secondaryWindows)
            {
                if (!secondary.IsDisposed)
                {
                    secondary.Close();
                }
            }
        };

        var filter = new ExitInputFilter(CloseAll);
        Application.AddMessageFilter(filter);
        Cursor.Hide();
        host.Start();

        try
        {
            Application.Run(primaryWindow);
        }
        finally
        {
            Cursor.Show();
            Application.RemoveMessageFilter(filter);
            foreach (var window in windows)
            {
                window.Dispose();
            }
        }
    }

    public static void RunPreview(nint parentHandle, ScreensaverSettings settings)
    {
        if (parentHandle == nint.Zero)
        {
            return;
        }

        var size = NativeMethods.GetClientSize(parentHandle);
        var viewport = new RectD(0, 0, size.Width, size.Height);
        var topology = new DesktopTopology(new[] { viewport });
        var repository = new LogoRepository();
        var cachedLogo = repository.TryLoadCached();
        using var host = new SimulationHost(topology, viewport, settings, cachedLogo);
        using var window = new PreviewWindow(host, viewport, parentHandle);
        window.Shown += async (_, _) => await RefreshLogoAsync(repository, host, settings, window);
        host.Start();
        Application.Run(window);
    }

    public static void RunDebug(ScreensaverSettings settings)
    {
        var viewport = new RectD(0, 0, 1280, 720);
        var topology = new DesktopTopology(new[] { viewport });
        var repository = new LogoRepository();
        var cachedLogo = repository.TryLoadCached();
        using var host = new SimulationHost(topology, viewport, settings, cachedLogo);
        using var window = new RenderWindow(host, viewport, fullScreen: false)
        {
            Text = "BouncingScreensaver - Debug (Esc to close)",
            StartPosition = FormStartPosition.CenterScreen,
            ClientSize = new Size(1280, 720)
        };
        window.KeyDown += (_, e) =>
        {
            if (e.KeyCode == Keys.Escape)
            {
                window.Close();
            }
        };
        window.Shown += async (_, _) => await RefreshLogoAsync(repository, host, settings, window);
        host.Start();
        Application.Run(window);
    }

    private static async Task RefreshLogoAsync(
        LogoRepository repository,
        SimulationHost host,
        ScreensaverSettings settings,
        Control owner)
    {
        var refreshed = await repository.RefreshFromSourcesAsync(settings.LogoSource, SidecarLogoPath);
        if (refreshed is not null && !owner.IsDisposed)
        {
            host.SetLogo(refreshed);
        }
        else
        {
            refreshed?.Dispose();
        }
    }

    private static RectD ToRect(Screen screen) => new(
        screen.Bounds.Left,
        screen.Bounds.Top,
        screen.Bounds.Width,
        screen.Bounds.Height);
}
