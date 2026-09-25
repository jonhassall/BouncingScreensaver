using Microsoft.Win32;

namespace BouncingScreensaver.Windows.Configuration;

public sealed class SettingsStore
{
    public const string MachineKeyPath = @"Software\BouncingScreensaver";
    public const string UserKeyPath = @"Software\BouncingScreensaver";
    public const string PolicyKeyPath = @"Software\Policies\BouncingScreensaver";

    public ScreensaverSettings Load()
    {
        var defaults = new ScreensaverSettings();
        return defaults with
        {
            LogoSource = ReadString(nameof(ScreensaverSettings.LogoSource), defaults.LogoSource),
            SpeedPxPerSecond = ReadInt(nameof(ScreensaverSettings.SpeedPxPerSecond), defaults.SpeedPxPerSecond, 20, 4000),
            LogoWidthPercent = ReadInt(nameof(ScreensaverSettings.LogoWidthPercent), defaults.LogoWidthPercent, 3, 60),
            BackgroundColor = ReadString(nameof(ScreensaverSettings.BackgroundColor), defaults.BackgroundColor),
            FlashColor = ReadString(nameof(ScreensaverSettings.FlashColor), defaults.FlashColor),
            FlashEnabled = ReadBool(nameof(ScreensaverSettings.FlashEnabled), defaults.FlashEnabled),
            FlashDurationMs = ReadInt(nameof(ScreensaverSettings.FlashDurationMs), defaults.FlashDurationMs, 50, 5000),
            CornerTolerancePx = ReadInt(nameof(ScreensaverSettings.CornerTolerancePx), defaults.CornerTolerancePx, 0, 200)
        };
    }

    public void SaveUser(ScreensaverSettings settings)
    {
        using var key = Registry.CurrentUser.CreateSubKey(UserKeyPath, writable: true)
            ?? throw new InvalidOperationException("Unable to create the user settings registry key.");

        key.SetValue(nameof(settings.LogoSource), settings.LogoSource, RegistryValueKind.String);
        key.SetValue(nameof(settings.SpeedPxPerSecond), settings.SpeedPxPerSecond, RegistryValueKind.DWord);
        key.SetValue(nameof(settings.LogoWidthPercent), settings.LogoWidthPercent, RegistryValueKind.DWord);
        key.SetValue(nameof(settings.BackgroundColor), settings.BackgroundColor, RegistryValueKind.String);
        key.SetValue(nameof(settings.FlashColor), settings.FlashColor, RegistryValueKind.String);
        key.SetValue(nameof(settings.FlashEnabled), settings.FlashEnabled ? 1 : 0, RegistryValueKind.DWord);
        key.SetValue(nameof(settings.FlashDurationMs), settings.FlashDurationMs, RegistryValueKind.DWord);
        key.SetValue(nameof(settings.CornerTolerancePx), settings.CornerTolerancePx, RegistryValueKind.DWord);
    }

    public void ClearUserOverrides() => Registry.CurrentUser.DeleteSubKeyTree(UserKeyPath, throwOnMissingSubKey: false);

    public void SetValue(string name, string rawValue, bool machine)
    {
        var normalizedName = NormalizeSettingName(name);
        var (value, kind) = ParseValue(normalizedName, rawValue);
        var hive = machine ? Registry.LocalMachine : Registry.CurrentUser;
        using var key = hive.CreateSubKey(machine ? MachineKeyPath : UserKeyPath, writable: true)
            ?? throw new InvalidOperationException("Unable to open the registry settings key for writing.");
        key.SetValue(normalizedName, value, kind);
    }

    public bool IsManaged(string name)
    {
        using var key = Registry.LocalMachine.OpenSubKey(PolicyKeyPath, writable: false);
        return key?.GetValue(name) is not null;
    }

    private string ReadString(string name, string fallback)
    {
        foreach (var value in EnumerateValues(name))
        {
            if (value is string s && !string.IsNullOrWhiteSpace(s))
            {
                return s;
            }
        }
        return fallback;
    }

    private int ReadInt(string name, int fallback, int min, int max)
    {
        foreach (var value in EnumerateValues(name))
        {
            if (value is int i)
            {
                return Math.Clamp(i, min, max);
            }
            if (value is string s && int.TryParse(s, out var parsed))
            {
                return Math.Clamp(parsed, min, max);
            }
        }
        return fallback;
    }

    private bool ReadBool(string name, bool fallback)
    {
        foreach (var value in EnumerateValues(name))
        {
            if (value is int i)
            {
                return i != 0;
            }
            if (value is string s && bool.TryParse(s, out var parsed))
            {
                return parsed;
            }
        }
        return fallback;
    }

    private IEnumerable<object?> EnumerateValues(string name)
    {
        using var policy = Registry.LocalMachine.OpenSubKey(PolicyKeyPath, writable: false);
        yield return policy?.GetValue(name);

        using var user = Registry.CurrentUser.OpenSubKey(UserKeyPath, writable: false);
        yield return user?.GetValue(name);

        using var machine = Registry.LocalMachine.OpenSubKey(MachineKeyPath, writable: false);
        yield return machine?.GetValue(name);
    }

    private static string NormalizeSettingName(string name)
    {
        var supported = typeof(ScreensaverSettings).GetProperties()
            .Select(p => p.Name)
            .FirstOrDefault(n => n.Equals(name, StringComparison.OrdinalIgnoreCase));
        return supported ?? throw new ArgumentException($"Unknown setting '{name}'.", nameof(name));
    }

    private static (object Value, RegistryValueKind Kind) ParseValue(string name, string rawValue) => name switch
    {
        nameof(ScreensaverSettings.LogoSource) or
        nameof(ScreensaverSettings.BackgroundColor) or
        nameof(ScreensaverSettings.FlashColor)
            => (rawValue, RegistryValueKind.String),

        nameof(ScreensaverSettings.FlashEnabled)
            => (ParseBoolean(rawValue) ? 1 : 0, RegistryValueKind.DWord),

        nameof(ScreensaverSettings.SpeedPxPerSecond)
            => (ParseInt(rawValue, 20, 4000), RegistryValueKind.DWord),
        nameof(ScreensaverSettings.LogoWidthPercent)
            => (ParseInt(rawValue, 3, 60), RegistryValueKind.DWord),
        nameof(ScreensaverSettings.FlashDurationMs)
            => (ParseInt(rawValue, 50, 5000), RegistryValueKind.DWord),
        nameof(ScreensaverSettings.CornerTolerancePx)
            => (ParseInt(rawValue, 0, 200), RegistryValueKind.DWord),

        _ => throw new ArgumentException($"Unknown setting '{name}'.", nameof(name))
    };

    private static bool ParseBoolean(string raw) => raw.Trim().ToLowerInvariant() switch
    {
        "1" or "true" or "yes" or "on" => true,
        "0" or "false" or "no" or "off" => false,
        _ => throw new ArgumentException($"'{raw}' is not a valid boolean value.")
    };

    private static int ParseInt(string raw, int min, int max)
    {
        if (!int.TryParse(raw, out var value) || value < min || value > max)
        {
            throw new ArgumentOutOfRangeException(nameof(raw), $"Value must be an integer between {min} and {max}.");
        }
        return value;
    }
}
